namespace MV.DomainLayer.DTOs.Brand.Response;

public class BrandResponse
{
    public int BrandId { get; set; }
    public string Name { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public int ProductCount { get; set; }
}
