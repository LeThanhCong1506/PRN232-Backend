namespace MV.DomainLayer.DTOs.ProductBundle.Response;

public class ProductBundleResponse
{
    public int BundleId { get; set; }
    public int ParentProductId { get; set; }
    public string ParentProductName { get; set; } = null!;
    public int ChildProductId { get; set; }
    public string ChildProductName { get; set; } = null!;
    public string ChildProductSku { get; set; } = null!;
    public decimal ChildProductPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime? CreatedAt { get; set; }
}
