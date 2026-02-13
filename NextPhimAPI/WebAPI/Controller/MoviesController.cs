using Microsoft.AspNetCore.Mvc;
using NextPhimAPI.Application.DTOs;
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

        [HttpPost("increment-view")]
        public async Task<IActionResult> IncrementView([FromBody] IncrementViewRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Slug))
            {
                return BadRequest("Invalid request data.");
            }
            await _movieStatsService.IncrementViewAsync(request);
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
