using System;

namespace MV.DomainLayer.Entities;

public partial class ProductDocument
{
    public int DocumentId { get; set; }

    public int ProductId { get; set; }

    public string DocumentType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Url { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Product Product { get; set; } = null!;
}
