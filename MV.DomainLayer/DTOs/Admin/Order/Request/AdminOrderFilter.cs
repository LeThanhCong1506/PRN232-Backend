using MV.DomainLayer.DTOs.RequestModels;

namespace MV.DomainLayer.DTOs.Admin.Order.Request;

public class AdminOrderFilter : PaginationFilter
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
