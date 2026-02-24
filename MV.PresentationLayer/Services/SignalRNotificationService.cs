using Microsoft.AspNetCore.SignalR;
using MV.InfrastructureLayer.Interfaces;
using MV.PresentationLayer.Hubs;

namespace MV.PresentationLayer.Services;

/// <summary>
/// Implementation of INotificationService using SignalR.
/// Gửi realtime events tới client qua SignalR Hub.
/// </summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
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
        _logger.LogInformation("PaymentExpired sent to user {UserId}: Order {OrderNumber}", userId, orderNumber);
    }

    public async Task SendNotificationAsync(int userId, string eventType, object data)
    {
        await SendToUserAsync(userId, eventType, data);
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
