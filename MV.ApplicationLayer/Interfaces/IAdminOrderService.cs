using MV.DomainLayer.DTOs.Admin.Order.Request;
using MV.DomainLayer.DTOs.Admin.Order.Response;
using MV.DomainLayer.DTOs.ResponseModels;

namespace MV.ApplicationLayer.Interfaces;

public interface IAdminOrderService
{
    Task<ApiResponse<AdminOrderListResult>> GetAdminOrdersAsync(AdminOrderFilter filter);
    Task<ApiResponse<AdminOrderDetailResponse>> GetAdminOrderDetailAsync(int orderId);
    Task<ApiResponse<AdminOrderDetailResponse>> UpdateOrderStatusAsync(int orderId, int adminUserId, AdminUpdateOrderStatusRequest request);
    Task<ApiResponse<bool>> UpdatePaymentStatusAsync(int orderId, UpdatePaymentStatusRequest request);
    Task<ApiResponse<DashboardResponse>> GetDashboardAsync();
    Task<ApiResponse<List<DailyRevenueData>>> GetRevenueChartAsync(DateTime from, DateTime to, string? status = null);
}

public class AdminOrderListResult
{
    public List<AdminOrderResponse> Orders { get; set; } = new();
    public AdminOrderStatusCounts StatusCounts { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = null!;
}

public class AdminOrderStatusCounts
{
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Shipped { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
}
