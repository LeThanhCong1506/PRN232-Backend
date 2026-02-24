using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Order.Request;
using MV.DomainLayer.DTOs.Order.Response;
using MV.DomainLayer.DTOs.ResponseModels;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Create an order from the shopping cart (Checkout)
    /// </summary>
    [HttpPost("checkout")]
    [SwaggerOperation(Summary = "Create an order from the shopping cart")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request)
    {
        Console.WriteLine($"[CHECKOUT] PaymentMethod={request.PaymentMethod}, CustomerName={request.CustomerName}, Phone={request.CustomerPhone}");
        
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var result = await _orderService.CheckoutAsync(userId.Value, request);
        
        Console.WriteLine($"[CHECKOUT RESULT] Success={result.Success}, Message={result.Message}");
        if (!result.Success)
        {
            Console.WriteLine($"[CHECKOUT ERRORS] {string.Join(", ", result.Errors ?? new List<string>())}");
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get order list of the current user
    /// </summary>
    [HttpGet("my-orders")]
    [SwaggerOperation(Summary = "Get my order list")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<OrderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderFilterRequest filter)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var result = await _orderService.GetMyOrdersAsync(userId.Value, filter);
        return Ok(result);
    }

    /// <summary>
    /// Get all orders (Admin/Staff only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "[Admin] Get all orders")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<OrderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterRequest filter)
    {
        var result = await _orderService.GetAllOrdersAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get order details
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get order details")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderDetail(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var result = await _orderService.GetOrderDetailAsync(id, userId.Value, isAdmin);

        if (!result.Success)
        {
            if (result.Message.ToLower().Contains("not found"))
                return NotFound(result);
            if (result.Message.Contains("Unauthorized"))
                return StatusCode(403, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update order status (Admin/Staff only)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "[Admin] Update order status")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var result = await _orderService.UpdateOrderStatusAsync(id, userId.Value, request);

        if (!result.Success)
        {
            if (result.Message.ToLower().Contains("not found"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPut("{id}/cancel")]
    [SwaggerOperation(Summary = "Cancel an order")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var result = await _orderService.CancelOrderAsync(id, userId.Value, isAdmin, request);

        if (!result.Success)
        {
            if (result.Message.ToLower().Contains("not found"))
                return NotFound(result);
            if (result.Message.Contains("Unauthorized"))
                return StatusCode(403, result);
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