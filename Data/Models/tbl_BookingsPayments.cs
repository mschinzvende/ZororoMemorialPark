using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class tbl_BurialBookings
{
    [Key]
    public int sys_bookingId { get; set; }

    public string sys_deceasedFirstName { get; set; }
    public string sys_deceasedLastName { get; set; }

    public DateTime sys_requestedBurialDateTime { get; set; }

    // SECTION SELECTED AT BOOKING TIME
    public int sys_sectionId { get; set; }

    public int sys_customerId { get; set; }
    public int? sys_agentId { get; set; }

    // PendingPayment, Invoiced, PaidPartial, PaidFull, Allocated, Cancelled, Rejected, Expired
    public string sys_bookingStatus { get; set; }

    public int? sys_invoiceId { get; set; }

    public DateTime sys_createdOn { get; set; }
    public DateTime sys_expiresOn { get; set; }
}


public class tbl_BurialPayments
{
    [Key]
    public int sys_paymentId { get; set; }

    [Required]
    public int sys_invoiceId { get; set; }

    [Required]
    public int sys_receiptBookId { get; set; }

    [Required]
    public int sys_receiptNumber { get; set; }

    [Required]
    public double sys_amountPaid { get; set; }

    [Required]
    public DateTime sys_paymentDate { get; set; } = DateTime.Now;

    [Required, MaxLength(50)]
    public string sys_paymentMethod { get; set; } = "Cash";

    [MaxLength(450)]
    public string sys_capturedByUserId { get; set; }
}
