using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MV.ApplicationLayer.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(User user)
        {
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.RoleName)
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["Jwt:ExpireMinutes"]!)
                ),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    //    public string GenerateToken(SystemUser user)
    //    {
    //        var claims = new[]
    //        {
    //    new Claim(ClaimTypes.Name, user.Username),
    //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    //    new Claim(ClaimTypes.Role, user.Role)
    //};

    //        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
    //        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //        var token = new JwtSecurityToken(
    //            issuer: _jwtSettings.Issuer,
    //            audience: _jwtSettings.Audience,
    //            claims: claims,
    //            expires: DateTime.Now.AddMinutes(_jwtSettings.DurationInMinutes),
    //            signingCredentials: creds
    //        );

    //        return new JwtSecurityTokenHandler().WriteToken(token);
    //    }

    }
}
