namespace MV.DomainLayer.DTOs.ProductImage.Response;

public class ProductImageResponse
{
    public int ImageId { get; set; }
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
}
