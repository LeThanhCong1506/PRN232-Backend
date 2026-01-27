using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class TutorialComponent
{
    public int Id { get; set; }

    public int TutorialId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public string? UsageNote { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Tutorial Tutorial { get; set; } = null!;
}
