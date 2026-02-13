using Microsoft.EntityFrameworkCore;
using NextPhimAPI.Application.DTOs;
using NextPhimAPI.Application.Interfaces;
using NextPhimAPI.Infrastructure.Persistence;
using StackExchange.Redis;
using System.Globalization;

namespace NextPhimAPI.Infrastructure.Redis
{
    public class MovieStatService : IMovieStatsService
    {
        private readonly IDatabase _redis;
        private readonly ApplicationDbContext _dbContext;

        public MovieStatService(IConnectionMultiplexer redis, ApplicationDbContext dbContext)
        {
            _redis = redis.GetDatabase();
            _dbContext = dbContext;
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

        public async Task IncrementViewAsync(string movieId)
        {
            var now = DateTime.UtcNow;

            string dailyKey = $"movies:daily:{now:yyyyMMdd}";
            string monthlyKey = $"movies:monthly:{now:yyyyMM}";

            int weekNum = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                now,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday
            );
            string weeklyKey = $"movies:weekly:{now.Year}-W{weekNum:D2}";

            var batch = _redis.CreateBatch();

            var t1 = batch.SortedSetIncrementAsync(dailyKey, movieId, 1);
            var t2 = batch.SortedSetIncrementAsync(weeklyKey, movieId, 1);
            var t3 = batch.SortedSetIncrementAsync(monthlyKey, movieId, 1);

            _ = batch.KeyExpireAsync(dailyKey, TimeSpan.FromDays(2));
            _ = batch.KeyExpireAsync(weeklyKey, TimeSpan.FromDays(14));
            _ = batch.KeyExpireAsync(monthlyKey, TimeSpan.FromDays(60));

            batch.Execute();
            await Task.WhenAll(t1, t2, t3);
        }
    }
}
