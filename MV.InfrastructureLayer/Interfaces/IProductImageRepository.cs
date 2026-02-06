using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IProductImageRepository
{
    Task<ProductImage> AddAsync(ProductImage productImage);
    Task DeleteAsync(int imageId);
    Task<List<ProductImage>> GetByProductIdAsync(int productId);
    Task<ProductImage?> GetByIdAsync(int imageId);
    Task<bool> ExistsAsync(int imageId);
}
