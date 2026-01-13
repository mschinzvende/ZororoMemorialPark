namespace ZororoMemorialPark.Services.WhatsApp
{
    public class WhatsAppOptions
    {
        public string VerifyToken { get; set; } = "";
        public string AppSecret { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string PhoneNumberId { get; set; } = "";
        public string GraphApiBase { get; set; } = "https://graph.facebook.com/v20.0";
        public string ReceiptUploadFolder { get; set; } = "App_Data/whatsapp/receipts";
        public int OtpExpiryMinutes { get; set; } = 10;
        public int SessionExpiryMinutes { get; set; } = 15;
    }
}
