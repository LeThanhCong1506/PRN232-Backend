namespace MV.DomainLayer.DTOs.ResponseModels;

public class AuthResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Token { get; set; } = null!;
    public int? ExpiresIn { get; set; }
}
