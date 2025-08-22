using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ZororoMemorialPark.Services
{
  
    public class EmailService 
    {
        public string SmtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER");
        public int Port = Int32.Parse(Environment.GetEnvironmentVariable("SMTP_PORT"));
        public string SenderName = Environment.GetEnvironmentVariable("SMTP_SENDER_NAME");
        public string SenderEmail = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL");
        public string Username = Environment.GetEnvironmentVariable("SMTP_SENDER_EMAIL");
        public string Password = Environment.GetEnvironmentVariable("SMTP_SENDER_PASSWORD");
        public bool UseSSL  = bool.Parse(Environment.GetEnvironmentVariable("USE_SSL"));

        public void SendEmail(string to, string subject, string body, string attachmentPath = null)
        {
            try
            {
                using (var client = new SmtpClient(SmtpServer, Port))
                {
                    client.Credentials = new NetworkCredential(Username, Password);
                    client.EnableSsl = UseSSL;

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(SenderEmail, SenderName);
                        message.To.Add(to);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        if (!string.IsNullOrEmpty(attachmentPath))
                        {
                            message.Attachments.Add(new Attachment(attachmentPath));
                        }

                        Console.WriteLine($"Sending email to {to} via {SmtpServer}:{Port} (SSL={UseSSL})...");
                        client.SendMailAsync(message);
                        Console.WriteLine("Email sent successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;

            }
        }
            
    }
}
