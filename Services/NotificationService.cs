using System.Net;
using System.Net.Mail;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZororoMemorialPark.Services
{
    public class NotificationService
    {
        private readonly SystemSettingsService _settings;

        public NotificationService(SystemSettingsService settings)
        {
            _settings = settings;
        }

        // EMAIL
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var cfg = await _settings.GetSettingsAsync();
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.SmtpServer) ||
                string.IsNullOrWhiteSpace(cfg.Email))
            {
                return;
            }

            var msg = new MailMessage
            {
                From = new MailAddress(cfg.Email, cfg.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            msg.To.Add(to);

            var client = new SmtpClient(cfg.SmtpServer, cfg.SmtpPort)
            {
                Credentials = new NetworkCredential(cfg.SmtpUsername, cfg.SmtpPassword),
                EnableSsl = cfg.UseSSL
            };

            await client.SendMailAsync(msg);
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
            if (cfg == null) return;

            string subject = $"Burial Reminder for {deceasedName}";
            string body =
                $"Dear Family Member,<br><br>" +
                $"This is a reminder of the upcoming burial:<br><br>" +
                $"<strong>Deceased:</strong> {deceasedName}<br>" +
                $"<strong>Date & Time:</strong> {burialDateTime:dddd, dd MMM yyyy HH:mm}<br>" +
                $"<strong>Grave:</strong> {plot} ({section})<br><br>" +
                $"Regards,<br>" +
                $"{cfg.CompanyName}";

            // Send Email if available
            if (!string.IsNullOrWhiteSpace(nokEmail))
            {
                await SendEmailAsync(nokEmail, subject, body);
            }

            // --- OPTIONAL SMS ---
            /*
            if (!string.IsNullOrWhiteSpace(nokPhone))
            {
                await SendSmsAsync(nokPhone, $"{deceasedName} burial reminder: {burialDateTime:dd MMM HH:mm}");
            }
            */
        }


        // SMS
        /* public async Task SendSmsAsync(string phone, string message)
         {
             var cfg = await _settings.GetSettingsAsync();
             if (cfg == null || string.IsNullOrWhiteSpace(cfg.SmsApiUrl) ||
                 string.IsNullOrWhiteSpace(cfg.SmsApiKey) ||
                 string.IsNullOrWhiteSpace(phone))
             {
                 return;
             }

             var payload = new
             {
                 api_key = cfg.SmsApiKey,
                 sender = cfg.SmsSenderId,
                 msg = message,
                 to = phone
             };

             using var client = new HttpClient();
             var content = new StringContent(
                 JsonConvert.SerializeObject(payload),
                 Encoding.UTF8,
                 "application/json");

             await client.PostAsync(cfg.SmsApiUrl, content);
         }*/
    }
}
