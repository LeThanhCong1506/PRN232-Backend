using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Checkout.Request;
using MV.DomainLayer.DTOs.Checkout.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Checkout APIs - Validate cart, shipping info, payment methods
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckoutController : ControllerBase
{
    private readonly ICheckoutService _checkoutService;

    public CheckoutController(ICheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
    }

    /// <summary>
    /// API 1: Validate Checkout - Validate toàn bộ thông tin trước khi đặt hàng
    /// </summary>
    /// <param name="request">Optional coupon code</param>
    /// <returns>Complete checkout validation result</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/checkout/validate
    ///     {
    ///       "couponCode": "SALE10"
    ///     }
    ///     
    /// Validates:
    /// - Cart not empty
    /// - Stock availability for all items (including KIT components)
    /// - Shipping info completeness (phone, address)
    /// - Coupon validity (if provided)
    /// - Calculate total with shipping and discount
    /// </remarks>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<ValidateCheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateCheckout([FromBody] ValidateCheckoutRequest request)
    {
        // Extract userId from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));
        }

        int userId = int.Parse(userIdClaim.Value);

        // Call service
        var result = await _checkoutService.ValidateCheckoutAsync(userId, request);

        if (!result.Success)
        {
            // Special handling for stock errors - return with detailed error structure
            if (result.Data?.StockErrors != null && result.Data.StockErrors.Any())
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Data.StockErrors
                });
            }

            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// API 2: Get Shipping Information - Lấy thông tin giao hàng của user
    /// </summary>
    /// <returns>User shipping information</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/checkout/shipping-info
    ///     
    /// Returns user's shipping details:
    /// - Username, Email, Phone, Address
    /// - Returns null for missing fields
    /// - Frontend should prompt user to update profile if incomplete
    /// </remarks>
    [HttpGet("shipping-info")]
    [ProducesResponseType(typeof(ApiResponse<ShippingInfoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShippingInfo()
    {
        // Extract userId from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));
        }

        int userId = int.Parse(userIdClaim.Value);

        // Call service
        var result = await _checkoutService.GetShippingInfoAsync(userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// API 3: Get Payment Methods - Lấy danh sách phương thức thanh toán
    /// </summary>
    /// <returns>List of available payment methods</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/checkout/payment-methods
    ///     
    /// Returns available payment methods:
    /// - COD (Cash on Delivery)
    /// - BANK_TRANSFER (Manual bank transfer)
    /// 
    /// Phase 1: Only COD and BANK_TRANSFER
    /// Future: VNPAY, MOMO integration
    /// </remarks>
    [HttpGet("payment-methods")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentMethodDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var result = await _checkoutService.GetPaymentMethodsAsync();
        return Ok(result);
    }
}
