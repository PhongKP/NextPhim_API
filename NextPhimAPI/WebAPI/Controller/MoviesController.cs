using Microsoft.AspNetCore.Mvc;
using NextPhimAPI.Application.Interfaces;

namespace NextPhimAPI.WebAPI.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieStatsService _movieStatsService;
        public MoviesController(IMovieStatsService movieStatsService)
        {
            _movieStatsService = movieStatsService;
        }

        [HttpPost("{movieId}/increment-view")]
        public async Task<IActionResult> IncrementView(string movieId)
        {
            await _movieStatsService.IncrementViewAsync(movieId);
            return Ok();
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingMovies([FromQuery] string type = "daily", [FromQuery] int limit = 5)
        {
            var trendingMovies = await _movieStatsService.GetTopTrendingMoviesAsync(type, limit);
            return Ok(trendingMovies);
        }
    }
}
