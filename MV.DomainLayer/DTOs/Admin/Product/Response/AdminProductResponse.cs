namespace MV.DomainLayer.DTOs.Admin.Product.Response;

public class AdminProductResponse
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string Sku { get; set; } = null!;
    public string ProductType { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public string? BrandName { get; set; }
    public List<string> Categories { get; set; } = new();
    public string? PrimaryImage { get; set; }
    public bool LowStock { get; set; }
}
