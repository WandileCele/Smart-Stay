using System.Net;
using System.Net.Mail;

namespace Smart_Stay.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var settings = _config.GetSection("EmailSettings");

            using var client = new SmtpClient(settings["SmtpHost"], int.Parse(settings["SmtpPort"]!))
            {
                Credentials = new NetworkCredential(settings["SenderEmail"], settings["SenderPassword"]),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(settings["SenderEmail"]!, settings["SenderName"]),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}