using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using MV.ApplicationLayer.Interfaces;

namespace MV.ApplicationLayer.Services;

public class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public SmtpEmailService(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? "smtp.gmail.com";
        _port = int.Parse(configuration["Smtp:Port"] ?? "587");
        _username = configuration["Smtp:Username"] ?? "";
        _password = configuration["Smtp:Password"] ?? "";
        _senderEmail = configuration["Smtp:SenderEmail"] ?? _username;
        _senderName = configuration["Smtp:SenderName"] ?? "STEM Gear";
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string otp)
    {
        var subject = "Password Reset Code - STEM Gear";
        var body = $@"
<html>
<body style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;"">
    <div style=""background-color: #2463eb; padding: 20px; text-align: center;"">
        <h1 style=""color: white; margin: 0;"">STEM Gear</h1>
    </div>
    <div style=""padding: 30px; background-color: #f8f9fa;"">
        <h2 style=""color: #0e121b;"">Reset Your Password</h2>
        <p style=""color: #4d6599;"">You have requested to reset your password. Use the OTP code below:</p>
        <div style=""background-color: white; border: 2px solid #2463eb; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;"">
            <span style=""font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #2463eb;"">{otp}</span>
        </div>
        <p style=""color: #4d6599;"">This code is valid for <strong>15 minutes</strong>.</p>
        <p style=""color: #4d6599;"">If you did not request a password reset, please ignore this email.</p>
    </div>
    <div style=""padding: 15px; text-align: center; color: #999; font-size: 12px;"">
        © 2025 STEM Gear. All rights reserved.
    </div>
</body>
</html>";

        using var client = new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(_senderEmail, _senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}
