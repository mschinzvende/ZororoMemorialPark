using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services.Bookings;

namespace ZororoMemorialPark.Services.WhatsApp
{
    public class WhatsAppBookingBotService
    {
        private readonly ApplicationDbContext _db;
        private readonly WhatsAppClient _wa;
        private readonly WhatsAppOptions _opt;
        private readonly BurialBookingEngine _engine;

        public WhatsAppBookingBotService(
            ApplicationDbContext db,
            WhatsAppClient wa,
            IOptions<WhatsAppOptions> opt,
            BurialBookingEngine engine)
        {
            _db = db;
            _wa = wa;
            _opt = opt.Value;
            _engine = engine;
        }

        public async Task ProcessIncomingAsync(WhatsAppWebhookModels.Message msg)
        {
            var phone = new string(msg.from.Where(char.IsDigit).ToArray());

            var session = await _db.tbl_WhatsAppSessions
                .FirstOrDefaultAsync(s => s.sys_phone == phone && s.sys_isActive);

            if (session == null)
            {
                session = new tbl_WhatsAppSession
                {
                    sys_phone = phone,
                    sys_state = "NEW"
                };
                _db.tbl_WhatsAppSessions.Add(session);
                await _db.SaveChangesAsync();
            }

            session.sys_lastUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var text = msg.text?.body?.Trim() ?? "";

            if (text.Equals("BOOK", StringComparison.OrdinalIgnoreCase))
            {
                session.sys_state = "BOOK_SECTION";
                session.sys_payload = "{}";
                await _db.SaveChangesAsync();

                var sections = await _engine.GetAvailableSectionsAsync();
                var reply = "Select section:\n" +
                    string.Join("\n",
                        sections.Select((s, i) =>
                            $"{i + 1}. {s.sys_sectionName} ($ {s.sys_SecPrice})"));

                await _wa.SendTextAsync(phone, reply);
            }
        }
    }
}
