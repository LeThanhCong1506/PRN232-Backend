using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly StemDbContext _context;

    public CategoryRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync(int pageNumber, int pageSize)
    {
        return await _context.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
    }

    public async Task<Category?> GetCategoryWithDetailsAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCategoriesCountAsync()
    {
        return await _context.Categories.CountAsync();
    }

    public async Task<bool> CategoryExistsAsync(int id)
    {
        return await _context.Categories.AnyAsync(c => c.CategoryId == id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await CategoryExistsAsync(id);
    }

    public async Task<bool> CategoryNameExistsAsync(string name, int? excludeCategoryId = null)
    {
        var query = _context.Categories.Where(c => c.Name.ToLower() == name.ToLower());

        if (excludeCategoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId != excludeCategoryId.Value);
        }

        return await query.AnyAsync();
    }
}
