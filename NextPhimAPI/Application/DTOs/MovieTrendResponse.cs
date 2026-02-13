namespace NextPhimAPI.Application.DTOs
{
   public record MovieTrendResponse(
       string Id,
       string Name,
       string Slug,
       string ThumbUrl,
       double TotalViews
   );
}
