using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.Review.Response;
using MV.DomainLayer.DTOs.Review.Request;

namespace MV.ApplicationLayer.Interfaces;

public interface IReviewService
{
    Task<ApiResponse<ProductReviewsResponse>> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<ApiResponse<ReviewItemResponse>> CreateReviewAsync(int userId, int productId, CreateReviewRequest request);
    Task<ApiResponse<bool>> DeleteReviewAsync(int reviewId);
}
