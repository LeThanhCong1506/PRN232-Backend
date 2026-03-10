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
        // Use raw SQL because PostgreSQL requires explicit cast for claim_status_enum
        var pWarrantyId = new Npgsql.NpgsqlParameter("p0", claim.WarrantyId);
        var pUserId = new Npgsql.NpgsqlParameter("p1", claim.UserId);
        var pClaimDate = new Npgsql.NpgsqlParameter("p2", claim.ClaimDate);
        var pIssue = new Npgsql.NpgsqlParameter("p3", claim.IssueDescription);
        var pStatus = new Npgsql.NpgsqlParameter("p4", (object)(claim.Status ?? "SUBMITTED"));
        var pPhone = new Npgsql.NpgsqlParameter("p5", (object?)claim.ContactPhone ?? DBNull.Value);
        var pCreatedAt = new Npgsql.NpgsqlParameter("p6", (object)(claim.CreatedAt ?? DateTime.Now));

        await _context.Database.ExecuteSqlRawAsync(
            @"INSERT INTO warranty_claim (warranty_id, user_id, claim_date, issue_description, status, contact_phone, created_at)
              VALUES (@p0, @p1, @p2, @p3, @p4::claim_status_enum, @p5, @p6)",
            pWarrantyId, pUserId, pClaimDate, pIssue, pStatus, pPhone, pCreatedAt);

        // Reload to get the generated claim_id
        var created = await _context.WarrantyClaims
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(c => c.WarrantyId == claim.WarrantyId && c.UserId == claim.UserId);

        return created ?? claim;
    }

    public async Task UpdateAsync(WarrantyClaim claim)
    {
        // Use raw SQL for status update with enum cast
        var pStatus = new Npgsql.NpgsqlParameter("p0", (object)(claim.Status ?? "SUBMITTED"));
        var pResolution = new Npgsql.NpgsqlParameter("p1", (object?)claim.Resolution ?? DBNull.Value);
        var pNote = new Npgsql.NpgsqlParameter("p2", (object?)claim.ResolutionNote ?? DBNull.Value);
        var pDate = new Npgsql.NpgsqlParameter("p3", claim.ResolvedDate.HasValue ? (object)claim.ResolvedDate.Value : DBNull.Value);
        var pClaimId = new Npgsql.NpgsqlParameter("p4", claim.ClaimId);

        await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE warranty_claim SET 
                status = @p0::claim_status_enum, 
                resolution = @p1, 
                resolution_note = @p2, 
                resolved_date = @p3
              WHERE claim_id = @p4",
            pStatus, pResolution, pNote, pDate, pClaimId);

        // Detach so EF Core doesn't try to re-save the dirty Status on next SaveChangesAsync
        _context.Entry(claim).State = EntityState.Detached;
    }

    public async Task<List<WarrantyClaim>> GetAllAsync(string? status, int page, int pageSize)
    {
        IQueryable<WarrantyClaim> query;

        if (!string.IsNullOrWhiteSpace(status))
        {
            var pStatus = new Npgsql.NpgsqlParameter("pStatus", status);
            query = _context.WarrantyClaims
                .FromSqlRaw("SELECT * FROM warranty_claim WHERE status = @pStatus::claim_status_enum", pStatus);
        }
        else
        {
            query = _context.WarrantyClaims.AsQueryable();
        }

        return await query
            .Include(c => c.Warranty)
                .ThenInclude(w => w.SerialNumberNavigation)
                    .ThenInclude(pi => pi.Product)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string? status)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            var pStatus = new Npgsql.NpgsqlParameter("pStatus", status);
            return await _context.WarrantyClaims
                .FromSqlRaw("SELECT * FROM warranty_claim WHERE status = @pStatus::claim_status_enum", pStatus)
                .CountAsync();
        }

        return await _context.WarrantyClaims.CountAsync();
    }
}
