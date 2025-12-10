using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZororoMemorialPark.Data;
using System.Net.Mail;
using System.Net;
using ZororoMemorialPark.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ZororoMemorialPark.Pages.Settings
{
    public class SystemSettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SystemSettingsModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public SystemSettings Settings { get; set; }

        [BindProperty]
        public IFormFile LogoFile { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Settings = _context.SystemSettings.FirstOrDefault();

            if (Settings == null)
            {
                Settings = new SystemSettings
                {
                    MaxBurialsPerDay = 3,
                    MinutesBetweenBurials = 60
                };

                _context.SystemSettings.Add(Settings);
                await _context.SaveChangesAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var settingsInDb = _context.SystemSettings.FirstOrDefault();
            if (settingsInDb == null)
                return NotFound();

            // Update values
            settingsInDb.CompanyName = Settings.CompanyName;
            settingsInDb.PhysicalAddress = Settings.PhysicalAddress;
            settingsInDb.Phone = Settings.Phone;
            settingsInDb.Email = Settings.Email;

            settingsInDb.SmtpServer = Settings.SmtpServer;
            settingsInDb.SmtpPort = Settings.SmtpPort;
            settingsInDb.SenderName = Settings.SenderName;
            settingsInDb.SenderEmail = Settings.SenderEmail;
            settingsInDb.SmtpUsername = Settings.SmtpUsername;
            if(Settings.SmtpPassword != null)
            {
                settingsInDb.SmtpPassword = Settings.SmtpPassword;
            }
            
            settingsInDb.UseSSL = Settings.UseSSL;

            settingsInDb.MaxBurialsPerDay = Settings.MaxBurialsPerDay;
            settingsInDb.MinutesBetweenBurials = Settings.MinutesBetweenBurials;

            // Upload Logo
            if (LogoFile != null)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = "company_logo" + Path.GetExtension(LogoFile.FileName);
                string fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await LogoFile.CopyToAsync(stream);
                }

                settingsInDb.LogoPath = "/uploads/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Settings updated successfully!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTestEmailAsync()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                TempData["Message"] = "SMTP Error: No system settings found.";
                return RedirectToPage();
            }

            try
            {
                using var smtp = new MailKit.Net.Smtp.SmtpClient();

                // Better debugging – shows real errors from cPanel
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                if (settings.UseSSL)
                {
                    await smtp.ConnectAsync(settings.SmtpServer, settings.SmtpPort, MailKit.Security.SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    await smtp.ConnectAsync(settings.SmtpServer, settings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                }

                await smtp.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPassword);

                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress(settings.SenderName, settings.SenderEmail));
                message.To.Add(new MimeKit.MailboxAddress(settings.CompanyName, settings.Email));
                message.Subject = "SMTP Test Email";

                message.Body = new MimeKit.TextPart("html")
                {
                    Text = "This is a test email from <strong>Zororo Memorial System</strong>."
                };

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                TempData["Message"] = "? Test email sent successfully!";
            }
            catch (Exception ex)
            {
                // Shows the REAL SMTP error
                TempData["Message"] = "? SMTP Error: " + ex.Message;
            }

            return RedirectToPage();
        }

    }
}
