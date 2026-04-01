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
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return BadRequest(new { Message = "Authorization code is required." });

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
        [AllowAnonymous]
        public async Task<IActionResult> GitHubLogin([FromBody] ExternalLoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return BadRequest(new { Message = "Authorization code is required." });

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

        /// <summary>
        /// GitHub OAuth callback — receives the auth code from GitHub, then deep-links back to the mobile app.
        /// Register this HTTPS URL in your GitHub OAuth App → Authorization callback URL:
        /// https://prn232-backend-production.up.railway.app/api/auth/github/callback
        /// </summary>
        [HttpGet("github/callback")]
        [AllowAnonymous]
        public IActionResult GitHubCallback([FromQuery] string? code, [FromQuery] string? error)
        {
            // GitHub redirected with an error (e.g. user denied access)
            if (!string.IsNullOrEmpty(error))
            {
                var encodedError = Uri.EscapeDataString(error);
                return Redirect($"myapp://auth/github?error={encodedError}");
            }

            // No code received
            if (string.IsNullOrEmpty(code))
            {
                return Redirect("myapp://auth/github?error=no_code");
            }

            // Forward the auth code to the mobile app via custom URI scheme deep link
            var encodedCode = Uri.EscapeDataString(code);
            return Redirect($"myapp://auth/github?code={encodedCode}");
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
