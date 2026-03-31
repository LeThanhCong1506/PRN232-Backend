using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly StemDbContext _context;

    public ReturnRequestRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<ReturnRequest> CreateAsync(ReturnRequest request)
    {
        request.CreatedAt = DateTimeHelper.VietnamNow();
        await _context.ReturnRequests.AddAsync(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<ReturnRequest?> GetByIdAsync(int returnRequestId)
    {
        return await _context.ReturnRequests
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductInstances)
            .Include(r => r.User)
            .Include(r => r.ProcessedByNavigation)
            .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequestId);
    }

    public async Task<(List<ReturnRequest> Items, int TotalCount)> GetByUserIdPagedAsync(int userId, int page, int pageSize)
    {
        var query = _context.ReturnRequests
            .Include(r => r.Order)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task<(List<ReturnRequest> Items, int TotalCount)> GetAllPagedAsync(int page, int pageSize)
    {
        var query = _context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.User)
            .Include(r => r.ProcessedByNavigation)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task UpdateAsync(ReturnRequest request)
    {
        request.UpdatedAt = DateTimeHelper.VietnamNow();
        _context.ReturnRequests.Update(request);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasActiveRequestForOrderAsync(int orderId)
    {
        return await _context.ReturnRequests.AnyAsync(r =>
            r.OrderId == orderId &&
            r.Status != "REJECTED" &&
            r.Status != "COMPLETED");
    }
}
