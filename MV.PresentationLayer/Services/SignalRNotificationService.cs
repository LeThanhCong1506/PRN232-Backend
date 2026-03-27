using Microsoft.AspNetCore.SignalR;
using MV.InfrastructureLayer.Interfaces;
using MV.PresentationLayer.Hubs;

using MV.DomainLayer.Entities;

namespace MV.PresentationLayer.Services;

/// <summary>
/// Implementation of INotificationService using SignalR and DB Persistence.
/// </summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _hubContext = hubContext;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task SendCartUpdatedAsync(int userId, int totalItems)
    {
        var dbNotif = await SaveNotificationAsync(userId, "CartUpdated", "Cart Updated", $"Your cart now has {totalItems} items.", "/cart");
        
        var data = new
        {
            Id = dbNotif?.NotificationId ?? 0,
            TotalItems = totalItems,
            Timestamp = dbNotif?.CreatedAt ?? DateTime.UtcNow,
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "CartUpdated",
            IsRead = false
        };

        await SendToUserAsync(userId, "CartUpdated", data);
        _logger.LogInformation("CartUpdated sent to user {UserId}: {TotalItems} items", userId, totalItems);
    }

    public async Task SendOrderStatusChangedAsync(int userId, int orderId, string orderNumber, string newStatus)
    {
        var dbNotif = await SaveNotificationAsync(userId, "OrderStatusChanged", "Order Status Update", $"Your order #{orderNumber} status changed to {newStatus}.", $"/orders/{orderId}");

        var data = new
        {
            Id = dbNotif?.NotificationId ?? 0,
            OrderId = orderId,
            OrderNumber = orderNumber,
            Status = newStatus,
            Timestamp = dbNotif?.CreatedAt ?? DateTime.UtcNow,
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "OrderStatusChanged",
            IsRead = false
        };

        await SendToUserAsync(userId, "OrderStatusChanged", data);
        _logger.LogInformation("OrderStatusChanged sent to user {UserId}: Order {OrderNumber} → {Status}", userId, orderNumber, newStatus);
    }

    public async Task SendPaymentConfirmedAsync(int userId, int orderId, string orderNumber, decimal amount)
    {
        var dbNotif = await SaveNotificationAsync(userId, "PaymentConfirmed", "Payment Received", $"Payment of {amount:C} for order #{orderNumber} confirmed.", $"/orders/{orderId}");

        var data = new
        {
            Id = dbNotif?.NotificationId ?? 0,
            OrderId = orderId,
            OrderNumber = orderNumber,
            Amount = amount,
            Status = "COMPLETED",
            Timestamp = dbNotif?.CreatedAt ?? DateTime.UtcNow,
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "PaymentConfirmed",
            IsRead = false
        };

        await SendToUserAsync(userId, "PaymentConfirmed", data);
        _logger.LogInformation("PaymentConfirmed sent to user {UserId}: Order {OrderNumber}, Amount {Amount}", userId, orderNumber, amount);
    }

    public async Task SendPaymentExpiredAsync(int userId, int orderId, string orderNumber)
    {
        var dbNotif = await SaveNotificationAsync(userId, "PaymentExpired", "Payment Expired", $"The payment window for order #{orderNumber} has expired.", $"/orders/{orderId}");

        var data = new
        {
            Id = dbNotif?.NotificationId ?? 0,
            OrderId = orderId,
            OrderNumber = orderNumber,
            Status = "EXPIRED",
            Timestamp = dbNotif?.CreatedAt ?? DateTime.UtcNow,
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "PaymentExpired",
            IsRead = false
        };

        await SendToUserAsync(userId, "PaymentExpired", data);
        _logger.LogInformation("PaymentExpired sent to user {UserId}: Order {OrderNumber}", userId, orderNumber);
    }

    public async Task SendNotificationAsync(int userId, string eventType, object data)
    {
        await SendToUserAsync(userId, eventType, data);
    }

    private async Task<Notification?> SaveNotificationAsync(int userId, string type, string title, string message, string? linkUrl = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var newNotif = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            return await repo.CreateAsync(newNotif);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save notification to DB for user {UserId}", userId);
            return null; // Don't crash SignalR if DB fails
        }
    }

    private async Task SendToUserAsync(int userId, string eventType, object data)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}").SendAsync(eventType, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {EventType} to user {UserId}", eventType, userId);
        }
    }
}
