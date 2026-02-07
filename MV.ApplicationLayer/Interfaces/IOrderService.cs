using MV.DomainLayer.DTOs.Order.Request;
using MV.DomainLayer.DTOs.Order.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface IOrderService
{
    Task<ApiResponse<CheckoutResponse>> CheckoutAsync(int userId, CreateOrderRequest request);
    Task<ApiResponse<PagedResponse<OrderResponse>>> GetMyOrdersAsync(int userId, OrderFilterRequest filter);
    Task<ApiResponse<PagedResponse<OrderResponse>>> GetAllOrdersAsync(OrderFilterRequest filter);
    Task<ApiResponse<OrderDetailResponse>> GetOrderDetailAsync(int orderId, int userId, bool isAdmin);
    Task<ApiResponse<OrderDetailResponse>> UpdateOrderStatusAsync(int orderId, int adminUserId, UpdateOrderStatusRequest request);
    Task<ApiResponse<object>> CancelOrderAsync(int orderId, int userId, bool isAdmin, CancelOrderRequest request);
}
