using NextPhimAPI.Application.DTOs;

namespace NextPhimAPI.Application.Interfaces
{
    public interface IMovieStatsService
    {
        Task IncrementViewAsync(string movieId);
        Task<IEnumerable<MovieTrendResponse>> GetTopTrendingMoviesAsync(string type, int limit);
    }
}
