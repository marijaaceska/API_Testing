using MimeKit;
using MailKit.Net.Smtp;
using APITestApp.Models;
using Microsoft.Extensions.Options;

namespace APITestApp.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string subject, string body, byte[] attachmentBytes, string attachmentName)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.SenderEmail, _settings.SenderEmail));
            var recipient = "fakultetmarija@gmail.com";
            email.To.Add(MailboxAddress.Parse(recipient));
            email.Subject = subject;

            var builder = new BodyBuilder { TextBody = body };

            if (attachmentBytes != null && attachmentBytes.Length > 0)
            {
                builder.Attachments.Add(attachmentName, attachmentBytes);
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port,
                _settings.UseSSL ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
