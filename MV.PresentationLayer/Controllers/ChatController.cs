using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;
using MV.PresentationLayer.Hubs;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Chat API - Lấy lịch sử tin nhắn.
/// Real-time chat xử lý qua SignalR ChatHub (/hubs/chat).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly StemDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ChatController> _logger;

    private readonly ICloudinaryService _cloudinaryService;

    public ChatController(StemDbContext context, IHubContext<ChatHub> hubContext,
        INotificationService notificationService, ILogger<ChatController> logger,
        ICloudinaryService cloudinaryService)
    {
        _context = context;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
    }

    private static readonly HashSet<string> _allowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
    };

    /// <summary>
    /// Upload ảnh cho chat lên Cloudinary, trả về URL để gửi trong tin nhắn.
    /// </summary>
    [HttpPost("upload-image")]
    [SwaggerOperation(Summary = "Upload a chat image to Cloudinary")]
    public async Task<IActionResult> UploadImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest(ApiResponse<object>.ErrorResponse("No image provided"));

        if (!_allowedImageTypes.Contains(image.ContentType))
            return BadRequest(ApiResponse<object>.ErrorResponse("Only JPEG, PNG, GIF, and WebP images are allowed"));

        const long maxSize = 5 * 1024 * 1024; // 5 MB
        if (image.Length > maxSize)
            return BadRequest(ApiResponse<object>.ErrorResponse("Image must be under 5 MB"));

        try
        {
            var (imageUrl, _) = await _cloudinaryService.UploadImageAsync(image, "chat");
            return Ok(ApiResponse<object>.SuccessResponse(new { imageUrl }, "Image uploaded"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload chat image to Cloudinary");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to upload image"));
        }
    }

    /// <summary>
    /// DTO for REST send message fallback
    /// </summary>
    public class SendMessageDto
    {
        public int? ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// REST fallback: gửi tin nhắn khi SignalR chưa kết nối được.
    /// Mirrors logic của ChatHub.SendMessage.
    /// </summary>
    [HttpPost("send")]
    [SwaggerOperation(Summary = "Send chat message (REST fallback when SignalR is unavailable)")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(ApiResponse<object>.ErrorResponse("Message content is required"));

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = role == "Admin" || role == "Staff";
        var senderName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var message = new ChatMessage
        {
            SenderId = userId,
            ReceiverId = isAdmin ? dto.ReceiverId : null,
            Content = dto.Content.Trim(),
            IsFromAdmin = isAdmin,
            SentAt = DateTimeHelper.VietnamNow(),
            IsRead = false
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

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

        // Broadcast via SignalR hub context (even though client used REST)
        if (isAdmin && dto.ReceiverId.HasValue)
        {
            await _hubContext.Clients.Group($"chat_user_{dto.ReceiverId.Value}")
                .SendAsync("ReceiveMessage", messageDto);

            // Gửi notification bell cho customer — giống ChatHub.SendMessage
            try
            {
                await _notificationService.SendNewChatMessageAsync(dto.ReceiverId.Value, senderName, dto.Content.Trim());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send chat notification for receiver {ReceiverId}", dto.ReceiverId.Value);
            }
        }
        else
        {
            await _hubContext.Clients.Group("chat_admins")
                .SendAsync("ReceiveMessage", messageDto);
        }

        return Ok(ApiResponse<object>.SuccessResponse(messageDto, "Message sent"));
    }

    /// <summary>
    /// Lấy lịch sử tin nhắn của user hiện tại
    /// </summary>
    /// <param name="pageNumber">Trang (mặc định: 1)</param>
    /// <param name="pageSize">Số tin nhắn mỗi trang (mặc định: 50)</param>
    [HttpGet("history")]
    [SwaggerOperation(Summary = "Get chat history for current user")]
    public async Task<IActionResult> GetChatHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = role == "Admin" || role == "Staff";

        IQueryable<DomainLayer.Entities.ChatMessage> query;

        if (isAdmin)
        {
            // Admin thấy tất cả tin nhắn
            query = _context.ChatMessages
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt);
        }
        else
        {
            // Customer chỉ thấy tin nhắn của mình (gửi hoặc nhận)
            query = _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.SentAt);
        }

        var totalCount = await query.CountAsync();
        var messages = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.MessageId,
                m.SenderId,
                SenderName = m.Sender != null ? m.Sender.Username : "Unknown",
                m.ReceiverId,
                m.Content,
                m.IsFromAdmin,
                m.SentAt,
                m.IsRead
            })
            .ToListAsync();

        var pagedResponse = new PagedResponse<object>(
            messages.Cast<object>().ToList(),
            pageNumber, pageSize, totalCount);

        return Ok(ApiResponse<PagedResponse<object>>.SuccessResponse(pagedResponse));
    }

    /// <summary>
    /// [Admin] Lấy danh sách users đang chat (conversations)
    /// </summary>
    [HttpGet("conversations")]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "Get list of chat conversations (Admin only)")]
    public async Task<IActionResult> GetConversations()
    {
        // Lấy danh sách unique users đã gửi tin nhắn (không phải admin)
        var conversations = await _context.ChatMessages
            .Where(m => !m.IsFromAdmin)
            .GroupBy(m => m.SenderId)
            .Select(g => new
            {
                UserId = g.Key,
                UserName = g.First().Sender.Username,
                LastMessage = g.OrderByDescending(m => m.SentAt).First().Content,
                LastMessageAt = g.Max(m => m.SentAt),
                UnreadCount = g.Count(m => !m.IsRead)
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        return Ok(ApiResponse<object>.SuccessResponse(conversations));
    }

    /// <summary>
    /// [Admin] Lấy lịch sử chat với 1 user cụ thể
    /// </summary>
    [HttpGet("history/{targetUserId}")]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "Get chat history with a specific user (Admin only)")]
    public async Task<IActionResult> GetChatHistoryWithUser(int targetUserId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.SenderId == targetUserId || m.ReceiverId == targetUserId)
            .OrderByDescending(m => m.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.MessageId,
                m.SenderId,
                SenderName = m.Sender != null ? m.Sender.Username : "Unknown",
                m.ReceiverId,
                m.Content,
                m.IsFromAdmin,
                m.SentAt,
                m.IsRead
            })
            .ToListAsync();

        var totalCount = await _context.ChatMessages
            .Where(m => m.SenderId == targetUserId || m.ReceiverId == targetUserId)
            .CountAsync();

        var pagedResponse = new PagedResponse<object>(
            messages.Cast<object>().ToList(),
            pageNumber, pageSize, totalCount);

        return Ok(ApiResponse<PagedResponse<object>>.SuccessResponse(pagedResponse));
    }

    /// <summary>
    /// [Admin] Mark all messages from a user as read
    /// </summary>
    [HttpPost("mark-read/{userId}")]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "Mark all messages from a user as read (Admin only)")]
    public async Task<IActionResult> MarkAsRead(int userId)
    {
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.SenderId == userId && !m.IsFromAdmin && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
        }

        await _context.SaveChangesAsync();
        
        // Notify the customer that the admin has read their messages
        await _hubContext.Clients.Group($"chat_user_{userId}").SendAsync("MessagesRead", userId);

        return Ok(ApiResponse<object>.SuccessResponse(new { markedCount = unreadMessages.Count }, "Messages marked as read"));
    }

    /// <summary>
    /// [Customer] Get unread message count (messages from admin that customer hasn't read)
    /// </summary>
    [HttpGet("unread-count")]
    [SwaggerOperation(Summary = "Get unread message count for current user")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = role == "Admin" || role == "Staff";

        int count;
        if (isAdmin)
        {
            // Admin: count unread messages from all customers
            count = await _context.ChatMessages
                .Where(m => !m.IsFromAdmin && !m.IsRead)
                .CountAsync();
        }
        else
        {
            // Customer: count unread messages from admin to them
            count = await _context.ChatMessages
                .Where(m => m.IsFromAdmin && m.ReceiverId == userId && !m.IsRead)
                .CountAsync();
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { unreadCount = count }));
    }

    /// <summary>
    /// [Customer] Mark admin messages as read
    /// </summary>
    [HttpPost("mark-read")]
    [SwaggerOperation(Summary = "Mark admin messages as read (Customer)")]
    public async Task<IActionResult> MarkMyMessagesAsRead()
    {
        var userId = GetUserId();
        if (userId == 0) return Unauthorized();

        var unreadMessages = await _context.ChatMessages
            .Where(m => m.IsFromAdmin && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
        }

        await _context.SaveChangesAsync();

        // Notify admins that the customer has read their messages
        await _hubContext.Clients.Group("chat_admins").SendAsync("MessagesRead", userId);

        return Ok(ApiResponse<object>.SuccessResponse(new { markedCount = unreadMessages.Count }));
    }

    private int GetUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }
}
