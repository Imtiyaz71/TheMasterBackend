using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using TheMaster.Application.Interfaces;

namespace TheMaster.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var host = _config["EmailSettings:Host"];
                var portStr = _config["EmailSettings:Port"];
                var senderEmail = _config["EmailSettings:SenderEmail"] ?? "no-reply@themaster.com";
                var senderName = _config["EmailSettings:SenderName"] ?? "The Master Academy";
                var username = _config["EmailSettings:Username"];
                var password = _config["EmailSettings:Password"];
                var enableSsl = bool.TryParse(_config["EmailSettings:EnableSsl"], out var ssl) && ssl;

                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || host.Contains("yourdomain.com", StringComparison.OrdinalIgnoreCase) || host.Contains("example.com", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Email settings contain placeholder values or are unconfigured. Email to {ToEmail} with subject '{Subject}' was simulated.", toEmail, subject);
                    return true;
                }

                int port = int.TryParse(portStr, out var p) ? p : 587;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(toEmail, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None);

                if (!string.IsNullOrWhiteSpace(password))
                {
                    await client.AuthenticateAsync(username, password);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'.", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
                return false;
            }
        }

        public async Task<bool> SendCourseEnrollmentEmailAsync(string studentName, string studentEmail, string courseTitle, string driveFolderLink)
        {
            var subject = $"Welcome to {courseTitle}! Your Course Access Details";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; }}
        .card {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }}
        .header {{ background: #1e3a8a; color: #ffffff; padding: 24px; text-align: center; }}
        .content {{ padding: 30px; color: #334155; line-height: 1.6; }}
        .btn {{ display: inline-block; background: #2563eb; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .footer {{ background: #f8fafc; padding: 16px; text-align: center; font-size: 13px; color: #64748b; }}
        .notice {{ background: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px; margin: 15px 0; font-size: 14px; color: #92400e; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h2>The Master Academy</h2>
            <p>Enrollment Confirmation & Access</p>
        </div>
        <div class='content'>
            <h3>Hello {studentName},</h3>
            <p>Congratulations! Your payment has been received successfully. You are now officially enrolled in <strong>{courseTitle}</strong>.</p>
            
            <p>Here is your Google Drive course folder link:</p>
            <div style='text-align: center;'>
                <a href='{driveFolderLink}' target='_blank' class='btn'>Open Google Drive Folder</a>
            </div>

            <div class='notice'>
                <strong>Important Notice:</strong><br/>
                This Drive folder is private and protected. Our admin will grant full view access to your registered email (<strong>{studentEmail}</strong>) shortly. If you cannot open it immediately, please allow a short moment for the permission to be granted.
            </div>

            <p>If you have any questions or need support, reply directly to this email.</p>
            <p>Best regards,<br/><strong>The Master Team</strong></p>
        </div>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} The Master Academy. All rights reserved.
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(studentEmail, subject, body);
        }
    }
}
