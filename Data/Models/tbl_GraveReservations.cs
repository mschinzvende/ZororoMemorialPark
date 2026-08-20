using System.ComponentModel.DataAnnotations;

namespace ZororoMemorialPark.Data.Models
{
    public class tbl_GraveReservations
    {
        [Key]
        public int sys_reservationId { get; set; }
        // A reservation can exist before a physical grave is selected.
        public int? sys_graveId { get; set; }
        public int sys_customerId { get; set; }
        public DateOnly sys_reservedOn { get; set; }
        public int? sys_preferredSectionId { get; set; }
        public string sys_reservationReference { get; set; } = string.Empty;
        public double sys_agreedPrice { get; set; }
        public string sys_status { get; set; } = "Active";
        public DateOnly? sys_nextInstallmentDueDate { get; set; }
        public DateOnly? sys_expiryDate { get; set; }
        public DateOnly? sys_lastInstallmentReminderSentOn { get; set; }
    }
}
