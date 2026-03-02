using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ZororoMemorialPark.Data.Models;
using Azure.Core;

namespace ZororoMemorialPark.Services
{
    public class NotificationService
    {
        private readonly SystemSettingsService _settings;

        public NotificationService(SystemSettingsService settings)
        {
            _settings = settings;
        }

        //WhatsApp



        
        // EMAIL
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var cfg = await _settings.GetSettingsAsync();

            if (cfg == null ||
                string.IsNullOrWhiteSpace(cfg.SmtpServer) ||
                string.IsNullOrWhiteSpace(cfg.Email) ||
                string.IsNullOrWhiteSpace(cfg.SmtpUsername) ||
                string.IsNullOrWhiteSpace(cfg.SmtpPassword))
            {
                throw new Exception("SMTP configuration is incomplete in SystemSettings.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(cfg.SenderName ?? cfg.Email, cfg.Email));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = body
            };

            try
            {
                using var client = new SmtpClient();

                // Let MailKit figure out best SSL mode
                var secureOption = SecureSocketOptions.Auto;

                // If you KNOW you’re using port 465, you can force it:
                // secureOption = SecureSocketOptions.SslOnConnect;

                await client.ConnectAsync(cfg.SmtpServer, cfg.SmtpPort, secureOption);

                // If the server doesn’t support auth, this will fail clearly.
                await client.AuthenticateAsync(cfg.SmtpUsername, cfg.SmtpPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Bubble the full detail up so you can see it in TempData or logs
                throw new Exception($"MailKit SMTP error: {ex.Message}", ex);
            }
        }


        public async Task SendNextOfKinNotificationAsync(
    string nokEmail,
    string nokPhone,
    string deceasedName,
    DateTime burialDateTime,
    string plot,
    string section)
        {
            var cfg = await _settings.GetSettingsAsync();
            if (cfg == null)
                throw new Exception("System settings not configured.");

            string subject = $"Burial Reminder – {deceasedName}";

            string body = $@"
        <p>Dear Family Member,</p>

        <p>This is a reminder of the upcoming burial:</p>

        <p>
            <strong>Deceased:</strong> {deceasedName}<br>
            <strong>Date & Time:</strong> {burialDateTime:dddd, dd MMM yyyy HH:mm}<br>
            <strong>Grave:</strong> {plot} ({section})
        </p>

        <p>Please ensure necessary arrangements are in place before the scheduled burial time.</p>

        <p>Regards,<br>
        {cfg.CompanyName}</p>
    ";

            // **EMAIL — MailKit**
            if (!string.IsNullOrWhiteSpace(nokEmail))
            {
                try
                {
                    await SendEmailAsync(nokEmail, subject, body);
                }
                catch (Exception ex)
                {
                    // Log or bubble — your choice
                    throw new Exception($"Failed to send NOK email: {ex.Message}", ex);
                }
            }

            // **OPTIONAL — SMS**
            /*
            if (!string.IsNullOrWhiteSpace(nokPhone))
            {
                await SendSmsAsync(nokPhone,
                    $"{deceasedName} burial reminder: {burialDateTime:dd MMM HH:mm}, Plot {plot} ({section})");
            }
            */
        }
    }

    }
