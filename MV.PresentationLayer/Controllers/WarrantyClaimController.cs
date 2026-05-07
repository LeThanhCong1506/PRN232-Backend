using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.WarrantyClaim.Request;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Quản lý Warranty Claims
/// </summary>
[ApiController]
public class WarrantyClaimController : ControllerBase
{
    private readonly IWarrantyClaimService _warrantyClaimService;

    public WarrantyClaimController(IWarrantyClaimService warrantyClaimService)
    {
        _warrantyClaimService = warrantyClaimService;
    }

    /// <summary>
    /// [Customer] Submit warranty claim cho một warranty cụ thể
    /// </summary>
    /// <param name="warrantyId">Warranty ID cần claim</param>
    /// <param name="request">Mô tả lỗi và SĐT liên hệ</param>
    /// <returns>Claim ID và status SUBMITTED</returns>
    [HttpPost("/api/warranties/{warrantyId}/claims")]
    [Authorize]
    [SwaggerOperation(Summary = "[Customer] Submit warranty claim")]
    public async Task<IActionResult> SubmitClaim(int warrantyId, [FromBody] SubmitWarrantyClaimRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        var result = await _warrantyClaimService.SubmitClaimAsync(warrantyId, userId, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return StatusCode(201, result);
    }

    /// <summary>
    /// [Customer] Lấy danh sách các warranty claims của mình (kèm serial number)
    /// </summary>
    [HttpGet("/api/warranties/claims")]
    [Authorize]
    [SwaggerOperation(Summary = "[Customer] Get my warranty claims with serial number")]
    public async Task<IActionResult> GetMyClaims(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            return Unauthorized();
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var result = await _warrantyClaimService.GetMyClaimsAsync(userId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Lấy danh sách tất cả warranty claims có phân trang và filter
    /// </summary>
    /// <param name="status">Filter theo status: SUBMITTED, APPROVED, REJECTED, RESOLVED</param>
    /// <param name="page">Trang (default: 1)</param>
    /// <param name="pageSize">Số lượng mỗi trang (default: 10)</param>
    [HttpGet("/api/admin/warranty-claims")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Get all warranty claims")]
    public async Task<IActionResult> GetAllClaims(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var result = await _warrantyClaimService.GetAllClaimsAsync(status, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// [Admin] Resolve warranty claim (APPROVED / REJECTED / RESOLVED)
    /// </summary>
    /// <param name="claimId">Claim ID cần resolve</param>
    /// <param name="request">Resolution và ghi chú</param>
    [HttpPut("/api/admin/warranty-claims/{claimId}/resolve")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Resolve warranty claim")]
    public async Task<IActionResult> ResolveClaim(int claimId, [FromBody] ResolveWarrantyClaimRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _warrantyClaimService.ResolveClaimAsync(claimId, request);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// [Admin] Lấy chi tiết một warranty claim theo ID
    /// </summary>
    /// <param name="claimId">Claim ID cần lấy</param>
    [HttpGet("/api/admin/warranty-claims/{claimId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Get warranty claim detail by ID")]
    public async Task<IActionResult> GetClaimById(int claimId)
    {
        var result = await _warrantyClaimService.GetClaimByIdAsync(claimId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }
}
