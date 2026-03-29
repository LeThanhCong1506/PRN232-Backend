namespace MV.DomainLayer.DTOs.Checkout.Response;

public class CheckoutSummaryDto
{
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public bool IsFreeShipping { get; set; }
    public decimal FreeShippingThreshold { get; set; }
}
