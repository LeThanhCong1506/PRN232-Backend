namespace MV.DomainLayer.DTOs.Checkout.Request;

public class ValidateCheckoutRequest
{
    public string? CouponCode { get; set; }
    public string? Province { get; set; }
}
