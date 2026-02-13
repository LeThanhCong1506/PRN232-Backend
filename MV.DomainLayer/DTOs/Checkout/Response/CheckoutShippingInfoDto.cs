namespace MV.DomainLayer.DTOs.Checkout.Response;

public class CheckoutShippingInfoDto
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
