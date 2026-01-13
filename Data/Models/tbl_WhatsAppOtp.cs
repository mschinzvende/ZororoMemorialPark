using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_WhatsAppOtp
    {
        [Key]
        public int sys_otpId { get; set; }

        public int sys_agentId { get; set; }

        [Required]
        public string sys_phone { get; set; } = string.Empty;

        [Required]
        public string sys_otpHash { get; set; } = string.Empty;

        public DateTime sys_expiresOn { get; set; }

        public bool sys_isUsed { get; set; } = false;

        public DateTime sys_createdOn { get; set; } = DateTime.UtcNow;
    }
}
