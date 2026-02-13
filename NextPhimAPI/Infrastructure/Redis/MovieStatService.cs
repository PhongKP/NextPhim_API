using Microsoft.EntityFrameworkCore;
using NextPhimAPI.Application.DTOs;
using NextPhimAPI.Application.Interfaces;
using NextPhimAPI.Domain.Entities;
using NextPhimAPI.Infrastructure.Persistence;
using StackExchange.Redis;
using System.Globalization;

namespace NextPhimAPI.Infrastructure.Redis
{
    public class MovieStatService : IMovieStatsService
    {
        private readonly IDatabase _redis;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConnectionMultiplexer _redisConn;
        private readonly IServiceScopeFactory _scopeFactory;

        public MovieStatService(IConnectionMultiplexer redis, ApplicationDbContext dbContext,
            IConnectionMultiplexer redisConn, IServiceScopeFactory scope)
        {
            _redis = redis.GetDatabase();
            _dbContext = dbContext;
            _redisConn = redisConn;
            _scopeFactory = scope;
        }

        public async Task<IEnumerable<MovieTrendResponse>> GetTopTrendingMoviesAsync(string type, int limit)
        {
            var now = DateTime.UtcNow;

            string key = type.ToLower() switch
            {
                "daily" => $"movies:daily:{now:yyyyMMdd}",
                "weekly" => $"movies:weekly:{now.Year}-W{CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday):D2}",
                "monthly" => $"movies:monthly:{now:yyyyMM}",
                _ => throw new ArgumentException("Invalid type. Use 'daily', 'weekly', or 'monthly'.")
            };
            var topRedis = await _redis.SortedSetRangeByRankWithScoresAsync(key, 0, limit - 1, Order.Descending);

            if (!topRedis.Any())
            {
                return Enumerable.Empty<MovieTrendResponse>();
            }

            var movieIds = topRedis.Select(x => x.Element.ToString()).ToList();
            var movieDetail = await _dbContext.Movies
                .Where(m => movieIds.Contains(m.Id))
                .ToListAsync();
            return topRedis.Select(r =>
            {
               var movie = movieDetail.FirstOrDefault(m => m.Id == r.Element.ToString());
                return new MovieTrendResponse(
                    r.Element.ToString(),
                    movie?.Name ?? "Unknown",
                    movie?.Slug ?? "",
                    movie?.ThumbUrl ?? "",
                    r.Score
                );
            }).OrderByDescending(x => x.TotalViews);
        }

        public Task IncrementViewAsync(IncrementViewRequest request)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var redis = _redisConn.GetDatabase();

                    var now = DateTime.UtcNow;
                    string dailyKey = $"movies:daily:{now:yyyyMMdd}";
                    string monthlyKey = $"movies:monthly:{now:yyyyMM}";
                    int weekNum = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                        now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    string weeklyKey = $"movies:weekly:{now.Year}-W{weekNum:D2}";

                    var batch = redis.CreateBatch();
                    var t1 = batch.SortedSetIncrementAsync(dailyKey, request.Slug, 1);
                    var t2 = batch.SortedSetIncrementAsync(weeklyKey, request.Slug, 1);
                    var t3 = batch.SortedSetIncrementAsync(monthlyKey, request.Slug, 1);

                    _ = batch.KeyExpireAsync(dailyKey, TimeSpan.FromDays(2));
                    _ = batch.KeyExpireAsync(weeklyKey, TimeSpan.FromDays(14));
                    _ = batch.KeyExpireAsync(monthlyKey, TimeSpan.FromDays(60));

                    batch.Execute();
                    await Task.WhenAll(t1, t2, t3);

                    var movieExists = await dbContext.Movies.AnyAsync(m => m.Slug == request.Slug);
                    if (!movieExists)
                    {
                        dbContext.Movies.Add(new Movie
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = request.Name,
                            Slug = request.Slug,
                            ThumbUrl = request.ThumbUrl,
                            TotalViews = 0
                        });
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Background Task Error]: {ex.Message}");
                }
            });
            return Task.CompletedTask;
        }
    }
}
