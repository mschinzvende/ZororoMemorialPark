using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Pages.Integrations.WhatsApp
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
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

        /*public IActionResult OnGet()
        {
            var mode = Request.Query["hub.mode"].ToString();
            var token = Request.Query["hub.verify_token"].ToString();
            var challenge = Request.Query["hub.challenge"].ToString();

            Console.WriteLine($"MODE: '{mode}'");
            Console.WriteLine($"TOKEN: '{token}'");
            Console.WriteLine($"EXPECTED: '{_options.VerifyToken}'");

            return Content($"mode={mode}, token={token}, expected={_options.VerifyToken}, challenge={challenge}", "text/plain");
        }
      */
        public IActionResult OnGet()
        {
            var mode = Request.Query["hub.mode"].ToString();
            var token = Request.Query["hub.verify_token"].ToString();
            var challenge = Request.Query["hub.challenge"].ToString();

            if (mode == "subscribe" && token == _options.VerifyToken)
            {
                return Content(challenge, "text/plain");
            }

            return Content("INVALID", "text/plain"); // avoid HTML fallback
        }

        // INCOMING WHATSAPP MESSAGES (POST)
        public async Task<IActionResult> OnPostAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            Console.WriteLine("RAW WEBHOOK:");
            Console.WriteLine(body);

            if (!IsValidSignature(body))
            {
                Console.WriteLine("INVALID SIGNATURE");
                return new StatusCodeResult(401);
            }

            var payload = WhatsAppWebhookModels.Parse(body);

            if (payload == null)
            {
                Console.WriteLine("FAILED TO PARSE PAYLOAD");
                return new OkResult();
            }

            var message = payload.GetFirstMessage();

            if (message != null)
            {
                Console.WriteLine($"MESSAGE FROM: {message.from}");
                await _bot.ProcessIncomingAsync(message);
            }

            return new OkResult();
        }

        private bool IsValidSignature(string body)
        {
            if (string.IsNullOrWhiteSpace(_options.AppSecret))
                return true;

            if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var sig))
                return true; // allow missing during testing

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