using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Payment.Request;
using MV.DomainLayer.DTOs.Payment.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;

    public PaymentController(IPaymentService paymentService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _configuration = configuration;
    }

    /// <summary>
    /// Webhook endpoint nhận thông báo giao dịch từ SePay.
    /// SePay sẽ POST đến endpoint này khi có giao dịch mới.
    /// </summary>
    [HttpPost("sepay-webhook")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "SePay Webhook - Nhận thông báo giao dịch từ SePay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SepayWebhook([FromBody] SepayWebhookRequest request)
    {
        // Verify webhook authentication from Authorization header
        // SePay sends: "Apikey YOUR_API_KEY" in the Authorization header
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var webhookSecret = _configuration["SePay:WebhookSecret"];

        if (!string.IsNullOrEmpty(webhookSecret))
        {
            var providedSecret = authHeader?
                .Replace("Apikey ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (providedSecret != webhookSecret)
            {
                return Unauthorized(new { success = false, message = "Invalid webhook secret" });
            }
        }

        var result = await _paymentService.ProcessWebhookAsync(request);

        // SePay requires: { "success": true } with HTTP 200
        return Ok(new { success = true });
    }

    /// <summary>
    /// Lấy trạng thái thanh toán của đơn hàng (cho frontend polling)
    /// </summary>
    [HttpGet("{orderId}/status")]
    [Authorize]
    [SwaggerOperation(Summary = "Lấy trạng thái thanh toán đơn hàng")]
    [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(int orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var result = await _paymentService.GetPaymentStatusAsync(orderId, userId.Value, isAdmin);

        if (!result.Success)
        {
            if (result.Message.Contains("không tồn tại"))
                return NotFound(result);
            if (result.Message.Contains("Unauthorized"))
                return StatusCode(403, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Admin xác nhận thanh toán thủ công (cho trường hợp webhook SePay lỗi)
    /// </summary>
    [HttpPut("{orderId}/verify")]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "[Admin] Xác nhận thanh toán thủ công")]
    [ProducesResponseType(typeof(ApiResponse<PaymentStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPaymentManually(int orderId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var result = await _paymentService.VerifyPaymentManuallyAsync(orderId, userId.Value);

        if (!result.Success)
        {
            if (result.Message.Contains("không tồn tại"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    // ==================== PRIVATE HELPERS ====================

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;
        return userId;
    }
}
