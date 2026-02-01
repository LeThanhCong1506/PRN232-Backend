namespace MV.DomainLayer.DTOs.Brand.Response;

public class BrandDetailResponse
{
    public int BrandId { get; set; }
    public string Name { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public int TotalProducts { get; set; }
    public int InStockProducts { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
