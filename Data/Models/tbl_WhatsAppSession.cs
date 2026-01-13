using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{ 
    public class tbl_WhatsAppSession
    {
        [Key]
        public int sys_sessionId { get; set; }

        [Required]
        public string sys_phone { get; set; } = string.Empty;

        public int? sys_agentId { get; set; }

        [Required]
        public string sys_state { get; set; } = "NEW";

        public string? sys_payload { get; set; }

        public bool sys_isActive { get; set; } = true;

        public DateTime sys_lastUpdated { get; set; } = DateTime.UtcNow;
    }
}
