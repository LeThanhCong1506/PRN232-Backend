using MV.DomainLayer.DTOs.RequestModels;

namespace MV.DomainLayer.DTOs.Order.Request;

public class OrderFilterRequest : PaginationFilter
{
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
