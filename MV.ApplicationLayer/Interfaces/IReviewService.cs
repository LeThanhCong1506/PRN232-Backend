using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.Review.Response;

namespace MV.ApplicationLayer.Interfaces;

public interface IReviewService
{
    Task<ApiResponse<ProductReviewsResponse>> GetProductReviewsAsync(int productId, int page, int pageSize);
    Task<ApiResponse<bool>> DeleteReviewAsync(int reviewId);
}
