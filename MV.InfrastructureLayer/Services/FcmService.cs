using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Services;

public class FcmService : IFcmService
{
    private readonly ILogger<FcmService> _logger;

    public FcmService(ILogger<FcmService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrEmpty(fcmToken)) return;

        var message = new Message
        {
            Token = fcmToken,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High
            }
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("FCM sent successfully: {Response}, Token: {Token}", response, fcmToken[..Math.Min(20, fcmToken.Length)]);
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            _logger.LogWarning("FCM token is invalid/unregistered: {Token}", fcmToken[..Math.Min(20, fcmToken.Length)]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM to token: {Token}", fcmToken[..Math.Min(20, fcmToken.Length)]);
        }
    }
}
