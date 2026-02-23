using Microsoft.AspNetCore.Http;
using MV.DomainLayer.DTOs.Admin.Product.Request;
using MV.DomainLayer.DTOs.Admin.Product.Response;
using MV.DomainLayer.DTOs.Product.Request;
using MV.DomainLayer.DTOs.Product.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface IAdminProductService
{
    Task<ApiResponse<PagedResponse<AdminProductResponse>>> GetAdminProductsAsync(AdminProductFilter filter);
    Task<ApiResponse<ProductDetailResponse>> CreateProductAsync(CreateProductRequest request);
    Task<ApiResponse<ProductDetailResponse>> UpdateProductAsync(int productId, UpdateProductRequest request);
    Task<ApiResponse<bool>> SoftDeleteProductAsync(int productId);
    Task<ApiResponse<List<AdminProductImageResponse>>> UploadImagesAsync(int productId, List<IFormFile> files, int? setPrimaryIndex);
    Task<ApiResponse<bool>> DeleteImageAsync(int productId, int imageId);
}
