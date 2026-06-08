namespace ZororoMemorialPark.Services.WhatsApp
{
    public class WhatsAppBookingSessionData
    {
        public int? SectionId { get; set; }

        public string? SlotIso { get; set; }

        public string? DeceasedFirstName { get; set; }

        public string? DeceasedLastName { get; set; }

        public string? PaymentMadeBy { get; set; }

        public string? CustomerPhone { get; set; }

        public DateOnly? BurialDate { get; set; }

        public string? ReceiptFileName { get; set; }

        public bool PaymentAlreadyMade { get; set; }
    }
}