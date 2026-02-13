namespace NextPhimAPI.Application.DTOs
{
    public record IncrementViewRequest
    (
        string Name,
        string Slug,
        string ThumbUrl
    );
}
