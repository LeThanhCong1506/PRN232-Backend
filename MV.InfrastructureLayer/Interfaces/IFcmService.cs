namespace MV.InfrastructureLayer.Interfaces;

public interface IFcmService
{
    Task SendAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
}
