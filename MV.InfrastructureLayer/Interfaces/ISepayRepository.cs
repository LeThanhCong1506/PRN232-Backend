using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface ISepayRepository
{
    // SepayConfig
    Task<SepayConfig?> GetActiveSepayConfigAsync();

    // SepayTransaction
    Task<SepayTransaction> CreateTransactionAsync(SepayTransaction transaction);
    Task<bool> TransactionExistsBySepayIdAsync(string sepayId);
    Task UpdateTransactionAsync(SepayTransaction transaction);

    // Payment queries
    Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
    Task<Payment?> GetPaymentByReferenceAsync(string paymentReference);
    Task<List<int>> GetExpiredPendingSepayOrderIdsAsync();
}
