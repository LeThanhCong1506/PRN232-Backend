using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual OrderHeader Order { get; set; } = null!;
}
