using System;
using System.Collections.Generic;
using MV.DomainLayer.Enums;

namespace MV.DomainLayer.Entities;

public partial class Product
{
    public int ProductId { get; set; }

    public int BrandId { get; set; }

    public int? WarrantyPolicyId { get; set; }

    public string Sku { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public ProductTypeEnum ProductType { get; set; }

    public decimal Price { get; set; }

    public int? StockQuantity { get; set; }

    public bool? HasSerialTracking { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductBundle> ProductBundleChildProducts { get; set; } = new List<ProductBundle>();

    public virtual ICollection<ProductBundle> ProductBundleParentProducts { get; set; } = new List<ProductBundle>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductInstance> ProductInstances { get; set; } = new List<ProductInstance>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<TutorialComponent> TutorialComponents { get; set; } = new List<TutorialComponent>();

    public virtual WarrantyPolicy? WarrantyPolicy { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}
