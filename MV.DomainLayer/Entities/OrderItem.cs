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

    public virtual OrderHeader Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductInstance> ProductInstances { get; set; } = new List<ProductInstance>();
}
