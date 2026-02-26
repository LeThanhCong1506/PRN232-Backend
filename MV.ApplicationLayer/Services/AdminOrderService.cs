using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Order.Request;
using MV.DomainLayer.DTOs.Admin.Order.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUserRepository _userRepo;
    private readonly StemDbContext _context;

    private static readonly Dictionary<string, List<string>> AllowedTransitions = new()
    {
        { "PENDING", new List<string> { "CONFIRMED", "CANCELLED" } },
        { "CONFIRMED", new List<string> { "SHIPPED", "CANCELLED" } },
        { "SHIPPED", new List<string> { "DELIVERED" } },
        { "DELIVERED", new List<string>() },
        { "CANCELLED", new List<string>() }
    };

    public AdminOrderService(
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IUserRepository userRepo,
        StemDbContext context)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _userRepo = userRepo;
        _context = context;
    }

    public async Task<ApiResponse<AdminOrderListResult>> GetAdminOrdersAsync(AdminOrderFilter filter)
    {
        var (orders, totalCount) = await _orderRepo.GetAdminOrdersAsync(filter);

        // Get enum values for these orders
        var orderIds = orders.Select(o => o.OrderId).ToList();
        var statusMap = await _orderRepo.GetOrderStatusesBatchAsync(orderIds);
        var paymentMap = await _orderRepo.GetPaymentEnumsBatchAsync(orderIds);

        var items = orders.Select(o =>
        {
            statusMap.TryGetValue(o.OrderId, out var status);
            paymentMap.TryGetValue(o.OrderId, out var payment);

            return new AdminOrderResponse
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                Status = status ?? "PENDING",
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                CustomerPhone = o.CustomerPhone,
                TotalAmount = o.TotalAmount,
                PaymentMethod = payment.Method,
                PaymentStatus = payment.Status,
                ItemCount = o.OrderItems.Count,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            };
        }).ToList();

        // Get status counts
        var statusCounts = await _orderRepo.GetOrderStatusCountsAsync();

        var result = new AdminOrderListResult
        {
            Orders = items,
            StatusCounts = new AdminOrderStatusCounts
            {
                Pending = statusCounts.GetValueOrDefault("PENDING", 0),
                Confirmed = statusCounts.GetValueOrDefault("CONFIRMED", 0),
                Shipped = statusCounts.GetValueOrDefault("SHIPPED", 0),
                Delivered = statusCounts.GetValueOrDefault("DELIVERED", 0),
                Cancelled = statusCounts.GetValueOrDefault("CANCELLED", 0)
            },
            Pagination = new PaginationMetadata
            {
                CurrentPage = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            }
        };

        return ApiResponse<AdminOrderListResult>.SuccessResponse(result);
    }

    public async Task<ApiResponse<AdminOrderDetailResponse>> GetAdminOrderDetailAsync(int orderId)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
            return ApiResponse<AdminOrderDetailResponse>.ErrorResponse($"Order with ID {orderId} not found.");

        var status = await _orderRepo.GetOrderStatusAsync(orderId);
        var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
        var paymentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(orderId);

        var currentStatus = status ?? "PENDING";
        AllowedTransitions.TryGetValue(currentStatus, out var transitions);

        var response = new AdminOrderDetailResponse
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            OrderNumber = order.OrderNumber,
            Status = currentStatus,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            ShippingAddress = order.ShippingAddress,
            Province = order.Province,
            District = order.District,
            Ward = order.Ward,
            StreetAddress = order.StreetAddress,
            SubtotalAmount = order.SubtotalAmount,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ConfirmedAt = order.ConfirmedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            CancelReason = order.CancelReason,
            TrackingNumber = order.TrackingNumber,
            Carrier = order.Carrier,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            Notes = order.Notes,
            AllowedStatusTransitions = transitions ?? new List<string>(),
            Payment = order.Payment != null ? new AdminOrderDetailResponse.AdminPaymentInfo
            {
                PaymentId = order.Payment.PaymentId,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                Amount = order.Payment.Amount,
                ReceivedAmount = order.Payment.ReceivedAmount,
                PaymentReference = order.Payment.PaymentReference,
                TransactionId = order.Payment.TransactionId,
                PaymentDate = order.Payment.PaymentDate,
                ExpiredAt = order.Payment.ExpiredAt,
                Notes = order.Payment.Notes
            } : null,
            Items = order.OrderItems.Select(oi => new AdminOrderDetailResponse.AdminOrderItemResponse
            {
                OrderItemId = oi.OrderItemId,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                ProductSku = oi.ProductSku,
                ProductImageUrl = oi.ProductImageUrl,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Subtotal = oi.Subtotal,
                DiscountAmount = oi.DiscountAmount
            }).ToList()
        };

        return ApiResponse<AdminOrderDetailResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<AdminOrderDetailResponse>> UpdateOrderStatusAsync(
        int orderId, int adminUserId, AdminUpdateOrderStatusRequest request)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
            return ApiResponse<AdminOrderDetailResponse>.ErrorResponse($"Order with ID {orderId} not found.");

        var currentStatus = await _orderRepo.GetOrderStatusAsync(orderId);
        currentStatus ??= "PENDING";

        var newStatus = request.NewStatus.ToUpper();

        // Validate transition
        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(newStatus))
            return ApiResponse<AdminOrderDetailResponse>.ErrorResponse(
                $"Cannot transition from {currentStatus} to {newStatus}.");

        // Use transaction for complex status changes
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            switch (newStatus)
            {
                case "CONFIRMED":
                    await HandleConfirmOrder(order, adminUserId, request);
                    break;
                case "SHIPPED":
                    await HandleShipOrder(order, adminUserId, request);
                    break;
                case "DELIVERED":
                    await HandleDeliverOrder(order, orderId);
                    break;
                case "CANCELLED":
                    await HandleCancelOrder(order, adminUserId, currentStatus, request);
                    break;
            }

            // Update status
            await _orderRepo.SetOrderStatusAsync(orderId, newStatus);
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdateOrderAsync(order);

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return ApiResponse<AdminOrderDetailResponse>.ErrorResponse($"Failed to update order status: {ex.Message}");
        }

        return await GetAdminOrderDetailAsync(orderId);
    }

    public async Task<ApiResponse<bool>> UpdatePaymentStatusAsync(int orderId, UpdatePaymentStatusRequest request)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
            return ApiResponse<bool>.ErrorResponse($"Order with ID {orderId} not found.");

        if (order.Payment == null)
            return ApiResponse<bool>.ErrorResponse("Order has no payment record.");

        var validStatuses = new[] { "PENDING", "COMPLETED", "FAILED", "EXPIRED" };
        var newStatus = request.NewPaymentStatus.ToUpper();
        if (!validStatuses.Contains(newStatus))
            return ApiResponse<bool>.ErrorResponse($"Invalid payment status: {newStatus}. Valid: {string.Join(", ", validStatuses)}");

        await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, newStatus);

        if (!string.IsNullOrEmpty(request.Note))
        {
            order.Payment.Notes = request.Note;
            order.Payment.UpdatedAt = DateTime.UtcNow;
            await _orderRepo.UpdatePaymentAsync(order.Payment);
        }

        return ApiResponse<bool>.SuccessResponse(true, $"Payment status updated to {newStatus}.");
    }

    public async Task<ApiResponse<DashboardResponse>> GetDashboardAsync()
    {
        var statusCounts = await _orderRepo.GetOrderStatusCountsAsync();
        var totalOrders = statusCounts.Values.Sum();

        // Revenue: all delivered orders
        var totalRevenue = await _orderRepo.GetDeliveredRevenueAsync(DateTime.MinValue, DateTime.MaxValue);

        // Monthly revenue
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var monthlyRevenue = await _orderRepo.GetDeliveredRevenueAsync(monthStart, monthEnd);

        // Product stats
        var totalProducts = await _context.Products.CountAsync(p => p.IsDeleted != true);
        var activeProducts = await _context.Products.CountAsync(p => p.IsActive == true && p.IsDeleted != true);
        var lowStockProducts = await _context.Products.CountAsync(p => p.IsDeleted != true && (p.StockQuantity ?? 0) < 10 && (p.StockQuantity ?? 0) > 0);
        var outOfStockProducts = await _context.Products.CountAsync(p => p.IsDeleted != true && (p.StockQuantity ?? 0) == 0);

        // Customer stats (role_id for Customer - typically 2, but let's query by role_name)
        var customerRoleId = await _context.Roles.Where(r => r.RoleName == "Customer").Select(r => r.RoleId).FirstOrDefaultAsync();
        var totalCustomers = customerRoleId > 0 ? await _context.Users.CountAsync(u => u.RoleId == customerRoleId) : 0;
        var newCustomersThisMonth = customerRoleId > 0
            ? await _context.Users.CountAsync(u => u.RoleId == customerRoleId && u.CreatedAt >= monthStart)
            : 0;

        // Recent orders
        var recentOrders = await _orderRepo.GetRecentOrdersAsync(10);
        var recentOrderIds = recentOrders.Select(o => o.OrderId).ToList();
        var recentStatusMap = await _orderRepo.GetOrderStatusesBatchAsync(recentOrderIds);
        var recentPaymentMap = await _orderRepo.GetPaymentEnumsBatchAsync(recentOrderIds);

        var dashboard = new DashboardResponse
        {
            Orders = new DashboardResponse.OrderStats
            {
                TotalOrders = totalOrders,
                PendingOrders = statusCounts.GetValueOrDefault("PENDING", 0),
                ConfirmedOrders = statusCounts.GetValueOrDefault("CONFIRMED", 0),
                ShippedOrders = statusCounts.GetValueOrDefault("SHIPPED", 0),
                DeliveredOrders = statusCounts.GetValueOrDefault("DELIVERED", 0),
                CancelledOrders = statusCounts.GetValueOrDefault("CANCELLED", 0),
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue
            },
            Products = new DashboardResponse.ProductStats
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                LowStockProducts = lowStockProducts,
                OutOfStockProducts = outOfStockProducts
            },
            Customers = new DashboardResponse.CustomerStats
            {
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newCustomersThisMonth
            },
            RecentOrders = recentOrders.Select(o =>
            {
                recentStatusMap.TryGetValue(o.OrderId, out var status);
                recentPaymentMap.TryGetValue(o.OrderId, out var payment);
                return new DashboardResponse.RecentOrderDto
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    Status = status ?? "PENDING",
                    PaymentMethod = payment.Method,
                    CreatedAt = o.CreatedAt
                };
            }).ToList()
        };

        return ApiResponse<DashboardResponse>.SuccessResponse(dashboard);
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task HandleConfirmOrder(OrderHeader order, int adminUserId, AdminUpdateOrderStatusRequest request)
    {
        // Validate payment is not FAILED
        var paymentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(order.OrderId);
        if (paymentStatus == "FAILED")
            throw new InvalidOperationException("Cannot confirm order with FAILED payment.");

        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmedBy = adminUserId;

        // Update ProductInstance status to SOLD (if serial tracking)
        var orderItemIds = order.OrderItems.Select(oi => oi.OrderItemId).ToList();
        await _orderRepo.SetProductInstanceStatusByOrderItemIdsAsync(orderItemIds, "SOLD");
    }

    private async Task HandleShipOrder(OrderHeader order, int adminUserId, AdminUpdateOrderStatusRequest request)
    {
        if (string.IsNullOrEmpty(request.TrackingNumber))
            throw new InvalidOperationException("Tracking number is required for SHIPPED status.");

        order.ShippedAt = DateTime.UtcNow;
        order.ShippedBy = adminUserId;
        order.TrackingNumber = request.TrackingNumber;
        order.Carrier = request.Carrier;
    }

    private async Task HandleDeliverOrder(OrderHeader order, int orderId)
    {
        order.DeliveredAt = DateTime.UtcNow;

        // Auto-complete COD payment
        var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
        if (paymentMethod == "COD")
        {
            await _orderRepo.SetPaymentStatusByOrderIdAsync(orderId, "COMPLETED");
            if (order.Payment != null)
            {
                order.Payment.PaymentDate = DateTime.UtcNow;
                order.Payment.ReceivedAmount = order.Payment.Amount;
            }
        }

        // Auto-create warranties for products with warranty policies
        await _orderRepo.CreateWarrantiesForDeliveredOrderAsync(orderId);
    }

    private async Task HandleCancelOrder(OrderHeader order, int adminUserId, string currentStatus, AdminUpdateOrderStatusRequest request)
    {
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledBy = adminUserId;
        order.CancelReason = request.Note ?? "Cancelled by admin";

        // Restore stock for each order item
        foreach (var item in order.OrderItems)
        {
            await _orderRepo.IncrementStockAsync(item.ProductId, item.Quantity);
        }

        // Restore coupon usage if applicable
        if (order.CouponId.HasValue)
        {
            await _orderRepo.DecrementCouponUsedCountAsync(order.CouponId.Value);
        }

        // If was CONFIRMED, restore ProductInstance status to IN_STOCK
        if (currentStatus == "CONFIRMED")
        {
            var orderItemIds = order.OrderItems.Select(oi => oi.OrderItemId).ToList();
            await _orderRepo.SetProductInstanceStatusByOrderItemIdsAsync(orderItemIds, "IN_STOCK");
        }
    }

    public async Task<ApiResponse<List<DailyRevenueData>>> GetRevenueChartAsync(DateTime from, DateTime to, string? status = null)
    {
        var data = await _orderRepo.GetDailyRevenueAsync(from, to, status);
        return ApiResponse<List<DailyRevenueData>>.SuccessResponse(data);
    }
}
