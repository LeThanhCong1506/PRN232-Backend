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
}
