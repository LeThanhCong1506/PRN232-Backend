using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.InfrastructureLayer.DBContext;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MV.PresentationLayer.Hubs;

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
    private readonly ILogger<ChatController> _logger;

    public ChatController(StemDbContext context, IHubContext<ChatHub> hubContext, ILogger<ChatController> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
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
                SenderName = m.Sender.Username,
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
                SenderName = m.Sender.Username,
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

    public class SendMessageRequest
    {
        public int? ReceiverId { get; set; }
        public string Content { get; set; } = null!;
    }

    /// <summary>
    /// REST API fallback cho tính năng chat khi client không kết nối được SignalR trực tiếp
    /// </summary>
    [HttpPost("send")]
    [SwaggerOperation(Summary = "Send a chat message via REST API (Fallback)")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var senderId = GetUserId();
        if (senderId == 0 || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid message content or user"));

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = role == "Admin" || role == "Staff";
        var senderName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        var message = new DomainLayer.Entities.ChatMessage
        {
            SenderId = senderId,
            ReceiverId = isAdmin ? request.ReceiverId : null,
            Content = request.Content.Trim(),
            IsFromAdmin = isAdmin,
            SentAt = DateTime.Now,
            IsRead = false
        };

        try
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            _logger.LogInformation("HTTP Fallback: Saved message {MessageId} from {SenderId} to {ReceiverId}", message.MessageId, senderId, request.ReceiverId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP Fallback: Failed to save message from {SenderId}", senderId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Could not save message to database."));
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

        // Push real-time to active SignalR connections
        try
        {
            if (isAdmin && request.ReceiverId.HasValue)
            {
                // Admin -> Customer
                await _hubContext.Clients.Group($"chat_user_{request.ReceiverId.Value}").SendAsync("ReceiveMessage", messageDto);
                // Notification to self (unnecessary if REST returns the dto, but keeps parity with Hub)
            }
            else
            {
                // Customer -> Admins
                await _hubContext.Clients.Group("chat_admins").SendAsync("ReceiveMessage", messageDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HTTP Fallback: Failed to broadcast to SignalR groups.");
        }

        return Ok(ApiResponse<object>.SuccessResponse(messageDto, "Message sent successfully"));
    }

    private int GetUserId()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }
}
