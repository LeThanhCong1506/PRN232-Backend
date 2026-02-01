using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.DTOs.Product.Request;
using MV.DomainLayer.DTOs.Product.Response;

namespace MV.ApplicationLayer.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetProductsAsync(ProductFilter filter);
        Task<ApiResponse<List<CategoryResponseDto>>> GetAllCategoriesAsync();
        Task<ApiResponse<List<BrandResponseDto>>> GetAllBrandsAsync();
        
        // Product CRUD
        Task<ApiResponse<ProductDetailResponse>> GetByIdAsync(int productId);
        Task<ApiResponse<ProductDetailResponse>> CreateAsync(CreateProductRequest request);
        Task<ApiResponse<ProductDetailResponse>> CreateKitAsync(CreateKitRequest request);
        Task<ApiResponse<ProductDetailResponse>> UpdateAsync(int productId, UpdateProductRequest request);
        Task<ApiResponse<bool>> DeleteAsync(int productId);
        
        // Category management
        Task<ApiResponse<bool>> AddCategoriesToProductAsync(int productId, List<int> categoryIds);
        Task<ApiResponse<bool>> RemoveCategoryFromProductAsync(int productId, int categoryId);
    }
}
