namespace MV.DomainLayer.DTOs.Checkout.Response;

public class ValidateCheckoutResponse
{
    public bool IsValid { get; set; }
    public List<CheckoutCartItemDto> CartItems { get; set; } = new();
    public CheckoutShippingInfoDto? ShippingInfo { get; set; }
    public CheckoutCouponDto? Coupon { get; set; }
    public CheckoutSummaryDto? Summary { get; set; }
    public List<StockErrorDto>? StockErrors { get; set; }
}
