using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Pages.Integrations.WhatsApp
{
    public class WebhookModel : PageModel
    {
        private readonly WhatsAppOptions _options;
        private readonly WhatsAppBookingBotService _bot;

        public WebhookModel(
            IOptions<WhatsAppOptions> options,
            WhatsAppBookingBotService bot)
        {
            _options = options.Value;
            _bot = bot;
        }

        



        // META VERIFICATION (GET)
        public IActionResult OnGet(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.verify_token")] string token,
            [FromQuery(Name = "hub.challenge")] string challenge)
        {
            if (mode == "subscribe" && token == _options.VerifyToken)
                return Content(challenge);

            return Unauthorized();
        }

        // INCOMING WHATSAPP MESSAGES (POST)
        public async Task<IActionResult> OnPostAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (!IsValidSignature(body))
                return Unauthorized();

            var payload = WhatsAppWebhookModels.Parse(body);
            var message = payload?.GetFirstMessage();

            if (message != null)
                await _bot.ProcessIncomingAsync(message);

            return new OkResult();
        }

        private bool IsValidSignature(string body)
        {
            if (string.IsNullOrWhiteSpace(_options.AppSecret))
                return true;

            if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var sig))
                return false;

            var expected = sig.ToString().Replace("sha256=", "");

            using var hmac = new HMACSHA256(
                Encoding.UTF8.GetBytes(_options.AppSecret));

            var hash = Convert.ToHexString(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(body)))
                .ToLowerInvariant();

            return hash == expected.ToLowerInvariant();
        }
    }
}
