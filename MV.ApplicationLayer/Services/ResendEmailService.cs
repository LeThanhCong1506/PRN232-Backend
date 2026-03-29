using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MV.ApplicationLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class ResendEmailService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly HttpClient _httpClient;

    public ResendEmailService(IConfiguration configuration, HttpClient httpClient)
    {
        _apiKey = configuration["Resend:ApiKey"] ?? "";
        _senderEmail = configuration["Resend:SenderEmail"] ?? "onboarding@resend.dev";
        _senderName = configuration["Resend:SenderName"] ?? "STEM Gear";
        _httpClient = httpClient;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string otp)
    {
        var body = $@"
<html>
<body style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
    <div style=""background-color: #2463eb; padding: 20px; text-align: center;"">
        <h1 style=""color: white; margin: 0;"">STEM Gear</h1>
    </div>
    <div style=""padding: 30px; background-color: #f8f9fa;"">
        <h2 style=""color: #0e121b;"">Reset your password</h2>
        <p style=""color: #4d6599;"">Use the OTP code below to reset your password:</p>
        <div style=""background-color: white; border: 2px solid #2463eb; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;"">
            <span style=""font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #2463eb;"">{otp}</span>
        </div>
        <p style=""color: #4d6599;"">This code expires in <strong>15 minutes</strong>.</p>
        <p style=""color: #4d6599;"">If you didn't request a password reset, please ignore this email.</p>
    </div>
    <div style=""padding: 15px; text-align: center; color: #999; font-size: 12px;"">
        &copy; 2025 STEM Gear. All rights reserved.
    </div>
</body>
</html>";

        var payload = new
        {
            from = $"{_senderName} <{_senderEmail}>",
            to = new[] { toEmail },
            subject = "Password Reset OTP - STEM Gear",
            html = body
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[RESEND ERROR] {response.StatusCode}: {responseBody}");
            throw new Exception($"Resend API error: {response.StatusCode} - {responseBody}");
        }

        Console.WriteLine($"[RESEND] Email sent to {toEmail}: {responseBody}");
    }
}
