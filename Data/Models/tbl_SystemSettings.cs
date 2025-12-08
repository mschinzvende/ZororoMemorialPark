using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }

        // Company Info
        [Required]
        public string CompanyName { get; set; }

        public string LogoPath { get; set; }

        public string PhysicalAddress { get; set; }

        public string Phone { get; set; }

        public string Email { get; set; }

        // SMTP Info
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }

        public string SenderName { get; set; }
        public string SenderEmail { get; set; }

        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool UseSSL { get; set; }

        // Burial rules
        public int MaxBurialsPerDay { get; set; }
        public int MinutesBetweenBurials { get; set; }
    }
}
