using MV.DomainLayer.DTOs.Chatbot;

namespace MV.ApplicationLayer.Interfaces;

public interface IChatbotService
{
    Task<ChatbotResponse> AskAsync(string question);
}
