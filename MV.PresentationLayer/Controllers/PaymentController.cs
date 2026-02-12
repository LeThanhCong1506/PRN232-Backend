using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
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

    public PaymentController(IPaymentService paymentService, IOrderRepository orderRepo, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _orderRepo = orderRepo;
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
        if (paymentStatus != PaymentStatusEnum.PENDING.ToString())
            return BadRequest($"Thanh toán đã ở trạng thái: {paymentStatus}");

        // Check expired
        if (payment.ExpiredAt.HasValue && payment.ExpiredAt.Value < DateTime.Now)
            return BadRequest("Đơn hàng đã hết hạn thanh toán");

        // Build SePay checkout form
        var sepayConfig = _configuration.GetSection("SePay");
        var merchantId = sepayConfig["MerchantId"];
        var secretKey = sepayConfig["SecretKey"];
        var checkoutBaseUrl = sepayConfig["CheckoutUrl"] ?? "https://pay-sandbox.sepay.vn/v1/checkout/init";

        if (string.IsNullOrEmpty(merchantId) || string.IsNullOrEmpty(secretKey))
            return BadRequest("SePay Payment Gateway chưa được cấu hình (MerchantId/SecretKey)");

        // Success URL: luôn trỏ về backend callback/success để cập nhật DB status
        // Nếu FE truyền successUrl → backend sẽ redirect về đó sau khi xử lý xong
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var backendSuccessUrl = $"{baseUrl}/api/Payment/callback/success";
        if (!string.IsNullOrEmpty(successUrl))
        {
            backendSuccessUrl += $"?redirectUrl={Uri.EscapeDataString(successUrl)}";
        }

        // Error và Cancel: SePay redirect thẳng về FE (không cần backend xử lý gì)
        var finalErrorUrl = errorUrl ?? "http://localhost:3000/payment/error";
        var finalCancelUrl = cancelUrl ?? "http://localhost:3000/payment/cancel";

        var orderAmountStr = payment.Amount.ToString("F0");
        var orderDesc = $"Thanh toan don hang {order.OrderNumber}";

        // SePay signature: base64(hmac_sha256(data, secretKey))
        // Thứ tự fields CHÍNH XÁC theo SDK: merchant, operation, payment_method,
        // order_amount, currency, order_invoice_number, order_description,
        // customer_id, success_url, error_url, cancel_url
        // Chỉ include fields có giá trị, format: "field=value,field=value,..."
        var signedParts = new List<string>
        {
            $"merchant={merchantId}",
            $"operation=PURCHASE",
            $"order_amount={orderAmountStr}",
            $"currency=VND",
            $"order_invoice_number={order.OrderNumber}",
            $"order_description={orderDesc}",
            $"success_url={backendSuccessUrl}",
            $"error_url={finalErrorUrl}",
            $"cancel_url={finalCancelUrl}"
        };
        var dataToSign = string.Join(",", signedParts);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        var signature = Convert.ToBase64String(hash);

        // Render HTML page with auto-submit form
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Đang chuyển đến trang thanh toán...</title>
    <style>
        body {{ font-family: Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f5f5f5; }}
        .loading {{ text-align: center; }}
        .spinner {{ border: 4px solid #f3f3f3; border-top: 4px solid #3498db; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto 16px; }}
        @keyframes spin {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
    </style>
</head>
<body>
    <div class='loading'>
        <div class='spinner'></div>
        <p>Đang chuyển đến trang thanh toán SePay...</p>
    </div>
    <form id='sepayForm' method='POST' action='{checkoutBaseUrl}'>
        <input type='hidden' name='merchant' value='{merchantId}' />
        <input type='hidden' name='operation' value='PURCHASE' />
        <input type='hidden' name='order_amount' value='{orderAmountStr}' />
        <input type='hidden' name='currency' value='VND' />
        <input type='hidden' name='order_invoice_number' value='{order.OrderNumber}' />
        <input type='hidden' name='order_description' value='{System.Net.WebUtility.HtmlEncode(orderDesc)}' />
        <input type='hidden' name='success_url' value='{System.Net.WebUtility.HtmlEncode(backendSuccessUrl)}' />
        <input type='hidden' name='error_url' value='{System.Net.WebUtility.HtmlEncode(finalErrorUrl)}' />
        <input type='hidden' name='cancel_url' value='{System.Net.WebUtility.HtmlEncode(finalCancelUrl)}' />
        <input type='hidden' name='signature' value='{signature}' />
    </form>
    <script>document.getElementById('sepayForm').submit();</script>
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
                return Redirect($"{redirectUrl}?status=success&orderId={data?.OrderId}&orderNumber={orderInvoiceNumber}");
            }
            else
            {
                if (result.Message.Contains("đã hoàn tất"))
                    return Redirect($"{redirectUrl}?status=success&orderNumber={orderInvoiceNumber}&note=already_completed");

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
