using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class OrderHeader
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int? CouponId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public decimal? ShipingFee { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Payment? Payment { get; set; }

    public virtual User User { get; set; } = null!;
}
