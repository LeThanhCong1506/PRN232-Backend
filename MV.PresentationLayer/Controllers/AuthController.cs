using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Auth;
using MV.DomainLayer.DTOs.RequestModels;
using System;
using System.Threading.Tasks;

namespace MV.PresentationLayer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IExternalAuthService _externalAuthService;
        private readonly IAuthService _authService;

        public AuthController(IExternalAuthService externalAuthService, IAuthService authService)
        {
            _externalAuthService = externalAuthService;
            _authService = authService;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _externalAuthService.GoogleLoginAsync(request.Code, request.RedirectUri);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("github")]
        public async Task<IActionResult> GitHubLogin([FromBody] ExternalLoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _externalAuthService.GitHubLoginAsync(request.Code, request.RedirectUri);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.ForgotPasswordAsync(request);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.ResetPasswordAsync(request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}
