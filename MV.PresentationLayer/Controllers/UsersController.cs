using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Login.Request;
using MV.DomainLayer.DTOs.Login.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MV.PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : Controller
    {
        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateUserDto dto)
        {
            var result = await _service.CreateAsync(dto);
            if (result != "OK")
                return BadRequest(result);

            var loginResult = await _service.LoginAsync(new LoginDto
            {
                Email = dto.Email,
                Password = dto.Password
            });

            if (loginResult == null)
                return StatusCode(500, "Registration succeeded but auto-login failed");

            return Ok(loginResult);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _service.LoginAsync(dto);
            if (result == null)
                return Unauthorized("Email hoặc mật khẩu không đúng");

            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _service.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(sub, out var userId))
                return Unauthorized();

            var user = await _service.GetByIdAsync(userId);
            if (user == null)
                return NotFound("User không tồn tại");

            return Ok(new
            {
                id = user.UserId,
                username = user.Username,
                email = user.Email,
                role = user.RoleName,
                fullName = user.FullName,
                phone = user.Phone,
                address = user.Address,
                avatarUrl = (string?)null,
                city = user.City,
                district = user.District,
                ward = user.Ward,
                createdAt = user.CreatedAt?.ToString("o")
            });
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateCurrentUser(UpdateUserDto dto)
        {
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(sub, out var userId))
                return Unauthorized();

            var result = await _service.UpdateAsync(userId, dto);
            if (result == true)
            {
                var updatedUser = await _service.GetByIdAsync(userId);
                return Ok(new
                {
                    id = updatedUser!.UserId,
                    username = updatedUser.Username,
                    email = updatedUser.Email,
                    role = updatedUser.RoleName,
                    fullName = updatedUser.FullName,
                    phone = updatedUser.Phone,
                    address = updatedUser.Address,
                    avatarUrl = (string?)null,
                    city = updatedUser.City,
                    district = updatedUser.District,
                    ward = updatedUser.Ward,
                    createdAt = updatedUser.CreatedAt?.ToString("o")
                });
            }
            else
            {
                return BadRequest("Cập nhật thất bại");
            }
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null)
                return NotFound("User không tồn tại");

            return Ok(user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UpdateUserDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == true)
                return Ok("Update thành công");
            else
                return BadRequest(result);
            //try
            //{
            //    await _service.UpdateAsync(id, dto);
            //    return Ok("Update thành công");
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(ex.Message);
            //}
        }
    }
}
