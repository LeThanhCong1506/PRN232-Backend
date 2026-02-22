using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class WarrantyClaimRepository : IWarrantyClaimRepository
{
    private readonly StemDbContext _context;

    public WarrantyClaimRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<WarrantyClaim?> GetByIdAsync(int claimId)
    {
        return await _context.WarrantyClaims
            .Include(c => c.Warranty)
                .ThenInclude(w => w.SerialNumberNavigation)
                    .ThenInclude(pi => pi.Product)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);
    }

    public async Task<WarrantyClaim> CreateAsync(WarrantyClaim claim)
    {
        _context.WarrantyClaims.Add(claim);
        await _context.SaveChangesAsync();
        return claim;
    }

    public async Task UpdateAsync(WarrantyClaim claim)
    {
        _context.WarrantyClaims.Update(claim);
        await _context.SaveChangesAsync();
    }

    public async Task<List<WarrantyClaim>> GetAllAsync(string? status, int page, int pageSize)
    {
        var query = _context.WarrantyClaims
            .Include(c => c.Warranty)
                .ThenInclude(w => w.SerialNumberNavigation)
                    .ThenInclude(pi => pi.Product)
            .Include(c => c.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Resolution == status);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string? status)
    {
        var query = _context.WarrantyClaims.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Resolution == status);
        }

        return await query.CountAsync();
    }
}
