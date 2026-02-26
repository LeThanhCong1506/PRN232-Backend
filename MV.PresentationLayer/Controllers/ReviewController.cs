using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Review.Request;
using Swashbuckle.AspNetCore.Annotations;

namespace MV.PresentationLayer.Controllers;

/// <summary>
/// Quản lý reviews sản phẩm
/// </summary>
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Lấy danh sách reviews của sản phẩm (Public)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="page">Trang (default: 1)</param>
    /// <param name="pageSize">Số lượng mỗi trang (default: 10, max: 50)</param>
    /// <returns>Rating distribution, average rating, paginated reviews</returns>
    [HttpGet("/api/Product/{productId}/reviews")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get product reviews with rating distribution")]
    public async Task<IActionResult> GetProductReviews(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var result = await _reviewService.GetProductReviewsAsync(productId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Tạo review cho sản phẩm
    /// </summary>
    [HttpPost("/api/Product/{productId}/reviews")]
    [Authorize]
    [SwaggerOperation(Summary = "Create a product review")]
    public async Task<IActionResult> CreateReview(int productId, [FromBody] CreateReviewRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized();
        }

        var result = await _reviewService.CreateReviewAsync(userId, productId, request);
        
        if (!result.Success)
        {
            if (result.Message != null && result.Message.Contains("purchase")) 
            {
                return StatusCode(403, result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Admin xóa review (hard delete)
    /// </summary>
    /// <param name="reviewId">Review ID cần xóa</param>
    /// <returns>Success hoặc 404 nếu không tìm thấy</returns>
    [HttpDelete("/api/admin/reviews/{reviewId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Delete a review")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var result = await _reviewService.DeleteReviewAsync(reviewId);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
