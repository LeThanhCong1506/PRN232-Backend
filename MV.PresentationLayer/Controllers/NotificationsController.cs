using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.InfrastructureLayer.Interfaces;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// [TEST] Send a test notification to the current user via SignalR
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _notificationService.SendOrderStatusChangedAsync(
            userId, 
            999, 
            "TEST-ORDER-001", 
            "CONFIRMED"
        );

        return Ok(new { success = true, message = "Test notification sent via SignalR!" });
    }

    /// <summary>
    /// [TEST] Send a test payment notification
    /// </summary>
    [HttpPost("test/payment")]
    public async Task<IActionResult> SendTestPaymentNotification()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _notificationService.SendPaymentConfirmedAsync(
            userId,
            999,
            "TEST-ORDER-001",
            250000
        );

        return Ok(new { success = true, message = "Test payment notification sent!" });
    }
}
