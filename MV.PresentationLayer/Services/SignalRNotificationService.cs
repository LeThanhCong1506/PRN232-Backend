using Microsoft.AspNetCore.SignalR;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.Interfaces;
using MV.PresentationLayer.Hubs;

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
            Timestamp = dbNotif?.CreatedAt ?? DateTimeHelper.VietnamNow(),
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "CartUpdated",
            IsRead = false,
            LinkUrl = dbNotif?.LinkUrl
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
            Timestamp = dbNotif?.CreatedAt ?? DateTimeHelper.VietnamNow(),
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "OrderStatusChanged",
            IsRead = false,
            LinkUrl = dbNotif?.LinkUrl
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
            Timestamp = dbNotif?.CreatedAt ?? DateTimeHelper.VietnamNow(),
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "PaymentConfirmed",
            IsRead = false,
            LinkUrl = dbNotif?.LinkUrl
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
            Timestamp = dbNotif?.CreatedAt ?? DateTimeHelper.VietnamNow(),
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "PaymentExpired",
            IsRead = false,
            LinkUrl = dbNotif?.LinkUrl
        };

        await SendToUserAsync(userId, "PaymentExpired", data);
        _logger.LogInformation("PaymentExpired sent to user {UserId}: Order {OrderNumber}", userId, orderNumber);
    }

    public async Task SendNotificationAsync(int userId, string eventType, object data)
    {
        await SendToUserAsync(userId, eventType, data);
    }

    public async Task SendWarrantyClaimStatusChangedAsync(int userId, int claimId, string productName, string newStatus)
    {
        var statusMessages = new Dictionary<string, string>
        {
            { "APPROVED", $"Your warranty claim for '{productName}' has been approved and is being processed." },
            { "REJECTED", $"Your warranty claim for '{productName}' has been rejected. Please check the details." },
            { "RESOLVED", $"Your warranty claim for '{productName}' has been resolved successfully!" },
            { "UNRESOLVED", $"Your warranty claim for '{productName}' could not be resolved. Device has been returned." },
        };

        var msg = statusMessages.TryGetValue(newStatus, out var m) ? m : $"Your warranty claim status changed to {newStatus}.";
        var title = $"Warranty Claim {newStatus.Substring(0, 1)}{newStatus.Substring(1).ToLower()}";

        var dbNotif = await SaveNotificationAsync(userId, "WarrantyClaimStatus", title, msg, $"/warranties/claims");

        var data = new
        {
            Id = dbNotif?.NotificationId ?? 0,
            ClaimId = claimId,
            ProductName = productName,
            Status = newStatus,
            Timestamp = dbNotif?.CreatedAt ?? DateTimeHelper.VietnamNow(),
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "WarrantyClaimStatus",
            IsRead = false,
            LinkUrl = dbNotif?.LinkUrl
        };

        await SendToUserAsync(userId, "WarrantyClaimStatus", data);
        _logger.LogInformation("WarrantyClaimStatus sent to user {UserId}: Claim {ClaimId} -> {Status}", userId, claimId, newStatus);
    }

    public async Task SendNewChatMessageAsync(int userId, string senderName, string messagePreview)
    {
        var dbNotif = await SaveNotificationAsync(userId, "NewChatMessage", $"New message from {senderName}",
            messagePreview.Length > 80 ? messagePreview.Substring(0, 77) + "..." : messagePreview,
            "/chat");

        var data = new
        {
            Id = dbNotif?.NotificationId ?? 0,
            SenderName = senderName,
            MessagePreview = messagePreview,
            Timestamp = dbNotif?.CreatedAt ?? DateTimeHelper.VietnamNow(),
            Title = dbNotif?.Title,
            Message = dbNotif?.Message,
            Type = "NewChatMessage",
            IsRead = false,
            LinkUrl = dbNotif?.LinkUrl
        };

        await SendToUserAsync(userId, "NewChatMessage", data);
        _logger.LogInformation("NewChatMessage notification sent to user {UserId} from {SenderName}", userId, senderName);
    }

    private async Task<Notification?> SaveNotificationAsync(int userId, string type, string title, string message, string? linkUrl = null)
    {
        _logger.LogInformation("[NotifSave] Attempting: UserId={UserId}, Type={Type}, Title={Title}", userId, type, title);
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
                CreatedAt = DateTimeHelper.VietnamNow()
            };
            var saved = await repo.CreateAsync(newNotif);
            _logger.LogInformation("[NotifSave] SUCCESS: NotificationId={NotifId} for UserId={UserId}", saved.NotificationId, userId);
            return saved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NotifSave] FAILED for UserId={UserId}, Type={Type}. Error: {Msg}. Inner: {Inner}",
                userId, type, ex.Message, ex.InnerException?.Message ?? "none");
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
