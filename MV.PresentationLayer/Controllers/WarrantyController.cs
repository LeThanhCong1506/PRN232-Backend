using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Warranty.Request;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Quản lý bảo hành sản phẩm
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WarrantyController : ControllerBase
{
    private readonly IWarrantyService _warrantyService;

    public WarrantyController(IWarrantyService warrantyService)
    {
        _warrantyService = warrantyService;
    }

    /// <summary>
    /// Lấy danh sách bảo hành của customer đang đăng nhập
    /// </summary>
    [HttpGet("/api/warranties")]
    [Authorize]
    [SwaggerOperation(Summary = "Get my warranties (Customer)")]
    public async Task<IActionResult> GetMyWarranties()
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        var result = await _warrantyService.GetMyWarrantiesAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin bảo hành theo ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get warranty by ID")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _warrantyService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin bảo hành theo serial number
    /// </summary>
    [HttpGet("serial/{serialNumber}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get warranty by serial number")]
    public async Task<IActionResult> GetBySerialNumber(string serialNumber)
    {
        var result = await _warrantyService.GetBySerialNumberAsync(serialNumber);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lấy tất cả bảo hành
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get all warranties")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _warrantyService.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách bảo hành theo product ID
    /// </summary>
    [HttpGet("product/{productId}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get warranties by product ID")]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var result = await _warrantyService.GetByProductIdAsync(productId);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách bảo hành đang active
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get active warranties")]
    public async Task<IActionResult> GetActiveWarranties()
    {
        var result = await _warrantyService.GetActiveWarrantiesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách bảo hành đã hết hạn
    /// </summary>
    [HttpGet("expired")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get expired warranties")]
    public async Task<IActionResult> GetExpiredWarranties()
    {
        var result = await _warrantyService.GetExpiredWarrantiesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Tạo bảo hành mới (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Create new warranty")]
    public async Task<IActionResult> Create([FromBody] CreateWarrantyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _warrantyService.CreateAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.WarrantyId }, result);
    }

    /// <summary>
    /// Cập nhật thông tin bảo hành (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Update warranty")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarrantyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _warrantyService.UpdateAsync(id, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Xóa bảo hành (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Delete warranty")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _warrantyService.DeleteAsync(id);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }
}
