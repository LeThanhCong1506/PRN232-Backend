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
        // Must use Raw SQL for PostgreSQL ENUM (claim_status_enum)
        var sql = @"
            UPDATE warranty_claim
            SET status = {0}::claim_status_enum,
                resolution = {1},
                resolution_note = {2},
                resolved_date = {3}
            WHERE claim_id = {4}
        ";
        await _context.Database.ExecuteSqlRawAsync(sql, claim.Status, claim.Resolution, claim.ResolutionNote, claim.ResolvedDate, claim.ClaimId);
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
            query = query.Where(c => c.Status == status);
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
            query = query.Where(c => c.Status == status);
        }

        return await query.CountAsync();
    }
}
