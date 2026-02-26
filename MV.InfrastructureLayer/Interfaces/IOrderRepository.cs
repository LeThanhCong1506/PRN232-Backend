using MV.DomainLayer.DTOs.Admin.Order.Request;
using MV.DomainLayer.DTOs.Admin.Order.Response;
using MV.DomainLayer.DTOs.Order.Request;
using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IOrderRepository
{
    // Create
    Task<OrderHeader> CreateOrderAsync(OrderHeader orderHeader);
    Task CreateOrderItemsAsync(List<OrderItem> items);
    Task<Payment> CreatePaymentAsync(Payment payment, string paymentMethod = "COD", string status = "PENDING");

    // Read
    Task<OrderHeader?> GetOrderByIdAsync(int orderId);
    Task<OrderHeader?> GetOrderByOrderNumberAsync(string orderNumber);
    Task<(List<OrderHeader> Items, int TotalCount)> GetOrdersByUserIdAsync(int userId, OrderFilterRequest filter);
    Task<(List<OrderHeader> Items, int TotalCount)> GetAllOrdersAsync(OrderFilterRequest filter);
    Task<int> GetTodayOrderCountAsync();
    Task<bool> HasUserPurchasedProductAsync(int userId, int productId);

    // Update
    Task UpdateOrderAsync(OrderHeader orderHeader);
    Task UpdatePaymentAsync(Payment payment);

    // Stock
    Task<int> DecrementStockAsync(int productId, int quantity);
    Task IncrementStockAsync(int productId, int quantity);

    // Coupon
    Task IncrementCouponUsedCountAsync(int couponId);
    Task DecrementCouponUsedCountAsync(int couponId);
    Task<Coupon?> GetCouponByCodeAsync(string code);

    // Raw SQL for enum columns (not scaffolded)
    Task<string?> GetOrderStatusAsync(int orderId);
    Task SetOrderStatusAsync(int orderId, string status);
    Task<Dictionary<int, string>> GetOrderStatusesBatchAsync(List<int> orderIds);
    Task<string?> GetPaymentMethodByOrderIdAsync(int orderId);
    Task<string?> GetPaymentStatusByOrderIdAsync(int orderId);
    Task SetPaymentStatusByOrderIdAsync(int orderId, string status);
    Task<Dictionary<int, (string? Method, string? Status)>> GetPaymentEnumsBatchAsync(List<int> orderIds);
    Task<string?> GetCouponDiscountTypeAsync(int couponId);

    // Polling: lấy danh sách order PENDING + SEPAY (chưa hết hạn)
    Task<List<OrderHeader>> GetPendingSepayOrdersAsync();

    // Admin Order Management
    Task<(List<OrderHeader> Items, int TotalCount)> GetAdminOrdersAsync(AdminOrderFilter filter);
    Task<Dictionary<string, int>> GetOrderStatusCountsAsync();
    Task<decimal> GetDeliveredRevenueAsync(DateTime from, DateTime to);
    Task<List<DailyRevenueData>> GetDailyRevenueAsync(DateTime from, DateTime to, string? status = null);
    Task<List<OrderHeader>> GetRecentOrdersAsync(int count);
    Task SetProductInstanceStatusByOrderItemIdsAsync(List<int> orderItemIds, string status);
    Task CreateWarrantiesForDeliveredOrderAsync(int orderId);
}
