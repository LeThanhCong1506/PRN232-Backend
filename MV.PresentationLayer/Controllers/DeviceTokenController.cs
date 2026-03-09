using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MV.InfrastructureLayer.DBContext;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/device-tokens")]
    [Authorize]
    public class DeviceTokenController : ControllerBase
    {
        private readonly StemDbContext _dbContext;

        public DeviceTokenController(StemDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Mobile gọi sau khi login để đăng ký FCM token nhận push notification.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterTokenRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            user.FcmToken = request.FcmToken;
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "FCM token registered successfully" });
        }

        /// <summary>
        /// Mobile gọi khi logout để xóa FCM token.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> Unregister()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized();

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            user.FcmToken = null;
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "FCM token removed successfully" });
        }
    }

    public class RegisterTokenRequest
    {
        public string FcmToken { get; set; } = null!;
    }
}
