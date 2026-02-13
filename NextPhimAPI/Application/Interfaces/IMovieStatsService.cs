using NextPhimAPI.Application.DTOs;

namespace NextPhimAPI.Application.Interfaces
{
    public interface IMovieStatsService
    {
        Task IncrementViewAsync(IncrementViewRequest request);
        Task<IEnumerable<MovieTrendResponse>> GetTopTrendingMoviesAsync(string type, int limit);
    }
}
