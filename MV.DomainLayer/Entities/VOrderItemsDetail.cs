using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class VOrderItemsDetail
{
    public int? OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public string? CustomerName { get; set; }

    public int? OrderItemId { get; set; }

    public int? ProductId { get; set; }

    public string? ProductName { get; set; }

    public string? ProductSku { get; set; }

    public string? ProductImageUrl { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? ItemDiscount { get; set; }

    public decimal? ItemSubtotal { get; set; }

    public string? ItemNotes { get; set; }
}
