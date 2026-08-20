using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_BookingNotificationRecipient
    {
        [Key]
        public int sys_bookingNotificationRecipientId { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
    }
}
