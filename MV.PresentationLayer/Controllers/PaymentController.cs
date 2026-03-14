using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Payment.Request;
using MV.DomainLayer.DTOs.Payment.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Enums;
using MV.InfrastructureLayer.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderRepository _orderRepo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, IOrderRepository orderRepo, IConfiguration configuration, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _orderRepo = orderRepo;
        _configuration = configuration;
        _logger = logger;
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
        try
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
        catch (Exception ex)
        {
            // Always return 200 to SePay to prevent infinite retries
            // Log the error for debugging
            _logger.LogError(ex, "Webhook endpoint error");
            return Ok(new { success = true });
        }
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

    /// <summary>
    /// Endpoint dành riêng cho trang Checkout HTML để polling trạng thái thanh toán mà không cần JWT token.
    /// Giao diện HTML tự host không có token, nên gọi endpoint này để biết khi nào giao dịch thành công.
    /// </summary>
    [HttpGet("{orderId}/poll-status")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)] // Ẩn khỏi Swagger
    public async Task<IActionResult> PollPaymentStatus(int orderId)
    {
        var paymentStatus = await _paymentService.CheckAndGetPaymentStatusAsync(orderId);
        return Ok(new { status = paymentStatus });
    }

    // ==================== SEPAY CHECKOUT REDIRECT ====================

    /// <summary>
    /// Redirect đến trang thanh toán SePay.
    /// Endpoint này render HTML form ẩn rồi auto-submit POST đến SePay Payment Gateway.
    /// Frontend chỉ cần mở URL này (window.location hoặc window.open).
    /// FE truyền successUrl, errorUrl, cancelUrl qua query params để linh hoạt thay đổi.
    /// Ví dụ: /api/Payment/30/checkout?successUrl=http://localhost:3000/payment/success&amp;errorUrl=...&amp;cancelUrl=...
    /// </summary>
    [HttpGet("{orderId}/checkout")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Redirect đến trang thanh toán SePay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RedirectToSepayCheckout(
        int orderId,
        [FromQuery] string? successUrl,
        [FromQuery] string? errorUrl,
        [FromQuery] string? cancelUrl)
    {
        var order = await _orderRepo.GetOrderByIdAsync(orderId);
        if (order == null)
            return NotFound("Đơn hàng không tồn tại");

        var payment = order.Payment;
        if (payment == null)
            return NotFound("Không tìm thấy thông tin thanh toán");

        // Check payment method
        var paymentMethod = await _orderRepo.GetPaymentMethodByOrderIdAsync(orderId);
        if (paymentMethod != PaymentMethodEnum.SEPAY.ToString())
            return BadRequest("Đơn hàng này không sử dụng phương thức thanh toán SEPAY");

        // Check payment status
        var paymentStatus = await _orderRepo.GetPaymentStatusByOrderIdAsync(orderId);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successParams = $"order_invoice_number={Uri.EscapeDataString(order.OrderNumber)}";
        if (!string.IsNullOrEmpty(successUrl)) successParams += $"&redirectUrl={Uri.EscapeDataString(successUrl)}";
        var backendSuccessUrl = $"{baseUrl}/api/Payment/callback/success?{successParams}";

        // If payment is completed, redirect to success
        if (paymentStatus == PaymentStatusEnum.COMPLETED.ToString())
        {
            return Redirect(backendSuccessUrl);
        }

        var finalCancelUrl = cancelUrl ?? "http://localhost:3000/payment/cancel";

        // Check expired - lazy expiry: cập nhật DB nếu đã hết hạn
        if (paymentStatus == PaymentStatusEnum.EXPIRED.ToString() ||
            (payment.ExpiredAt.HasValue && payment.ExpiredAt.Value < DateTime.Now))
        {
            await _paymentService.CheckAndGetPaymentStatusAsync(orderId);
            return Content($@"
                <html><head><meta http-equiv='refresh' content='3;url={finalCancelUrl}'></head>
                <body style='font-family: Arial; text-align: center; padding: 50px;'>
                    <h2 style='color: red;'>Đơn hàng đã hết hạn thanh toán!</h2>
                    <p>Đang chuyển về trang chủ...</p>
                </body></html>", "text/html");
        }

        // Build self-hosted checkout UI
        var sepayConfig = _configuration.GetSection("SePay");
        var bankName = sepayConfig["BankName"];
        var accountNumber = sepayConfig["AccountNumber"];
        var accountName = sepayConfig["AccountName"];

        var remainingSeconds = payment.ExpiredAt.HasValue ? Math.Max(0, (int)(payment.ExpiredAt.Value - DateTime.Now).TotalSeconds) : 0;
        var minutes = remainingSeconds / 60;
        var seconds = remainingSeconds % 60;

        var html = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thanh toán đơn hàng {order.OrderNumber}</title>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>
    <style>
        body {{ 
            font-family: 'Inter', sans-serif; 
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%); 
            margin: 0; 
            padding: 20px; 
            display: flex; 
            justify-content: center; 
            align-items: center; 
            min-height: 100vh; 
        }}
        .card {{ 
            background: white; 
            border-radius: 20px; 
            box-shadow: 0 10px 30px rgba(0,0,0,0.1); 
            padding: 40px 30px; 
            max-width: 400px; 
            width: 100%; 
            text-align: center; 
            position: relative;
            overflow: hidden;
            box-sizing: border-box;
        }}
        .card::before {{
            content: '';
            position: absolute;
            top: 0; left: 0; right: 0; height: 6px;
            background: linear-gradient(90deg, #3498db, #2ecc71);
        }}
        h2 {{ color: #1e293b; margin-top: 0; font-size: 24px; font-weight: 700; }}
        .amount-subtitle {{ color: #64748b; font-size: 14px; margin-bottom: 5px; }}
        .amount {{ font-size: 32px; color: #3498db; font-weight: 700; margin: 0 0 20px 0; }}
        .qr-container {{
            background: white;
            padding: 15px;
            border-radius: 16px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.05);
            display: inline-block;
            margin-bottom: 25px;
            border: 1px solid #e2e8f0;
        }}
        .qr-image {{ max-width: 100%; height: auto; display: block; border-radius: 8px; }}
        .info-box {{ 
            background: #f8fafc; 
            border-radius: 12px; 
            padding: 20px; 
            text-align: left; 
            margin-bottom: 25px; 
            border: 1px solid #e2e8f0;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 12px;
            font-size: 14px;
        }}
        .info-row:last-child {{ margin-bottom: 0; }}
        .info-label {{ color: #64748b; }}
        .info-value {{ color: #0f172a; font-weight: 600; text-align: right; word-break: break-word; }}
        .highlight {{ color: #e74c3c; font-weight: 700; font-size: 16px; }}
        
        .countdown-container {{
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
            padding: 15px;
            background: #eff6ff;
            border-radius: 12px;
            color: #1e40af;
            font-weight: 500;
            font-size: 15px;
        }}
        .spinner {{ 
            display: inline-block; 
            width: 18px; 
            height: 18px; 
            border: 3px solid rgba(59, 130, 246, 0.2); 
            border-left-color: #3b82f6; 
            border-radius: 50%; 
            animation: spin 1s linear infinite; 
        }}
        @keyframes spin {{ 100% {{ transform: rotate(360deg); }} }}
        
        .timer-text {{ font-family: monospace; font-size: 16px; font-weight: 700; background: white; padding: 4px 8px; border-radius: 6px; }}
        
        .success-overlay {{
            position: absolute; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(255,255,255,0.95);
            display: none; flex-direction: column; align-items: center; justify-content: center;
            z-index: 10;
        }}
        .success-icon {{
            width: 80px; height: 80px; background: #2ecc71; border-radius: 50%;
            display: flex; align-items: center; justify-content: center;
            color: white; font-size: 40px; margin-bottom: 20px;
            animation: popIn 0.5s cubic-bezier(0.175, 0.885, 0.32, 1.275);
        }}
        @keyframes popIn {{ 0% {{ transform: scale(0); }} 100% {{ transform: scale(1); }} }}
        .success-text {{ font-size: 24px; font-weight: 700; color: #2ecc71; margin-bottom: 10px; }}
        .redirect-text {{ color: #64748b; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='card'>
        <h2>Mã QR Thanh Toán</h2>
        <div class='amount-subtitle'>Số tiền cần thanh toán</div>
        <div class='amount'>{payment.Amount:N0} VNĐ</div>
        
        <div class='qr-container'>
            <img class='qr-image' src='{payment.QrCodeUrl}' alt='QR Code' />
        </div>
        
        <div class='info-box'>
            <div class='info-row'>
                <span class='info-label'>Ngân hàng</span>
                <span class='info-value'>{bankName}</span>
            </div>
            <div class='info-row'>
                <span class='info-label'>Tài khoản</span>
                <span class='info-value'>{accountNumber}</span>
            </div>
            <div class='info-row'>
                <span class='info-label'>Chủ thẻ</span>
                <span class='info-value'>{accountName}</span>
            </div>
            <div class='info-row' style='margin-top: 8px; padding-top: 12px; border-top: 1px dashed #cbd5e1;'>
                <span class='info-label'>Nội dung CK</span>
                <span class='info-value highlight'>{payment.PaymentReference}</span>
            </div>
        </div>
        
        <div class='countdown-container'>
            <div class='spinner'></div>
            <span>Chờ thanh toán...</span>
            <span class='timer-text' id='timer'>{minutes:D2}:{seconds:D2}</span>
        </div>
        
        <div class='success-overlay' id='successOverlay'>
            <div class='success-icon'>✓</div>
            <div class='success-text'>Thanh toán thành công!</div>
            <div class='redirect-text'>Đang chuyển hướng...</div>
        </div>
    </div>
    
    <script>
        // Countdown Logic
        let totalSeconds = {remainingSeconds};
        const timerEl = document.getElementById('timer');
        const finalCancelUrl = {System.Text.Json.JsonSerializer.Serialize(finalCancelUrl)};
        const backendSuccessUrl = {System.Text.Json.JsonSerializer.Serialize(backendSuccessUrl)};
        
        const countdownInterval = setInterval(() => {{
            totalSeconds--;
            if (totalSeconds <= 0) {{
                clearInterval(countdownInterval);
                if (finalCancelUrl) window.location.href = finalCancelUrl;
            }} else {{
                const m = Math.floor(totalSeconds / 60).toString().padStart(2, '0');
                const s = (totalSeconds % 60).toString().padStart(2, '0');
                timerEl.textContent = `${{m}}:${{s}}`;
            }}
        }}, 1000);
        
        // Polling Logic
        const pollInterval = setInterval(() => {{
            fetch('/api/Payment/{orderId}/poll-status')
                .then(res => res.json())
                .then(data => {{
                    if (data.status === 'COMPLETED') {{
                        clearInterval(pollInterval);
                        clearInterval(countdownInterval);
                        
                        // Show success overlay
                        document.getElementById('successOverlay').style.display = 'flex';
                        
                        // Redirect after 2 seconds
                        setTimeout(() => {{
                            window.location.href = backendSuccessUrl;
                        }}, 2000);
                    }} else if (data.status === 'EXPIRED' || data.status === 'CANCELLED') {{
                        clearInterval(pollInterval);
                        if (finalCancelUrl) window.location.href = finalCancelUrl;
                    }}
                }})
                .catch(err => console.error('Polling error:', err));
        }}, 3000);
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }

    // ==================== SEPAY SUCCESS CALLBACK ====================

    /// <summary>
    /// SePay redirect về đây khi thanh toán thành công.
    /// Tự động cập nhật Payment → COMPLETED, Order → CONFIRMED.
    /// Nếu có redirectUrl → redirect user về frontend, không thì trả JSON.
    /// </summary>
    [HttpGet("callback/success")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "SePay Success Callback - Xử lý khi thanh toán thành công")]
    public async Task<IActionResult> SepaySuccessCallback(
        [FromQuery(Name = "order_invoice_number")] string? orderInvoiceNumber,
        [FromQuery] string? redirectUrl)
    {
        if (string.IsNullOrEmpty(orderInvoiceNumber))
        {
            if (!string.IsNullOrEmpty(redirectUrl))
                return Redirect($"{redirectUrl}?status=error&message=missing_order_number");
            return BadRequest(ApiResponse<object>.ErrorResponse("Thiếu order_invoice_number"));
        }

        // Xử lý cập nhật status
        var result = await _paymentService.ProcessSuccessCallbackAsync(orderInvoiceNumber);

        // Nếu FE có truyền redirectUrl → redirect về FE sau khi cập nhật DB
        if (!string.IsNullOrEmpty(redirectUrl))
        {
            if (result.Success)
            {
                var data = result.Data;
                if (result.Message != null && result.Message.Contains("đã hoàn tất"))
                {
                    return Redirect($"{redirectUrl}?status=success&orderNumber={orderInvoiceNumber}&note=already_completed");
                }
                return Redirect($"{redirectUrl}?status=success&orderId={data?.OrderId}&orderNumber={orderInvoiceNumber}");
            }
            else
            {
                return Redirect($"{redirectUrl}?status=error&orderNumber={orderInvoiceNumber}&message={Uri.EscapeDataString(result.Message)}");
            }
        }

        // Không có redirectUrl → trả JSON
        if (result.Success)
            return Ok(result);

        return BadRequest(result);
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
