using System.Net;
using System.Net.Mail;

namespace StudShare.Api.Services;

public class EmailService(IConfiguration config)
{
    private string Host => Environment.GetEnvironmentVariable("EMAIL_HOST")
        ?? config["Email:Host"]
        ?? throw new InvalidOperationException("EMAIL_HOST is not configured.");

    private int Port => int.TryParse(
        Environment.GetEnvironmentVariable("EMAIL_PORT") ?? config["Email:Port"],
        out var p) ? p : 587;

    private string User => Environment.GetEnvironmentVariable("EMAIL_USER")
        ?? config["Email:User"]
        ?? throw new InvalidOperationException("EMAIL_USER is not configured.");

    private string Password => Environment.GetEnvironmentVariable("EMAIL_PASSWORD")
        ?? throw new InvalidOperationException("EMAIL_PASSWORD is not configured.");

    private string From => Environment.GetEnvironmentVariable("EMAIL_FROM")
        ?? config["Email:From"]
        ?? User;

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(Host, Port)
        {
            Credentials = new NetworkCredential(User, Password),
            EnableSsl = true,
        };

        var message = new MailMessage(From, to, subject, htmlBody)
        {
            IsBodyHtml = true,
        };

        await client.SendMailAsync(message);
    }

    public Task SendVerificationCodeAsync(string to, string code) =>
        SendAsync(to, "StudShare — підтвердження email",
            $"""
            <p>Ваш код підтвердження:</p>
            <h2 style="letter-spacing:4px">{code}</h2>
            <p>Код дійсний 15 хвилин.</p>
            """);

    public Task SendPasswordResetCodeAsync(string to, string code) =>
        SendAsync(to, "StudShare — скидання паролю",
            $"""
            <p>Ваш код для скидання паролю:</p>
            <h2 style="letter-spacing:4px">{code}</h2>
            <p>Код дійсний 15 хвилин. Якщо ви не запитували скидання — проігноруйте цей лист.</p>
            """);
}
