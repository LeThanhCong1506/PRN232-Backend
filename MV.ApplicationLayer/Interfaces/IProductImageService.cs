using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.ProductImage.Request;
using MV.DomainLayer.DTOs.ProductImage.Response;

namespace MV.ApplicationLayer.Interfaces;

public interface IProductImageService
{
    Task<ApiResponse<ProductImageResponse>> AddImageAsync(int productId, AddProductImageRequest request);
    Task<ApiResponse<bool>> DeleteImageAsync(int imageId);
    Task<ApiResponse<List<ProductImageResponse>>> GetImagesByProductIdAsync(int productId);
}
