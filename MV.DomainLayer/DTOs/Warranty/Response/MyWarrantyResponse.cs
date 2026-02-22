namespace MV.DomainLayer.DTOs.Warranty.Response;

public class MyWarrantyResponse
{
    public int WarrantyId { get; set; }
    public MyWarrantyProductInfo Product { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public DateOnly PurchaseDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public int MonthsRemaining { get; set; }
    public string Status { get; set; } = null!;
    public string PolicyName { get; set; } = null!;
}

public class MyWarrantyProductInfo
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string? Image { get; set; }
}
