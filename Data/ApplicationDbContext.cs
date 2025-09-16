using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define DbSets for your entities here

        public DbSet<tbl_AuditLogs> tbl_AuditLogs { get; set; }
        public DbSet<tbl_Sections> tbl_Sections { get; set; }

        public DbSet<tbl_DevelopmentStages> tbl_DevelopmentStages { get; set; }

        public DbSet<tbl_Graves> tbl_Graves { get; set; }

        public DbSet<tbl_Suppliers> tbl_Suppliers { get; set; }

        public DbSet<tbl_Jobs> tbl_Jobs { get; set; }

        public DbSet<tbl_jobEvaluationParameters> tbl_jobEvaluationParameters { get; set; }

        public DbSet<tbl_JobEvaluations> tbl_JobEvaluations { get; set; }
        public DbSet<tbl_PaymentMethods> tbl_PaymentMethods { get; set; }
        public DbSet<tbl_Expenses> tbl_Expenses { get; set; }

        public DbSet<tbl_Customers> tbl_Customers { get; set; }
        public DbSet<tbl_Deceased> tbl_Deceased { get; set; }
        public DbSet<tbl_GraveAllocations> tbl_GraveAllocations { get; set; }
        public DbSet<tbl_GraveReservations> tbl_GraveReservations { get; set; }
        public DbSet<tbl_Beneficiaries> tbl_Beneficiaries { get; set; }
        public DbSet<tbl_Invoices> tbl_Invoices { get; set; }

        public DbSet<tbl_GraveAllocationInvoices> tbl_GraveAllocationInvoices { get; set; }
        public DbSet<tbl_GraveReservationInvoices> tbl_GraveReservationInvoices { get; set; }

        public DbSet<tbl_InvoiceItems> tbl_InvoiceItems { get; set; }
        public DbSet<tbl_IncomePayments> tbl_IncomePayments { get; set; }


    }
}
