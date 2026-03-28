using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationController(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "[Customer] Lấy danh sách thông báo")]
    public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId <= 0) return Unauthorized();

        var notifs = await _notificationRepository.GetUserNotificationsAsync(userId, page, pageSize);
        var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);

        var data = new
        {
            Items = notifs,
            UnreadCount = unreadCount
        };

        return Ok(ApiResponse<object>.SuccessResponse(data));
    }

    [HttpPut("{id}/read")]
    [SwaggerOperation(Summary = "[Customer] Đánh dấu 1 thông báo là đã đọc")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        if (userId <= 0) return Unauthorized();

        var success = await _notificationRepository.MarkAsReadAsync(id, userId);
        return success ? Ok(ApiResponse<object>.SuccessResponse(null, "Marked as read")) 
                       : BadRequest(ApiResponse<object>.ErrorResponse("Notification not found."));
    }

    [HttpPut("read-all")]
    [SwaggerOperation(Summary = "[Customer] Đánh dấu tất cả thông báo là đã đọc")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (userId <= 0) return Unauthorized();

        await _notificationRepository.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse<object>.SuccessResponse(null, "All marked as read"));
    }
}
