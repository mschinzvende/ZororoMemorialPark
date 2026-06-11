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
        private readonly WhatsAppNotificationService _notificationService;

        public WhatsAppBookingBotService(
            ApplicationDbContext db,
            WhatsAppClient wa,
            IOptions<WhatsAppOptions> opt,
            BurialBookingEngine engine,
            WhatsAppNotificationService notificationService)
        {
            _db = db;
            _wa = wa;
            _opt = opt.Value;
            _engine = engine;
            _notificationService = notificationService;
        }

        public async Task ProcessIncomingAsync(WhatsAppWebhookModels.Message msg)
        {
            var phone = new string(msg.from.Where(char.IsDigit).ToArray());

            var session = await _db.tbl_WhatsAppSessions
                .FirstOrDefaultAsync(s =>
                    s.sys_phone == phone &&
                    s.sys_isActive);

            if (session == null)
            {
                session = new tbl_WhatsAppSession
                {
                    sys_phone = phone,
                    sys_state = WhatsAppStates.New,
                    sys_payload = "{}",
                    sys_isActive = true,
                    sys_lastUpdated = DateTime.UtcNow
                };

                _db.tbl_WhatsAppSessions.Add(session);
                await _db.SaveChangesAsync();
            }

            // SESSION TIMEOUT
            if (session.sys_lastUpdated <
                DateTime.UtcNow.AddMinutes(-_opt.SessionExpiryMinutes))
            {
                session.sys_state = WhatsAppStates.New;
                session.sys_payload = "{}";
            }

            session.sys_lastUpdated = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            if (msg.type == "image")
            {
                await HandleImageMessage(
                    session,
                    msg,
                    phone);

                return;
            }

            var text =
    msg.text?.body?.Trim()
    ?? msg.interactive?.button_reply?.id
    ?? "";


            var agent = await _db.tbl_Agents
    .FirstOrDefaultAsync(a =>
        a.sys_agentPhone == phone &&
        a.sys_isActive);

            if (agent == null)
            {
                await _notificationService.SendTextMessage(
                    phone,
                    "You are not registered as an agent.");

                return;
            }

            if (!agent.sys_whatsAppPinSet)
            {
                if (session.sys_state != WhatsAppStates.SetPin)
                {
                    session.sys_state = WhatsAppStates.SetPin;

                    await _db.SaveChangesAsync();

                    await _notificationService.SendTextMessage(
                        phone,
                        "Welcome. Create a 4 digit PIN.");

                    return;
                }

                if (text.Length != 4 || !text.All(char.IsDigit))
                {
                    await _notificationService.SendTextMessage(
                        phone,
                        "PIN must be exactly 4 digits.");

                    return;
                }

                agent.sys_whatsAppPinHash = HashPin(text);

                agent.sys_whatsAppPinSet = true;

                session.sys_state = WhatsAppStates.New;

                await _db.SaveChangesAsync();

                await _notificationService.SendButtonsMessage(
                phone,
                "Please login to continue.",
                ("LOGIN", "Login"));

                return;
            }


            if (!session.sys_isAuthenticated)
            {
                if (text.Equals("LOGIN",
                    StringComparison.OrdinalIgnoreCase))
                {
                    session.sys_state = WhatsAppStates.EnterPin;

                    await _db.SaveChangesAsync();

                    await _notificationService.SendTextMessage(
                        phone,
                        "Enter PIN");

                    return;
                }

                if (session.sys_state ==
                    WhatsAppStates.EnterPin)
                {
                    if (HashPin(text) ==
                        agent.sys_whatsAppPinHash)
                    {
                        session.sys_isAuthenticated = true;

                        session.sys_agentId =
                            agent.sys_agentId;

                        session.sys_authenticatedUntil =
                            DateTime.UtcNow.AddHours(12);

                        session.sys_state =
                            WhatsAppStates.New;

                        await _db.SaveChangesAsync();

                        await _notificationService.SendTextMessage(
                            phone,
                            "Login successful.");

                        await SendMainMenu(phone);

                        return;
                    }

                    await _notificationService.SendTextMessage(
                        phone,
                        "Invalid PIN");

                    return;
                }

                await _notificationService.SendButtonsMessage(
    phone,
    "Please login to continue",
    ("LOGIN", "Login"));
                return;
            }
            if (session.sys_authenticatedUntil.HasValue &&
    session.sys_authenticatedUntil <
    DateTime.UtcNow)
            {
                session.sys_isAuthenticated = false;

                session.sys_agentId = null;

                await _db.SaveChangesAsync();
            }

            switch (session.sys_state)
            {
                case WhatsAppStates.New:
                    await HandleNewState(session, text, phone);
                    break;

               

                case WhatsAppStates.BookSection:
                    await HandleBookSection(session, text, phone);
                    break;

                case WhatsAppStates.BurialDate:
                    await HandleBurialDate(
                        session,
                        text,
                        phone);
                    break;

                case WhatsAppStates.BookSlot:
                    await HandleBookSlot(session, text, phone);
                    break;

                case WhatsAppStates.DeceasedFirstName:
                    await HandleDeceasedFirstName(session, text, phone);
                    break;

                case WhatsAppStates.DeceasedLastName:
                    await HandleDeceasedLastName(session, text, phone);
                    break;

                case WhatsAppStates.PaymentMadeBy:
                    await HandlePaymentMadeBy(session, text, phone);
                    break;

                case WhatsAppStates.CustomerPhone:
                    await HandleCustomerPhone(session, text, phone);
                    break;

                case WhatsAppStates.ConfirmBooking:
                    await HandleConfirmBooking(session, text, phone);
                    break;

                case WhatsAppStates.PaymentStatus:
                    await HandlePaymentStatus(
                        session,
                        text,
                        phone);
                    break;

                default:
                    session.sys_state = WhatsAppStates.New;
                    await _db.SaveChangesAsync();

                    await _notificationService.SendTextMessage(
                        phone,
                        "Session reset. Type BOOK to start again.");
                    break;
            }
        }


        private WhatsAppBookingSessionData GetData(
    tbl_WhatsAppSession session)
        {
            if (string.IsNullOrWhiteSpace(session.sys_payload))
                return new WhatsAppBookingSessionData();

            return JsonSerializer.Deserialize<WhatsAppBookingSessionData>(
                session.sys_payload)!
                ?? new WhatsAppBookingSessionData();
        }

        private void SaveData(
            tbl_WhatsAppSession session,
            WhatsAppBookingSessionData data)
        {
            session.sys_payload =
                JsonSerializer.Serialize(data);
        }


        private async Task HandleNewState(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                // await SendMainMenu(phone);

                await _notificationService.SendButtonsMessage(
    phone,
    "Welcome to Zororo Memorial.\nSelect an option:",
    ("BOOK", "Book"),
    ("HELP", "Help"));
                return;
            }

            if (text == "HELP")
            {
                await _notificationService.SendTextMessage(
                    phone,
                    "Available commands:\nBOOK - Create booking");

                return;
            }

            if (text != "BOOK")
            {
                await SendMainMenu(phone);
                return;
            }

            var sections = await _engine.GetAvailableSectionsAsync();

            if (!sections.Any())
            {
                await _notificationService.SendTextMessage(
                    phone,
                    "No sections available.");
                return;
            }

            var reply = "Select section:\n\n";

            for (int i = 0; i < sections.Count; i++)
            {
                reply += $"{i + 1}. {sections[i].sys_sectionName} ($ {sections[i].sys_SecPrice})\n";
            }

            session.sys_state = WhatsAppStates.BookSection;

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(phone, reply);
        }


        private async Task HandleImageMessage(
    tbl_WhatsAppSession session,
    WhatsAppWebhookModels.Message msg,
    string phone)
        {
            if (session.sys_state !=
                WhatsAppStates.WaitingReceipt)
            {
                await _notificationService.SendTextMessage(
                    phone,
                    "Image received but not expected.");

                return;
            }

            var mediaId =
                msg.image?.id;

            if (string.IsNullOrWhiteSpace(mediaId))
                return;

            var bytes = await _notificationService.DownloadMediaAsync(mediaId);

            var folder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "receipts");

            Directory.CreateDirectory(folder);

            var fileName =
                $"{Guid.NewGuid()}.jpg";

            var fullPath =
                Path.Combine(folder, fileName);

            await File.WriteAllBytesAsync(
                fullPath,
                bytes);

            var data = GetData(session);

            data.ReceiptFileName = fileName;

            SaveData(session, data);

            await _db.SaveChangesAsync();

            await CreateFinalBooking(
                session,
                phone);
        }


        private async Task CreateFinalBooking(
     tbl_WhatsAppSession session,
     string phone)
        {
            var data = GetData(session);

            var result =
                await _engine.CreateBookingAsync(
                    data.SectionId!.Value,
                    data.SlotIso!,
                    data.DeceasedFirstName!,
                    data.DeceasedLastName!,
                    data.PaymentMadeBy!,
                    data.CustomerPhone!,
                    session.sys_agentId,
                    data.ReceiptFileName,
                    data.PaymentAlreadyMade);

            if (!result.success)
            {
                await _notificationService.SendTextMessage(
                    phone,
                    result.message);

                session.sys_state = WhatsAppStates.New;
                session.sys_payload = "{}";

                return;
            }

           
            session.sys_state = WhatsAppStates.New;
            session.sys_payload = "{}";

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(
                phone,
                $"Booking created successfully.\nReference: {result.booking!.sys_bookingId}");
        }
        private async Task HandleBookSection(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            var sections = await _engine.GetAvailableSectionsAsync();

            if (!int.TryParse(text, out int selected) ||
                selected < 1 ||
                selected > sections.Count)
            {
                await _notificationService.SendTextMessage(
                    phone,
                    "Invalid selection.");
                return;
            }

            var chosen = sections[selected - 1];

            var data = GetData(session);

            data.SectionId = chosen.sys_sectionId;

            SaveData(session, data);

            session.sys_state =
     WhatsAppStates.BurialDate;

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(
                phone,
                "Enter burial date (YYYY-MM-DD)");

        }


        private async Task HandleBurialDate(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            if (!DateOnly.TryParse(text,
                out var burialDate))
            {
                await _notificationService.SendTextMessage(
                    phone,
                    "Invalid date format. Use YYYY-MM-DD");

                return;
            }

            var data = GetData(session);

            data.BurialDate = burialDate;

            SaveData(session, data);

            var slotsResult =
                await _engine.GetAvailableSlotsAsync(
                    burialDate, (int)data.SectionId);

            if (!slotsResult.slots.Any())
            {
                await _notificationService.SendTextMessage(
                    phone,
                    slotsResult.message);

                return;
            }

            var reply =
                $"Available slots for {burialDate:yyyy-MM-dd}\n\n";

            for (int i = 0; i < slotsResult.slots.Count; i++)
            {
                var dt = DateTime.Parse(
                    slotsResult.slots[i]);

                reply += $"{i + 1}. {dt:HH:mm}\n";
            }

            session.sys_state =
                WhatsAppStates.BookSlot;

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(
                phone,
                reply);
        }

        private async Task HandleBookSlot(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            var data = GetData(session);

            var burialDate =
                data.BurialDate!.Value;

            var slotsResult =
                await _engine.GetAvailableSlotsAsync(burialDate, (int)data.SectionId);

            if (!int.TryParse(text, out int selected) ||
                selected < 1 ||
                selected > slotsResult.slots.Count)
            {
                await _notificationService.SendTextMessage(
                    phone,
                    "Invalid slot selection.");
                return;
            }

            var slot = slotsResult.slots[selected - 1];

           

            data.SlotIso = slot;

            SaveData(session, data);

            session.sys_state = WhatsAppStates.DeceasedFirstName;

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(
                phone,
                "Enter deceased first name:");
        }

        private async Task HandleDeceasedFirstName(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            var data = GetData(session);

            data.DeceasedFirstName = text;

            SaveData(session, data);

            session.sys_state =
                WhatsAppStates.DeceasedLastName;

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(
                phone,
                "Enter deceased last name:");
        }

        private async Task HandleDeceasedLastName(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            var data = GetData(session);

            data.DeceasedLastName = text;

            SaveData(session, data);

            session.sys_state =
                WhatsAppStates.PaymentMadeBy;

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(
                phone,
                "Enter payment made by:");
        }

        private async Task HandlePaymentMadeBy(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            var data = GetData(session);

            data.PaymentMadeBy = text;

            SaveData(session, data);

            session.sys_state =
                WhatsAppStates.CustomerPhone;

            await _db.SaveChangesAsync();

            await _notificationService.SendTextMessage(
                phone,
                "Enter Next Of Kin Phone:");
        }


        private async Task HandlePaymentStatus(
    tbl_WhatsAppSession session,
    string text,
    string phone)
        {
            var data = GetData(session);

            if (text == "PAYMENT_YES")
            {
                data.PaymentAlreadyMade = true;

                SaveData(session, data);

                session.sys_state =
                    WhatsAppStates.WaitingReceipt;

                await _db.SaveChangesAsync();

                await _notificationService.SendTextMessage(
                    phone,
                    "Please send a photo of the payment receipt.");

                return;
            }

            if (text == "PAYMENT_NO")
            {
                data.PaymentAlreadyMade = false;

                SaveData(session, data);

                await CreateFinalBooking(
                    session,
                    phone);

                return;
            }
        }

        private async Task HandleCustomerPhone(
      tbl_WhatsAppSession session,
      string text,
      string phone)
        {
            var data = GetData(session);

            data.CustomerPhone = text;

            SaveData(session, data);

            session.sys_state =
                WhatsAppStates.ConfirmBooking;

            await _db.SaveChangesAsync();

            var summary =
        $"""
Confirm booking:
Date:
{data.BurialDate}

Time: 
{data.SlotIso}


Deceased:
{data.DeceasedFirstName} {data.DeceasedLastName}

Next Of Kin Phone:
{data.CustomerPhone}
""";

            await _notificationService.SendButtonsMessage(
                phone,
                summary,
                ("CONFIRM_BOOKING", "Confirm"),
                ("CANCEL_BOOKING", "Cancel"));
        }

        private async Task HandleConfirmBooking(
     tbl_WhatsAppSession session,
     string text,
     string phone)
        {
            if (text == "CANCEL_BOOKING")
            {
                session.sys_state = WhatsAppStates.New;
                session.sys_payload = "{}";

                await _db.SaveChangesAsync();

                await _notificationService.SendTextMessage(
                    phone,
                    "Booking cancelled.");

                return;
            }

            if (text != "CONFIRM_BOOKING")
            {
                await _notificationService.SendButtonsMessage(
                    phone,
                    "Invalid command",
                    ("CONFIRM_BOOKING", "Confirm"),
                    ("CANCEL_BOOKING", "Cancel"));

                return;
            }

            session.sys_state =
                WhatsAppStates.PaymentStatus;

            await _db.SaveChangesAsync();

            await _notificationService.SendButtonsMessage(
                phone,
                "Has payment already been made?",
                ("PAYMENT_YES", "Yes"),
                ("PAYMENT_NO", "No"));
        }

        private string HashPin(string pin)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();

            return Convert.ToHexString(
                sha.ComputeHash(
                    System.Text.Encoding.UTF8.GetBytes(pin)));
        }

        private async Task SendMainMenu(string phone)
        {
            await _notificationService.SendButtonsMessage(
                phone,
                "Welcome to Zororo Memorial.\nSelect an option:",
                ("BOOK", "Book"),
                ("HELP", "Help"));
        }
    }
}
