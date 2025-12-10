using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using ZororoMemorialPark.Services;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Tools
{
    public class SmtpTestModel : PageModel
    {
        private readonly NotificationService _notify;
        private readonly SystemSettingsService _settingsService;

        public SmtpTestModel(NotificationService notify, SystemSettingsService settingsService)
        {
            _notify = notify;
            _settingsService = settingsService;
        }

        [BindProperty]
        public TestEmailInput Input { get; set; }

        public class TestEmailInput
        {
            public string ToEmail { get; set; }
            public string Subject { get; set; }
            public string Message { get; set; }
        }

        public SystemSettings Settings { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            Settings = await _settingsService.GetSettingsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Settings = await _settingsService.GetSettingsAsync();

            try
            {
                await _notify.SendEmailAsync(Input.ToEmail, Input.Subject, Input.Message);

                SuccessMessage = "Email sent successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    ex.Message + "\n\n" +
                    (ex.InnerException?.Message ?? "");
            }

            return Page();
        }
    }
}
