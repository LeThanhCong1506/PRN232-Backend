using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Auth;
using MV.DomainLayer.DTOs.RequestModels;
using MV.DomainLayer.DTOs.ResponseModels;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;

namespace MV.ApplicationLayer.Services;

public class AuthService : IAuthService
{
    private readonly StemDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    public AuthService(StemDbContext context, IConfiguration configuration, IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        var errors = new List<string>();

        // Check email uniqueness
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            errors.Add("Email already exists.");
        }

        // Check username uniqueness
        var usernameExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
        if (usernameExists)
        {
            errors.Add("Username already exists");
        }

        if (errors.Count > 0)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse("Validation failed.", errors);
        }

        // Get Customer role (role_id = 2)
        var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
        if (customerRole == null)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse("Customer role not found.");
        }

        // Create new user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Phone = request.Phone,
            Address = request.Address,
            RoleId = customerRole.RoleId,
            IsActive = true,
            CreatedAt = DateTimeHelper.VietnamNow()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = GenerateJwtToken(user, customerRole.RoleName);

        var response = new AuthResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = customerRole.RoleName,
            Token = token
        };

        return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        // Find user by email with role
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password.");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password.");
        }

        // Check if user is active
        if (user.IsActive != true)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse("Account is deactivated.");
        }

        // Generate JWT token
        var token = GenerateJwtToken(user, user.Role.RoleName);

        // Get expiry from config
        var expiryHours = _configuration.GetValue<int>("JwtSettings:ExpiryHours", 1);

        var response = new AuthResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.RoleName,
            Token = token,
            ExpiresIn = expiryHours * 3600
        };

        return ApiResponse<AuthResponseDto>.SuccessResponse(response, "Login successful.");
    }

    public async Task<ApiResponse<UserProfileResponseDto>> GetProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return ApiResponse<UserProfileResponseDto>.ErrorResponse("User not found.");
        }

        var response = new UserProfileResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Address = user.Address,
            Role = user.Role.RoleName,
            IsActive = user.IsActive ?? false,
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserProfileResponseDto>.SuccessResponse(response);
    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        // Always return success to prevent email enumeration
        if (user == null || user.IsActive != true)
            return ApiResponse<string>.SuccessResponse("Nếu email tồn tại, mã OTP đã được gửi.", "Nếu email tồn tại, mã OTP đã được gửi.");

        // Rate limit: max 3 requests per hour
        var now = DateTimeHelper.VietnamNow();
        if (user.PasswordResetAttempts >= 3 && user.PasswordResetTokenExpiry.HasValue
            && user.PasswordResetTokenExpiry.Value > now.AddMinutes(-60))
        {
            return ApiResponse<string>.ErrorResponse("Bạn đã yêu cầu quá nhiều lần. Vui lòng thử lại sau 1 giờ.");
        }

        // Generate 6-digit OTP
        var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var hashedOtp = BCrypt.Net.BCrypt.HashPassword(otp);

        user.PasswordResetToken = hashedOtp;
        user.PasswordResetTokenExpiry = now.AddMinutes(15);
        user.PasswordResetAttempts = (user.PasswordResetAttempts ?? 0) + 1;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, otp);
        }
        catch
        {
            // Log but don't reveal email failure to user
        }

        return ApiResponse<string>.SuccessResponse("Nếu email tồn tại, mã OTP đã được gửi.", "Nếu email tồn tại, mã OTP đã được gửi.");
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || user.IsActive != true)
            return ApiResponse<string>.ErrorResponse("Thông tin không hợp lệ.");

        if (string.IsNullOrEmpty(user.PasswordResetToken) || !user.PasswordResetTokenExpiry.HasValue)
            return ApiResponse<string>.ErrorResponse("Mã OTP không hợp lệ hoặc đã hết hạn.");

        var now = DateTimeHelper.VietnamNow();
        if (user.PasswordResetTokenExpiry.Value < now)
            return ApiResponse<string>.ErrorResponse("Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

        if (!BCrypt.Net.BCrypt.Verify(request.Otp, user.PasswordResetToken))
            return ApiResponse<string>.ErrorResponse("Mã OTP không đúng.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.PasswordResetAttempts = 0;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse("Đặt lại mật khẩu thành công.", "Đặt lại mật khẩu thành công.");
    }

    private string GenerateJwtToken(User user, string roleName)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured.");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiryHours = _configuration.GetValue<int>("JwtSettings:ExpiryHours", 1);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
