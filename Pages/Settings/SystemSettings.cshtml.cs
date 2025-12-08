using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZororoMemorialPark.Data;
using System.Net.Mail;
using System.Net;
using ZororoMemorialPark.Data.Models;

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
            var settings = _context.SystemSettings.FirstOrDefault();

            try
            {
                var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
                    EnableSsl = settings.UseSSL
                };

                var mail = new MailMessage(settings.SenderEmail, settings.Email)
                {
                    Subject = "SMTP Test Email",
                    Body = "This is a test email from Zororo Memorial System."
                };

                await client.SendMailAsync(mail);

                TempData["Message"] = "? Test email sent successfully!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "SMTP Error: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
