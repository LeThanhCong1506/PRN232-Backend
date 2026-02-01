using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly StemDbContext _context;

    public BrandRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Brand>> GetAllBrandsAsync(int pageNumber, int pageSize)
    {
        return await _context.Brands
            .Include(b => b.Products)
            .OrderBy(b => b.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Brand?> GetBrandByIdAsync(int id)
    {
        return await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.BrandId == id);
    }

    public async Task<Brand?> GetBrandWithDetailsAsync(int id)
    {
        return await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.BrandId == id);
    }

    public async Task<Brand> CreateBrandAsync(Brand brand)
    {
        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();
        return brand;
    }

    public async Task UpdateBrandAsync(Brand brand)
    {
        _context.Brands.Update(brand);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBrandAsync(int id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand != null)
        {
            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalBrandsCountAsync()
    {
        return await _context.Brands.CountAsync();
    }

    public async Task<bool> BrandExistsAsync(int id)
    {
        return await _context.Brands.AnyAsync(b => b.BrandId == id);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await BrandExistsAsync(id);
    }

    public async Task<bool> BrandNameExistsAsync(string name, int? excludeBrandId = null)
    {
        var query = _context.Brands.Where(b => b.Name.ToLower() == name.ToLower());

        if (excludeBrandId.HasValue)
        {
            query = query.Where(b => b.BrandId != excludeBrandId.Value);
        }

        return await query.AnyAsync();
    }
}
