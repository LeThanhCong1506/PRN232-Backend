using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class ProductInstance
{
    public string SerialNumber { get; set; } = null!;

    public int ProductId { get; set; }

    public int? OrderItemId { get; set; }

    public DateOnly? ManufacturingDate { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual OrderItem? OrderItem { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
