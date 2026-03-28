using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

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
            .AsTracking() // Entity sẽ được update sau → cần tracking
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<Payment?> GetPaymentByReferenceAsync(string paymentReference)
    {
        return await _context.Payments
            .AsTracking() // Entity sẽ được update trong ProcessWebhookAsync → cần tracking
            .Include(p => p.Order)
                .ThenInclude(o => o.OrderItems)
            .FirstOrDefaultAsync(p => p.PaymentReference == paymentReference);
    }

}
