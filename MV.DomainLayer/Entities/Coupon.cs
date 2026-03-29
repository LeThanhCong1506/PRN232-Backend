using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string Code { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal? MinOrderValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public int? UsedCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<OrderHeader> OrderHeaders { get; set; } = new List<OrderHeader>();
}
