using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using MV.PresentationLayer.Hubs;

namespace MV.PresentationLayer.Services;

/// <summary>
/// Gửi notification qua cả SignalR (realtime) và FCM (push mobile).
/// </summary>
public class HybridNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IFcmService _fcmService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HybridNotificationService> _logger;

    public HybridNotificationService(
        IHubContext<NotificationHub> hubContext,
        IFcmService fcmService,
        IServiceScopeFactory scopeFactory,
        ILogger<HybridNotificationService> logger)
    {
        _hubContext = hubContext;
        _fcmService = fcmService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SendCartUpdatedAsync(int userId, int totalItems)
    {
        var data = new
        {
            TotalItems = totalItems,
            Timestamp = DateTime.UtcNow
        };

        await SendToUserAsync(userId, "CartUpdated", data);

        await SendFcmToUserAsync(userId,
            "Giỏ hàng",
            $"Giỏ hàng có {totalItems} sản phẩm",
            new Dictionary<string, string>
            {
                ["eventType"] = "CartUpdated",
                ["totalItems"] = totalItems.ToString()
            });

        _logger.LogInformation("CartUpdated sent to user {UserId}: {TotalItems} items", userId, totalItems);
    }

    public async Task SendOrderStatusChangedAsync(int userId, int orderId, string orderNumber, string newStatus)
    {
        var data = new
        {
            OrderId = orderId,
            OrderNumber = orderNumber,
            Status = newStatus,
            Timestamp = DateTime.UtcNow
        };

        await SendToUserAsync(userId, "OrderStatusChanged", data);

        await SendFcmToUserAsync(userId,
            "Cập nhật đơn hàng",
            $"Đơn #{orderNumber} → {newStatus}",
            new Dictionary<string, string>
            {
                ["eventType"] = "OrderStatusChanged",
                ["orderId"] = orderId.ToString(),
                ["orderNumber"] = orderNumber,
                ["status"] = newStatus
            });

        _logger.LogInformation("OrderStatusChanged sent to user {UserId}: Order {OrderNumber} → {Status}", userId, orderNumber, newStatus);
    }

    public async Task SendPaymentConfirmedAsync(int userId, int orderId, string orderNumber, decimal amount)
    {
        var data = new
        {
            OrderId = orderId,
            OrderNumber = orderNumber,
            Amount = amount,
            Status = "COMPLETED",
            Timestamp = DateTime.UtcNow
        };

        await SendToUserAsync(userId, "PaymentConfirmed", data);

        await SendFcmToUserAsync(userId,
            "Thanh toán thành công",
            $"Đơn #{orderNumber} đã thanh toán {amount:N0}đ",
            new Dictionary<string, string>
            {
                ["eventType"] = "PaymentConfirmed",
                ["orderId"] = orderId.ToString(),
                ["orderNumber"] = orderNumber,
                ["amount"] = amount.ToString("F0")
            });

        _logger.LogInformation("PaymentConfirmed sent to user {UserId}: Order {OrderNumber}, Amount {Amount}", userId, orderNumber, amount);
    }

    public async Task SendPaymentExpiredAsync(int userId, int orderId, string orderNumber)
    {
        var data = new
        {
            OrderId = orderId,
            OrderNumber = orderNumber,
            Status = "EXPIRED",
            Timestamp = DateTime.UtcNow
        };

        await SendToUserAsync(userId, "PaymentExpired", data);

        await SendFcmToUserAsync(userId,
            "Hết hạn thanh toán",
            $"Đơn #{orderNumber} đã hết hạn thanh toán",
            new Dictionary<string, string>
            {
                ["eventType"] = "PaymentExpired",
                ["orderId"] = orderId.ToString(),
                ["orderNumber"] = orderNumber
            });

        _logger.LogInformation("PaymentExpired sent to user {UserId}: Order {OrderNumber}", userId, orderNumber);
    }

    public async Task SendNotificationAsync(int userId, string eventType, object data)
    {
        await SendToUserAsync(userId, eventType, data);

        await SendFcmToUserAsync(userId,
            eventType,
            eventType,
            new Dictionary<string, string>
            {
                ["eventType"] = eventType
            });
    }

    private async Task SendToUserAsync(int userId, string eventType, object data)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}").SendAsync(eventType, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SignalR {EventType} to user {UserId}", eventType, userId);
        }
    }

    private async Task SendFcmToUserAsync(int userId, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StemDbContext>();

            var fcmToken = await dbContext.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.FcmToken)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(fcmToken))
            {
                await _fcmService.SendAsync(fcmToken, title, body, data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM to user {UserId}", userId);
        }
    }
}
