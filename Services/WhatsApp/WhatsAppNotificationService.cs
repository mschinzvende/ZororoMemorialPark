
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Text;


namespace ZororoMemorialPark.Services.WhatsApp
{
    public class WhatsAppNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly SystemSettingsService _settingsService;

        private bool _initialized = false;

        public WhatsAppNotificationService(SystemSettingsService systemSettings)
        {
            _settingsService = systemSettings;
            _httpClient = new HttpClient();
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            var settings = await _settingsService.GetSettingsAsync();

            if (string.IsNullOrWhiteSpace(settings.waApiUrl))
                throw new InvalidOperationException("WhatsApp API URL is not configured in system settings.");

            if (string.IsNullOrWhiteSpace(settings.waAccessToken))
                throw new InvalidOperationException("WhatsApp Access Token is not configured in system settings.");

            _httpClient.BaseAddress = new Uri(settings.waApiUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", settings.waAccessToken);

            _initialized = true;
        }

        public async Task<HttpResponseMessage> SendRequisitionNotificationWithLink(
    string to,
    string templateName,
    string reviewerName,
    string requestNumber,
    string lineSummary,
    string totalCost,
    string requisitionId,
    string actionToken)
        {
            await EnsureInitializedAsync();

            var messageData = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = "en" },
                    components = new object[]
                    {
                // BODY
                new
                {
                    type = "body",
                    parameters = new object[]
                    {
                        new { type = "text", text = reviewerName },
                        new { type = "text", text = requestNumber },
                        new { type = "text", text=lineSummary },
                        new { type = "text", text=totalCost }
                    }
                },

                

                //BUTTON 1 → Accept
                new
                {
                    type = "button",
                    sub_type = "URL",
                    index = "0",
                    parameters = new object[]
                    {
                        new { type = "text", text = actionToken }
                    }
                },

                // BUTTON 2 → Reject
                new
                {
                    type = "button",
                    sub_type = "URL",
                    index ="1",
                    parameters = new object[]
                    {
                        new { type = "text", text = actionToken }
                    }
                }
                    }
                }
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(messageData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("", content);


            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Status: {response.StatusCode}");
            Console.WriteLine($"Response Content: {responseContent}");
            return response;
        }



















        public async Task<HttpResponseMessage> SendCalendarRemider(string to, string message, string fullName, string schoolName, string templateName)
            {

                var messageData = new
                {
                    messaging_product = "whatsapp",
                    to = to,
                    type = "template",
                    template = new
                    {
                        name = templateName,
                        language = new { code = "en" },
                        components = new object[]
                {
                new
                {
                    type = "body",
                    parameters = new object[]
                    {
                        new { type = "text", text = fullName },
                        new { type = "text", text = message },

                        new { type = "text", text = schoolName }
                    }
                }
                }
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(messageData), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("", content);


                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {responseContent}");
                return response;
            }


          



        }
    }
