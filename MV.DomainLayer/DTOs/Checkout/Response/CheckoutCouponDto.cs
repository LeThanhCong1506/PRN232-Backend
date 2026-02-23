namespace MV.DomainLayer.DTOs.Checkout.Response;

public class CheckoutCouponDto
{
    public bool IsApplied { get; set; }
    public string? Code { get; set; }
    public decimal DiscountAmount { get; set; }
}
