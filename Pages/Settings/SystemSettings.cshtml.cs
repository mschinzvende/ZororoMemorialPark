using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZororoMemorialPark.Data;
using System.Net.Mail;
using System.Net;
using ZororoMemorialPark.Data.Models;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Settings
{
    [Authorize(Roles = "Manager")]
    public class SystemSettingsModel : PageModel
    {
        private static readonly IReadOnlyDictionary<string, string> BookingNotificationRoleLabels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Manager"] = "Manager (legacy Admin)",
                ["Admin"] = "Manager (legacy Admin)",
                ["FinanceOfficer"] = "Finance Officer (legacy Accountant)",
                ["Accountant"] = "Finance Officer (legacy Accountant)",
                ["OperationsOfficer"] = "Operations Officer (legacy Supervisor)",
                ["Supervisor"] = "Operations Officer (legacy Supervisor)",
                ["Agent"] = "Agent"
            };

        private readonly ApplicationDbContext _context;
        private readonly PersistentStaticFileStorage _fileStorage;

        public SystemSettingsModel(ApplicationDbContext context, PersistentStaticFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        [BindProperty]
        public SystemSettings Settings { get; set; }

        [BindProperty]
        public IFormFile LogoFile { get; set; }

        [BindProperty]
        public List<string> SelectedBookingNotificationUserIds { get; set; } = new();

        public List<BookingNotificationRecipientOption> BookingNotificationUsers { get; set; } = new();

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

            // Credentials are write-only on this page. They are never rendered back to the browser.
            Settings.SmtpPassword = string.Empty;
            Settings.waAccessToken = string.Empty;

            await LoadBookingNotificationUsersAsync();

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
            if (!string.IsNullOrWhiteSpace(Settings.SmtpPassword))
            {
                settingsInDb.SmtpPassword = Settings.SmtpPassword;
            }
            
            settingsInDb.UseSSL = Settings.UseSSL;
            if (!string.IsNullOrWhiteSpace(Settings.waAccessToken))
            {
                settingsInDb.waAccessToken = Settings.waAccessToken;
            }
            settingsInDb.waApiUrl = Settings.waApiUrl;
            settingsInDb.systemUrl = Settings.systemUrl;

            settingsInDb.MaxBurialsPerDay = Settings.MaxBurialsPerDay;
            settingsInDb.MinutesBetweenBurials = Settings.MinutesBetweenBurials;
            settingsInDb.dayStartTime = Settings.dayStartTime;
            settingsInDb.dayEndTime = Settings.dayEndTime;

            await SaveBookingNotificationRecipientsAsync();

            // Upload Logo
            if (LogoFile != null)
            {
                string folder = _fileStorage.GetDirectory("uploads");

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

        private async Task LoadBookingNotificationUsersAsync()
        {
            var selectedUserIds = await _context.tbl_BookingNotificationRecipients
                .AsNoTracking()
                .Select(r => r.UserId)
                .ToListAsync();

            var userRoles = await (
                from user in _context.Users
                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                join role in _context.Roles on userRole.RoleId equals role.Id
                where BookingNotificationRoleLabels.Keys.Contains(role.Name!)
                select new
                {
                    user.Id,
                    UserName = user.UserName ?? user.Email ?? "Unnamed user",
                    user.PhoneNumber,
                    RoleName = role.Name!
                })
                .ToListAsync();

            var selected = selectedUserIds.ToHashSet(StringComparer.Ordinal);

            BookingNotificationUsers = userRoles
                .GroupBy(x => new { x.Id, x.UserName, x.PhoneNumber })
                .Select(group => new BookingNotificationRecipientOption
                {
                    UserId = group.Key.Id,
                    UserName = group.Key.UserName,
                    PhoneNumber = group.Key.PhoneNumber,
                    Roles = group
                        .Select(x => BookingNotificationRoleLabels[x.RoleName])
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList(),
                    IsSelected = selected.Contains(group.Key.Id)
                })
                .OrderBy(x => x.UserName)
                .ToList();
        }

        private async Task SaveBookingNotificationRecipientsAsync()
        {
            await LoadBookingNotificationUsersAsync();

            var eligibleUserIds = BookingNotificationUsers
                .Select(x => x.UserId)
                .ToHashSet(StringComparer.Ordinal);

            var requestedUserIds = (SelectedBookingNotificationUserIds ?? new List<string>())
                .Where(userId => eligibleUserIds.Contains(userId))
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);

            var existingRecipients = await _context.tbl_BookingNotificationRecipients
                .ToListAsync();

            var recipientsToRemove = existingRecipients
                .Where(recipient => !requestedUserIds.Contains(recipient.UserId))
                .ToList();

            _context.tbl_BookingNotificationRecipients.RemoveRange(recipientsToRemove);

            var existingUserIds = existingRecipients
                .Select(recipient => recipient.UserId)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var userId in requestedUserIds.Except(existingUserIds))
            {
                _context.tbl_BookingNotificationRecipients.Add(
                    new tbl_BookingNotificationRecipient { UserId = userId });
            }
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

    public class BookingNotificationRecipientOption
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsSelected { get; set; }
    }
}
