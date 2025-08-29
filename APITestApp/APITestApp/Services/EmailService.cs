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

        public async Task SendSelectedLogsAsync(List<Dictionary<string, object>> logs)
        {
            if (logs == null || !logs.Any())
                throw new ArgumentException("No logs selected");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            var recipient = "fakultetmarija@gmail.com";
            email.To.Add(MailboxAddress.Parse(recipient));

            email.Subject = "Selected API Logs Report";

            var bodyHtml = "<p>Dear Team,</p>";
            bodyHtml += "<p>The following API logs have been selected:</p>";

            foreach (var log in logs)
            {
                var apiName = log.GetValueOrDefault("api_name")?.ToString() ?? "Unknown API";
                var status = log.GetValueOrDefault("status")?.ToString() ?? "-";
                var error = log.GetValueOrDefault("error")?.ToString() ?? "None";
                var responseTime = log.GetValueOrDefault("response_time")?.ToString() ?? "-";
                var timestamp = log.GetValueOrDefault("timestamp")?.ToString() ?? "-";

                bodyHtml += $"<p><b>API Name: {apiName}</b><br/>" +
                            $"Status: {status}<br/>" +
                            $"Error: {error}<br/>" +
                            $"Response Time: {responseTime} ms<br/>" +
                            $"Timestamp: {timestamp}</p>";

                bodyHtml += "<hr/>"; 
            }

            bodyHtml += "<p>This report is intended to help monitor system health and support timely troubleshooting.</p>";
            bodyHtml += "<p>Best regards,<br/>API Monitoring System</p>";

            var builder = new BodyBuilder { HtmlBody = bodyHtml };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _settings.SmtpServer,
                _settings.Port,
                _settings.UseSSL
                    ? MailKit.Security.SecureSocketOptions.SslOnConnect
                    : MailKit.Security.SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }


    }
}
