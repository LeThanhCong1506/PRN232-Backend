using MV.DomainLayer.Enums;

namespace MV.DomainLayer.DTOs.Product.Response;

public class ProductDetailResponse
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ProductTypeEnum ProductType { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public bool HasSerialTracking { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Brand info
    public BrandInfo Brand { get; set; } = null!;

    // Warranty info
    public WarrantyPolicyInfo? WarrantyPolicy { get; set; }

    // Categories
    public List<CategoryInfo> Categories { get; set; } = new List<CategoryInfo>();

    // Images
    public List<ProductImageInfo> Images { get; set; } = new List<ProductImageInfo>();

    // Reviews summary
    public ReviewSummary Reviews { get; set; } = new ReviewSummary();

    public class BrandInfo
    {
        public int BrandId { get; set; }
        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }
    }

    public class WarrantyPolicyInfo
    {
        public int PolicyId { get; set; }
        public string PolicyName { get; set; } = null!;
        public int DurationMonths { get; set; }
        public string? Description { get; set; }
    }

    public class CategoryInfo
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
    }

    public class ProductImageInfo
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }

    public class ReviewSummary
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }
}
