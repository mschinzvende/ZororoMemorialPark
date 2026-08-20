
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;


namespace ZororoMemorialPark.Services.WhatsApp
{
    public class WhatsAppNotificationService
    {
        private const string InternalBookingNotificationTemplateName = "internal_booking_notification";
        private const string InternalBookingNotificationLanguageCode = "en";
        private const string ReservationNotificationTemplateName = "reservation_created";
        private const string ReservationInstallmentReminderTemplateName = "reservation_installment_reminder";

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



        public async Task<HttpResponseMessage> SendBookingNotification(
    string to,
    string templateName,
    string deceasedName,
    string burialDate,
    string burialSection,
    string customerName)
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
                        new { type = "text", text=customerName },
                        new { type = "text", text = deceasedName },
                        new { type = "text", text = burialDate },
                        new { type = "text", text=burialSection }
                        
                    }
                },

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


        public async Task<HttpResponseMessage> SendTextMessage(
    string to,
    string message)
        {
            await EnsureInitializedAsync();

            var messageData = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = message
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

        public async Task<HttpResponseMessage> SendReservationNotification(
            string to,
            string reservationReference,
            string customerName,
            string sectionName,
            string agreedPrice,
            string nextInstallmentDueDate)
        {
            return await SendReservationTemplateAsync(
                to,
                ReservationNotificationTemplateName,
                reservationReference,
                customerName,
                sectionName,
                agreedPrice,
                nextInstallmentDueDate);
        }

        public async Task<HttpResponseMessage> SendReservationInstallmentReminder(
            string to,
            string reservationReference,
            string customerName,
            string sectionName,
            string outstandingBalance,
            string dueDate)
        {
            return await SendReservationTemplateAsync(
                to,
                ReservationInstallmentReminderTemplateName,
                reservationReference,
                customerName,
                sectionName,
                outstandingBalance,
                dueDate);
        }

        private async Task<HttpResponseMessage> SendReservationTemplateAsync(
            string to,
            string templateName,
            string reservationReference,
            string customerName,
            string sectionName,
            string amount,
            string dueDate)
        {
            await EnsureInitializedAsync();

            var messageData = new
            {
                messaging_product = "whatsapp",
                to,
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
                                new { type = "text", text = reservationReference },
                                new { type = "text", text = customerName },
                                new { type = "text", text = sectionName },
                                new { type = "text", text = amount },
                                new { type = "text", text = dueDate }
                            }
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(messageData),
                Encoding.UTF8,
                "application/json");

            return await _httpClient.PostAsync("", content);
        }

        public async Task<HttpResponseMessage> SendInternalBookingNotification(
            string to,
            string bookingReference,
            string deceasedName,
            string burialDate,
            string burialSection,
            string payerName,
            string customerPhone)
        {
            await EnsureInitializedAsync();

            var messageData = new
            {
                messaging_product = "whatsapp",
                to,
                type = "template",
                template = new
                {
                    name = InternalBookingNotificationTemplateName,
                    language = new { code = InternalBookingNotificationLanguageCode },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[]
                            {
                                new { type = "text", text = bookingReference },
                                new { type = "text", text = deceasedName },
                                new { type = "text", text = burialDate },
                                new { type = "text", text = burialSection },
                                new { type = "text", text = payerName },
                                new { type = "text", text = customerPhone }
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

        public async Task<HttpResponseMessage> SendPdfMessage(string to, string fileUrl, string templateName, string filename, string parameter1, string parameter2)
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
                    new
                    {
                        type = "header",
                        parameters = new object[]
                        {
                            new
                            {
                                type = "document",
                                document = new
                                {
                                    link =fileUrl,
                                    filename = filename
                                }
                            }
                        }
                    },
                    new
                    {
                        type = "body",
                        parameters = new[]
                        {
                            new { type = "text", text = parameter1 },
                            new { type = "text", text = parameter2 }
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






        public async Task<HttpResponseMessage> SendButtonsMessage(
    string to,
    string body,
    params (string Id, string Title)[] buttons)
        {
            await EnsureInitializedAsync();

            var messageData = new
            {
                messaging_product = "whatsapp",
                to = to,
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

            var content = new StringContent(
                JsonConvert.SerializeObject(messageData),
                Encoding.UTF8,
                "application/json");

            return await _httpClient.PostAsync("", content);
        }


        public async Task<byte[]> DownloadMediaAsync(
    string mediaId)
        {
            await EnsureInitializedAsync();

            var metaResponse =
                await _httpClient.GetAsync(
                    $"https://graph.facebook.com/v23.0/{mediaId}");

            var metaJson =
                await metaResponse.Content.ReadAsStringAsync();

            Console.WriteLine(metaJson);

            using var doc =
                JsonDocument.Parse(metaJson);

            var mediaUrl =
                doc.RootElement
                    .GetProperty("url")
                    .GetString();

            var mediaResponse =
                await _httpClient.GetAsync(mediaUrl);

            return await mediaResponse.Content
                .ReadAsByteArrayAsync();
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
