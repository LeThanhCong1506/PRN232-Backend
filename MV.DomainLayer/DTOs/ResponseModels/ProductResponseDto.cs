namespace MV.DomainLayer.DTOs.ResponseModels
{
    public class ProductResponseDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string ProductType { get; set; }
        public BrandDto Brand { get; set; }
        public string PrimaryImage { get; set; }
        public List<CategoryDto> Categories { get; set; }
        public bool InStock { get; set; }
    }

    public class BrandDto
    {
        public int BrandId { get; set; }
        public string Name { get; set; }
    }

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
    }
}