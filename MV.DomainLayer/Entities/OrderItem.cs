using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public string? ProductName { get; set; }

    public string? ProductSku { get; set; }

    /// <summary>
    /// Snapshot ảnh sản phẩm
    /// </summary>
    public string? ProductImageUrl { get; set; }

    /// <summary>
    /// Giảm giá cho item
    /// </summary>
    public decimal? DiscountAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual OrderHeader Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductInstance> ProductInstances { get; set; } = new List<ProductInstance>();
}
