namespace MV.DomainLayer.DTOs.Admin.Product.Response;

public class AdminProductImageResponse
{
    public int ImageId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public bool IsPrimary { get; set; }
}
