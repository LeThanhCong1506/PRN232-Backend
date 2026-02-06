using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync(int pageNumber, int pageSize);
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category?> GetCategoryWithDetailsAsync(int id);
    Task<Category> CreateCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(int id);
    Task<int> GetTotalCategoriesCountAsync();
    Task<bool> CategoryExistsAsync(int id);
    Task<bool> ExistsAsync(int id); // Alias for consistency
    Task<bool> CategoryNameExistsAsync(string name, int? excludeCategoryId = null);
}
