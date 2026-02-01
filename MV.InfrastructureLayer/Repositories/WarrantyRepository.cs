using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class WarrantyRepository : IWarrantyRepository
{
    private readonly StemDbContext _context;

    public WarrantyRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<Warranty?> GetByIdAsync(int id)
    {
        return await _context.Warranties
            .Include(w => w.WarrantyPolicy)
            .Include(w => w.SerialNumberNavigation)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(w => w.WarrantyId == id);
    }

    public async Task<Warranty?> GetBySerialNumberAsync(string serialNumber)
    {
        return await _context.Warranties
            .Include(w => w.WarrantyPolicy)
            .Include(w => w.SerialNumberNavigation)
                .ThenInclude(pi => pi.Product)
            .FirstOrDefaultAsync(w => w.SerialNumber == serialNumber);
    }

    public async Task<IEnumerable<Warranty>> GetAllAsync()
    {
        return await _context.Warranties
            .Include(w => w.WarrantyPolicy)
            .Include(w => w.SerialNumberNavigation)
                .ThenInclude(pi => pi.Product)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Warranty>> GetByProductIdAsync(int productId)
    {
        return await _context.Warranties
            .Include(w => w.WarrantyPolicy)
            .Include(w => w.SerialNumberNavigation)
                .ThenInclude(pi => pi.Product)
            .Where(w => w.SerialNumberNavigation.ProductId == productId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Warranty>> GetActiveWarrantiesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Warranties
            .Include(w => w.WarrantyPolicy)
            .Include(w => w.SerialNumberNavigation)
                .ThenInclude(pi => pi.Product)
            .Where(w => w.IsActive == true && w.EndDate >= today)
            .OrderByDescending(w => w.EndDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Warranty>> GetExpiredWarrantiesAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Warranties
            .Include(w => w.WarrantyPolicy)
            .Include(w => w.SerialNumberNavigation)
                .ThenInclude(pi => pi.Product)
            .Where(w => w.EndDate < today)
            .OrderByDescending(w => w.EndDate)
            .ToListAsync();
    }

    public async Task<Warranty> CreateAsync(Warranty warranty)
    {
        _context.Warranties.Add(warranty);
        await _context.SaveChangesAsync();
        return warranty;
    }

    public async Task UpdateAsync(Warranty warranty)
    {
        _context.Warranties.Update(warranty);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var warranty = await _context.Warranties.FindAsync(id);
        if (warranty != null)
        {
            _context.Warranties.Remove(warranty);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> SerialNumberExistsAsync(string serialNumber, int? excludeId = null)
    {
        if (excludeId.HasValue)
        {
            return await _context.Warranties
                .AnyAsync(w => w.SerialNumber == serialNumber && w.WarrantyId != excludeId.Value);
        }
        return await _context.Warranties.AnyAsync(w => w.SerialNumber == serialNumber);
    }
}
