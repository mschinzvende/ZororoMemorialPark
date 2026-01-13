using System.Text.Json;

namespace ZororoMemorialPark.Services.WhatsApp
{
    public static class WhatsAppWebhookModels
    {
        public static WhatsAppPayload? Parse(string json)
            => JsonSerializer.Deserialize<WhatsAppPayload>(json);

        public class WhatsAppPayload
        {
            public List<Entry> entry { get; set; } = new();

            public Message? GetFirstMessage()
            {
                return entry
                    .SelectMany(e => e.changes)
                    .SelectMany(c => c.value.messages ?? new())
                    .FirstOrDefault();
            }
        }

        public class Entry
        {
            public List<Change> changes { get; set; } = new();
        }

        public class Change
        {
            public Value value { get; set; } = new();
        }

        public class Value
        {
            public List<Message>? messages { get; set; }
        }

        public class Message
        {
            public string from { get; set; } = "";
            public string type { get; set; } = "";
            public Text? text { get; set; }
            public Media? document { get; set; }
            public Media? image { get; set; }
        }

        public class Text
        {
            public string body { get; set; } = "";
        }

        public class Media
        {
            public string id { get; set; } = "";
        }
    }
}
