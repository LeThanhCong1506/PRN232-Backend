using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Login.Request;
using MV.DomainLayer.DTOs.Login.Response;

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
            if (result == "OK")
                return Ok("Tạo tài khoản thành công");
            else
                return BadRequest(result);
            //try
            //{
            //    await _service.CreateAsync(dto);
            //    return Ok("Tạo tài khoản thành công");
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest(ex.Message);
            //}
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

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null)
                return NotFound("User không tồn tại");

            return Ok(user);
        }

        [HttpPut("{id}")]
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
