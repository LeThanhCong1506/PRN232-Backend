namespace MV.ApplicationLayer.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string otp);
}
