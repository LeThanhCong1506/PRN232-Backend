namespace MV.DomainLayer.DTOs.RequestModels
{
    public class ProductFilter : PaginationFilter
    {
        public string? SearchTerm { get; set; }
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}