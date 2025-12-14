namespace ZororoMemorialPark.Data.Models
{
    public class tbl_PasswordResetCode
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public string Code { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Used { get; set; }
    }
}
