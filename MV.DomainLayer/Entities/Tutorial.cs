using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class Tutorial
{
    public int TutorialId { get; set; }

    public int CreatedBy { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Duration in minutes
    /// </summary>
    public int? EstimatedDuration { get; set; }

    public string? Instructions { get; set; }

    public string? VideoUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<TutorialComponent> TutorialComponents { get; set; } = new List<TutorialComponent>();
}
