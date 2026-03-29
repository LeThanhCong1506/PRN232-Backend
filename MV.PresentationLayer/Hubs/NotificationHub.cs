using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MV.PresentationLayer.Hubs;

/// <summary>
/// SignalR Hub cho realtime notifications.
/// Client connect và tự động join group theo userId.
/// Events: CartUpdated, OrderStatusChanged, PaymentConfirmed, PaymentExpired
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
            
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to NotificationHub. ConnectionId: {ConnectionId}", userIdStr, Context.ConnectionId);

            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? Context.User?.FindFirst("role")?.Value;

            if (role == "Admin" || role == "Staff")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin_notifications");
                _logger.LogInformation("User {UserId} joined admin_notifications group.", userIdStr);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
            
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from NotificationHub. ConnectionId: {ConnectionId}", userIdStr, Context.ConnectionId);

            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? Context.User?.FindFirst("role")?.Value;

            if (role == "Admin" || role == "Staff")
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin_notifications");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
