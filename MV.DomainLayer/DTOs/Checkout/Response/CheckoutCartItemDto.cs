namespace MV.DomainLayer.DTOs.Checkout.Response;

public class CheckoutCartItemDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemTotal { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
}
