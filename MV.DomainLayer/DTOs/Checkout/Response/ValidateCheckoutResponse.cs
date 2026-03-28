namespace MV.DomainLayer.DTOs.Checkout.Response;

public class ValidateCheckoutResponse
{
    public bool IsValid { get; set; }
    public List<CheckoutCartItemDto> CartItems { get; set; } = new();
    public CheckoutShippingInfoDto? ShippingInfo { get; set; }
    public CheckoutCouponDto? Coupon { get; set; }
    public CheckoutSummaryDto? Summary { get; set; }
    public List<StockErrorDto>? StockErrors { get; set; }
    /// <summary>
    /// Non-null nếu coupon được truyền vào nhưng không hợp lệ.
    /// Page vẫn load bình thường, chỉ không áp dụng discount.
    /// </summary>
    public string? CouponError { get; set; }
}
