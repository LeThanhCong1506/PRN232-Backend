using MV.DomainLayer.DTOs.RequestModels;

namespace MV.DomainLayer.DTOs.Admin.Product.Request;

public class AdminProductFilter : PaginationFilter
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? ProductType { get; set; }
    public bool? LowStock { get; set; }
}
