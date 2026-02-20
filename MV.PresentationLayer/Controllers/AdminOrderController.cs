using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Order.Request;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminOrderController : ControllerBase
{
    private readonly IAdminOrderService _adminOrderService;

    public AdminOrderController(IAdminOrderService adminOrderService)
    {
        _adminOrderService = adminOrderService;
    }

    [HttpGet("orders")]
    [SwaggerOperation(Summary = "[Admin] Get all orders with admin filters and status counts")]
    public async Task<IActionResult> GetOrders([FromQuery] AdminOrderFilter filter)
    {
        var result = await _adminOrderService.GetAdminOrdersAsync(filter);
        return Ok(result);
    }

    [HttpGet("orders/{id}")]
    [SwaggerOperation(Summary = "[Admin] Get order detail with allowed status transitions")]
    public async Task<IActionResult> GetOrderDetail(int id)
    {
        var result = await _adminOrderService.GetAdminOrderDetailAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPut("orders/{id}/status")]
    [SwaggerOperation(Summary = "[Admin] Update order status with side effects (stock, warranty, etc.)")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] AdminUpdateOrderStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminUserId = GetUserId();
        if (adminUserId == 0)
            return Unauthorized();

        var result = await _adminOrderService.UpdateOrderStatusAsync(id, adminUserId, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPut("orders/{id}/payment-status")]
    [SwaggerOperation(Summary = "[Admin] Manually update payment status")]
    public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminOrderService.UpdatePaymentStatusAsync(id, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    [SwaggerOperation(Summary = "[Admin] Get dashboard statistics")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _adminOrderService.GetDashboardAsync();
        return Ok(result);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }
}
