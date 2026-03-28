using System;
using System.Collections.Generic;

namespace MV.DomainLayer.Entities;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string? ExternalProvider { get; set; }
    
    public string? ExternalId { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? FullName { get; set; }

    public string? Province { get; set; }

    public string? District { get; set; }

    public string? Ward { get; set; }

    public string? StreetAddress { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<OrderHeader> OrderHeaderCancelledByNavigations { get; set; } = new List<OrderHeader>();

    public virtual ICollection<OrderHeader> OrderHeaderConfirmedByNavigations { get; set; } = new List<OrderHeader>();

    public virtual ICollection<OrderHeader> OrderHeaderShippedByNavigations { get; set; } = new List<OrderHeader>();

    public virtual ICollection<OrderHeader> OrderHeaderUsers { get; set; } = new List<OrderHeader>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();

    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();
}
