namespace MV.DomainLayer.DTOs.Category.Response;

public class CategoryDetailResponse
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public int TotalProducts { get; set; }
    public int InStockProducts { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
