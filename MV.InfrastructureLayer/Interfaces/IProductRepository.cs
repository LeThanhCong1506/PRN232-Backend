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
        Task<Product> GetProductByIdAsync(int productId);
    }
}
