namespace MV.DomainLayer.DTOs.Checkout.Response;

public class StockErrorDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
}
