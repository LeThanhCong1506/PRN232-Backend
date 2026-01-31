using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MV.PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<CartResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCart()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));
            }

            int userId = int.Parse(userIdClaim.Value);

            var result = await _cartService.GetCartAsync(userId);
            return Ok(result);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

            int userId = int.Parse(userIdClaim.Value);

            var result = await _cartService.AddToCartAsync(userId, request);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    availableQuantity = ((dynamic)result.Data).availableQuantity
                });
            }

            // Trả về 201 Created
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateCartItem([FromRoute] int cartItemId, [FromBody] UpdateCartItemRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

            int userId = int.Parse(userIdClaim.Value);

            var result = await _cartService.UpdateCartItemQuantityAsync(userId, cartItemId, request.Quantity);

            if (!result.Success)
            {
                // Kiểm tra nếu Data là Dictionary (lỗi Stock)
                if (result.Data is Dictionary<string, int> errorData)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message,
                        availableQuantity = errorData["availableQuantity"]
                    });
                }

                if (result.Message == "Cart item not found") return NotFound(result);

                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveCartItem([FromRoute] int cartItemId)
        {
            // 1. Lấy UserId từ Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

            int userId = int.Parse(userIdClaim.Value);

            // 2. Gọi Service
            var result = await _cartService.RemoveCartItemAsync(userId, cartItemId);

            // 3. Xử lý kết quả trả về
            if (!result.Success)
            {
                if (result.Message == "Cart item not found") return NotFound(result);
                if (result.Message.Contains("Unauthorized")) return StatusCode(403, result);

                return BadRequest(result);
            }

            // Trả về 200 OK với Message thành công
            return Ok(result);
        }

        /// <summary>
        /// API 15: Clear Cart - Xóa tất cả sản phẩm trong giỏ hàng
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearCart()
        {
            // 1. Lấy UserId từ Token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null) return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid Token"));

            int userId = int.Parse(userIdClaim.Value);

            // 2. Gọi Service để xóa tất cả items trong cart
            var result = await _cartService.ClearCartAsync(userId);

            // 3. Trả về kết quả
            return Ok(result);
        }
    }
}