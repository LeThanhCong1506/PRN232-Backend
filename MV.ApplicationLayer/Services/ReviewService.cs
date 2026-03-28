using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.Review.Response;
using MV.DomainLayer.DTOs.Review.Request;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.Interfaces;
using MV.DomainLayer.Entities;

namespace MV.ApplicationLayer.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IOrderRepository _orderRepository;

    public ReviewService(IReviewRepository reviewRepository, IOrderRepository orderRepository)
    {
        _reviewRepository = reviewRepository;
        _orderRepository = orderRepository;
    }

    public async Task<ApiResponse<ProductReviewsResponse>> GetProductReviewsAsync(int productId, int page, int pageSize)
    {
        var totalReviews = await _reviewRepository.CountByProductIdAsync(productId);
        var reviews = await _reviewRepository.GetByProductIdAsync(productId, page, pageSize);
        var ratingDistribution = await _reviewRepository.GetRatingDistributionAsync(productId);

        // Tính average rating từ distribution (tránh query thêm)
        double averageRating = 0;
        if (totalReviews > 0)
        {
            var totalScore = ratingDistribution.Sum(kvp => kvp.Key * kvp.Value);
            averageRating = Math.Round((double)totalScore / totalReviews, 1);
        }

        var response = new ProductReviewsResponse
        {
            AverageRating = averageRating,
            TotalReviews = totalReviews,
            RatingDistribution = ratingDistribution.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
            Reviews = reviews.Select(r => new ReviewItemResponse
            {
                ReviewId = r.ReviewId,
                Rating = r.Rating,
                Comment = r.Comment,
                Reviewer = FormatReviewerName(r.User?.FullName ?? r.User?.Username),
                CreatedAt = r.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalPages = totalReviews > 0 ? (int)Math.Ceiling((double)totalReviews / pageSize) : 0
        };

        return ApiResponse<ProductReviewsResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<ReviewItemResponse>> CreateReviewAsync(int userId, int productId, CreateReviewRequest request)
    {
        bool hasPurchased = await _orderRepository.HasUserPurchasedProductAsync(userId, productId);
        if (!hasPurchased)
        {
            return ApiResponse<ReviewItemResponse>.ErrorResponse("You must purchase and receive this product before reviewing it.");
        }

        bool hasReviewed = await _reviewRepository.HasUserReviewedProductAsync(userId, productId);
        if (hasReviewed)
        {
            return ApiResponse<ReviewItemResponse>.ErrorResponse("You have already reviewed this product.");
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTimeHelper.VietnamNow()
        };

        await _reviewRepository.AddAsync(review);

        var response = new ReviewItemResponse
        {
            ReviewId = review.ReviewId,
            Rating = review.Rating,
            Comment = review.Comment,
            Reviewer = "You",
            CreatedAt = review.CreatedAt
        };

        return ApiResponse<ReviewItemResponse>.SuccessResponse(response, "Review submitted successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteReviewAsync(int reviewId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);
        if (review == null)
        {
            return ApiResponse<bool>.ErrorResponse($"Review with ID {reviewId} not found.");
        }

        await _reviewRepository.DeleteAsync(reviewId);
        return ApiResponse<bool>.SuccessResponse(true, "Review deleted successfully.");
    }

    /// <summary>
    /// Format tên reviewer để ẩn danh một phần: "Nguyen Van A" → "Nguyen V."
    /// </summary>
    private static string FormatReviewerName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Anonymous";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return parts[0];
        // Vietnamese name: "Nguyen Van A" → "Nguyen V."
        return parts[0] + " " + parts[1][0] + ".";
    }
}
