namespace MV.DomainLayer.DTOs.ReturnRequest;

public class ReturnRequestItemResponse
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public List<string> SerialNumbers { get; set; } = new();
}

public class ReturnRequestResponse
{
    public int ReturnRequestId { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedByName { get; set; }
    public List<ReturnRequestItemResponse> Items { get; set; } = new();
}
