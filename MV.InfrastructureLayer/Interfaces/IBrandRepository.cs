using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IBrandRepository
{
    Task<IEnumerable<Brand>> GetAllBrandsAsync(int pageNumber, int pageSize);
    Task<Brand?> GetBrandByIdAsync(int id);
    Task<Brand?> GetBrandWithDetailsAsync(int id);
    Task<Brand> CreateBrandAsync(Brand brand);
    Task UpdateBrandAsync(Brand brand);
    Task DeleteBrandAsync(int id);
    Task<int> GetTotalBrandsCountAsync();
    Task<bool> BrandExistsAsync(int id);
    Task<bool> ExistsAsync(int id); // Alias for consistency
    Task<bool> BrandNameExistsAsync(string name, int? excludeBrandId = null);
}
