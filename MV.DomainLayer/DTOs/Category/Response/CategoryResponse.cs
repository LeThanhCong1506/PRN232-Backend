namespace MV.DomainLayer.DTOs.Category.Response;

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public int ProductCount { get; set; }
}
