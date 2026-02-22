namespace MV.DomainLayer.DTOs.Review.Response;

public class ProductReviewsResponse
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
    public List<ReviewItemResponse> Reviews { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ReviewItemResponse
{
    public int ReviewId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string Reviewer { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
}
