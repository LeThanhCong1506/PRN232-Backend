using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Admin.Coupon.Request;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

[ApiController]
[Route("api/admin/coupons")]
[Authorize(Roles = "Admin")]
public class AdminCouponController : ControllerBase
{
    private readonly ICouponService _couponService;

    public AdminCouponController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "[Admin] Get all coupons with usage statistics")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _couponService.GetAllCouponsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "[Admin] Get coupon detail by ID")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _couponService.GetCouponByIdAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "[Admin] Create a new coupon")]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _couponService.CreateCouponAsync(request);
        if (!result.Success)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.CouponId }, result);
    }

    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "[Admin] Update an existing coupon")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _couponService.UpdateCouponAsync(id, request);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "[Admin] Delete a coupon (only if not used in any orders)")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _couponService.DeleteCouponAsync(id);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
