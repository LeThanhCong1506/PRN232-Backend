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
}
