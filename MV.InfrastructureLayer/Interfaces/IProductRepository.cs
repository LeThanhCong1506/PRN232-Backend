using MV.DomainLayer.DTOs.Admin.Product.Request;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces
{
    public interface IProductRepository
    {
        Task<(List<Product> Items, int TotalCount)> GetPagedProductsAsync(ProductFilter filter);
        Task<List<CategoryResponseDto>> GetAllCategoriesWithCountAsync();
        Task<List<BrandResponseDto>> GetAllBrandsWithCountAsync();

        // Product CRUD
        Task<Product?> GetByIdAsync(int productId);
        Task<Product?> GetDetailByIdAsync(int productId);
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int productId);

        // Validation
        Task<bool> ExistsAsync(int productId);
        Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null);
        Task<bool> HasOrdersAsync(int productId);

        // Category associations
        Task AddCategoriesToProductAsync(int productId, List<int> categoryIds);
        Task RemoveCategoryFromProductAsync(int productId, int categoryId);
        Task<List<int>> GetCategoryIdsByProductIdAsync(int productId);
        Task<Product> GetProductByIdAsync(int productId);

        // Admin
        Task<(List<Product> Items, int TotalCount)> GetAdminPagedProductsAsync(AdminProductFilter filter);
        Task<bool> SoftDeleteAsync(int productId);
        Task<bool?> ToggleActiveAsync(int productId);
    }
}
