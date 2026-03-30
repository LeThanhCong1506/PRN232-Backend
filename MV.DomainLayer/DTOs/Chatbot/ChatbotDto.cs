using MV.DomainLayer.Helpers;

namespace MV.DomainLayer.DTOs.Chatbot;

public class ChatbotRequest
{
    public string Question { get; set; } = null!;
}

public class ChatbotResponse
{
    public string Answer { get; set; } = null!;

    /// <summary>
    /// "faq" | "ai" | "fallback" | "error"
    /// </summary>
    public string Source { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTimeHelper.VietnamNow();

    /// <summary>
    /// Optional list of matching products (when question mentions a product keyword)
    /// </summary>
    public List<ProductSuggestion>? Products { get; set; }
}

public class ProductSuggestion
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
}
