using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using System.Security.Claims;

namespace MV.PresentationLayer.Hubs;

/// <summary>
/// SignalR Hub cho real-time chat giữa User và Admin/Store.
/// Client methods: ReceiveMessage, MessageRead
/// Server methods: SendMessage, MarkAsRead
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly StemDbContext _context;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(StemDbContext context, ILogger<ChatHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_user_{userId}");

            // Admin/Staff join admin group
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Admin" || role == "Staff")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "chat_admins");
            }

            _logger.LogInformation("User {UserId} ({Role}) connected to ChatHub", userId, role);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_user_{userId}");

            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Admin" || role == "Staff")
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "chat_admins");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client gọi method này để gửi tin nhắn.
    /// Customer gửi → Admin group nhận.
    /// Admin gửi → Customer cụ thể nhận.
    /// </summary>
    public async Task SendMessage(int? receiverId, string content)
    {
        var senderId = GetUserId();
        if (senderId == 0 || string.IsNullOrWhiteSpace(content)) return;

        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = role == "Admin" || role == "Staff";
        var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var message = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = isAdmin ? receiverId : null,
            Content = content.Trim(),
            IsFromAdmin = isAdmin,
            SentAt = DateTime.Now,
            IsRead = false
        };

        try
        {
            _logger.LogInformation("Attempting to save message to DB. SenderId: {SenderId}, Content: {Content}", senderId, content);
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully saved message {MessageId} to DB", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FAILED to save message to DB. SenderId: {SenderId}, Content: {Content}", senderId, content);
            throw; // Re-throw to inform SignalR client
        }

        var messageDto = new
        {
            message.MessageId,
            message.SenderId,
            SenderName = senderName,
            message.ReceiverId,
            message.Content,
            message.IsFromAdmin,
            message.SentAt,
            message.IsRead
        };

        if (isAdmin && receiverId.HasValue)
        {
            // Admin → gửi cho customer cụ thể
            await Clients.Group($"chat_user_{receiverId.Value}").SendAsync("ReceiveMessage", messageDto);
            // Cũng gửi lại cho chính admin (confirm)
            await Clients.Caller.SendAsync("ReceiveMessage", messageDto);
        }
        else
        {
            // Customer → gửi cho tất cả admin
            await Clients.Group("chat_admins").SendAsync("ReceiveMessage", messageDto);
            // Cũng gửi lại cho chính customer (confirm)
            await Clients.Caller.SendAsync("ReceiveMessage", messageDto);
        }

        _logger.LogInformation("Message sent: {SenderId} → {ReceiverId}, IsAdmin={IsAdmin}",
            senderId, receiverId, isAdmin);
    }

    /// <summary>
    /// Đánh dấu tin nhắn đã đọc
    /// </summary>
    public async Task MarkAsRead(int messageId)
    {
        var userId = GetUserId();
        var message = await _context.ChatMessages.FindAsync(messageId);
        if (message == null) return;

        // Chỉ người nhận mới được đánh dấu đã đọc
        if (message.ReceiverId == userId || (!message.IsFromAdmin && message.ReceiverId == null))
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();

            // Notify sender
            await Clients.Group($"chat_user_{message.SenderId}").SendAsync("MessageRead", messageId);
        }
    }

    private int GetUserId()
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }
}
