using System.Net;
using System.Net.Mail;
using System.Text;
using FoodSales.Application.Interfaces;
using FoodSales.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;

namespace FoodSales.Infrastructure.Services;

public sealed class EmailSender(EmailOptions options, IHostEnvironment environment) : IEmailSender
{
    public bool IsAvailable => options.DeliveryMethod != "Disabled";

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        if (options.DeliveryMethod == "DevelopmentFile" && environment.IsDevelopment())
        {
            // Recovery links stay outside wwwroot and are never returned by the API or written to logs.
            var directory = Path.Combine(environment.ContentRootPath, "App_Data", "mail");
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, $"{Guid.NewGuid():N}.txt"),
                $"To: {recipient}\nSubject: {subject}\n\n{body}", Encoding.UTF8, ct);
            return;
        }
        if (options.DeliveryMethod != "Smtp")
            throw new InvalidOperationException("Email delivery is not configured.");
        using var message = new MailMessage
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };
        message.To.Add(recipient);
        using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(options.Username, options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        await client.SendMailAsync(message, ct);
    }
}
