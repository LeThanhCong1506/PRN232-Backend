using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using Npgsql;

namespace MV.InfrastructureLayer.Repositories;

public class SepayRepository : ISepayRepository
{
    private readonly StemDbContext _context;

    public SepayRepository(StemDbContext context)
    {
        _context = context;
    }

    // ==================== SEPAY TRANSACTION ====================

    public async Task<SepayTransaction> CreateTransactionAsync(SepayTransaction transaction)
    {
        _context.SepayTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<bool> TransactionExistsBySepayIdAsync(string sepayId)
    {
        return await _context.SepayTransactions
            .AnyAsync(t => t.SepayId == sepayId);
    }

    public async Task UpdateTransactionAsync(SepayTransaction transaction)
    {
        _context.SepayTransactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    // ==================== PAYMENT QUERIES ====================

    public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
    {
        return await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<Payment?> GetPaymentByReferenceAsync(string paymentReference)
    {
        return await _context.Payments
            .Include(p => p.Order)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(p => p.PaymentReference == paymentReference);
    }

    /// <summary>
    /// Tìm các order có payment SEPAY đã hết hạn mà vẫn PENDING
    /// Dùng raw SQL vì payment_method và status là PostgreSQL custom enum
    /// </summary>
    public async Task<List<int>> GetExpiredPendingSepayOrderIdsAsync()
    {
        var orderIds = new List<int>();

        var conn = _context.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT p.order_id
                FROM payment p
                WHERE p.status = 'PENDING'::payment_status_enum
                  AND p.payment_method = 'SEPAY'::payment_method_enum
                  AND p.expired_at IS NOT NULL
                  AND p.expired_at < NOW()";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orderIds.Add(reader.GetInt32(0));
            }
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }

        return orderIds;
    }
}
