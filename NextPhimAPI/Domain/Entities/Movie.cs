using System.ComponentModel.DataAnnotations;

namespace NextPhimAPI.Domain.Entities
{
    public class Movie
    {
        [Key]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ThumbUrl { get; set; } = string.Empty;
        public long TotalViews { get; set; }
    }
}
