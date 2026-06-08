using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ZororoMemorialPark.Services.WhatsApp
{
    public class WhatsAppClient
    {
        private readonly HttpClient _http;
        private readonly WhatsAppOptions _opt;

        public WhatsAppClient(HttpClient http, IOptions<WhatsAppOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        public async Task SendTextAsync(string phone, string message)
        {
            var url = $"{_opt.GraphApiBase}/{_opt.PhoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "text",
                text = new { body = message }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _opt.AccessToken);

            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            await _http.SendAsync(req);
        }

        public async Task<byte[]> DownloadMediaAsync(string mediaId)
        {
            var metaReq = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_opt.GraphApiBase}/{mediaId}");

            metaReq.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _opt.AccessToken);

            var metaResp = await _http.SendAsync(metaReq);
            var metaJson = await metaResp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(metaJson);
            var url = doc.RootElement.GetProperty("url").GetString();

            var fileReq = new HttpRequestMessage(HttpMethod.Get, url);
            fileReq.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _opt.AccessToken);

            var fileResp = await _http.SendAsync(fileReq);
            return await fileResp.Content.ReadAsByteArrayAsync();
        }

        public async Task SendButtonsAsync(
    string phone,
    string body,
    params (string Id, string Title)[] buttons)
        {
            var url =
                $"{_opt.GraphApiBase}/{_opt.PhoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = phone,
                type = "interactive",
                interactive = new
                {
                    type = "button",
                    body = new
                    {
                        text = body
                    },
                    action = new
                    {
                        buttons = buttons.Select(x => new
                        {
                            type = "reply",
                            reply = new
                            {
                                id = x.Id,
                                title = x.Title
                            }
                        })
                    }
                }
            };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                url);

            req.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _opt.AccessToken);

            req.Content =
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

            var response = await _http.SendAsync(req);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine("BUTTON RESPONSE:");
            Console.WriteLine(responseBody);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("BUTTON SEND FAILED");
            }
        }

    }
}
