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
    /// Tạo đơn hàng từ giỏ hàng (Checkout)
    /// </summary>
    [HttpPost("checkout")]
    [SwaggerOperation(Summary = "Checkout - Tạo đơn hàng từ giỏ hàng")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

        var result = await _orderService.CheckoutAsync(userId.Value, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách đơn hàng của user hiện tại
    /// </summary>
    [HttpGet("my-orders")]
    [SwaggerOperation(Summary = "Lấy danh sách đơn hàng của tôi")]
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
    /// Lấy tất cả đơn hàng (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "[Admin] Lấy tất cả đơn hàng")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<OrderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterRequest filter)
    {
        var result = await _orderService.GetAllOrdersAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết đơn hàng
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Lấy chi tiết đơn hàng")]
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
            if (result.Message.Contains("không tồn tại"))
                return NotFound(result);
            if (result.Message.Contains("Unauthorized"))
                return StatusCode(403, result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng (Admin only)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Staff")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật trạng thái đơn hàng")]
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
            if (result.Message.Contains("không tồn tại"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Hủy đơn hàng
    /// </summary>
    [HttpPut("{id}/cancel")]
    [SwaggerOperation(Summary = "Hủy đơn hàng")]
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
            if (result.Message.Contains("không tồn tại"))
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
