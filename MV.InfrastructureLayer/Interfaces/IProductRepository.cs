using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces
{
    public interface IProductRepository
    {
        Task<(List<Product> Items, int TotalCount)> GetPagedProductsAsync(ProductFilter filter);
    }
}
