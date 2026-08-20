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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<tbl_RequisitionLineChangeLog>()
                .HasOne(x => x.WorkflowStep)
                .WithMany()
                .HasForeignKey(x => x.WorkflowStepId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<tbl_RequisitionLineChangeLog>()
                .HasOne(x => x.RequisitionLine)
                .WithMany()
                .HasForeignKey(x => x.RequisitionLineId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<tbl_RequisitionLineComment>()
                .HasOne(x => x.WorkflowStep)
                .WithMany()
                .HasForeignKey(x => x.WorkflowStepId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<tbl_RequisitionLineComment>()
                .HasOne(x => x.RequisitionLine)
                .WithMany()
                .HasForeignKey(x => x.RequisitionLineId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<tbl_CommissionsDue>()
    .HasIndex(x => x.sys_agentId);

            modelBuilder.Entity<tbl_CommissionPayments>()
                .HasIndex(x => x.sys_agentId);

            modelBuilder.Entity<tbl_CommissionPaymentAllocations>()
                .HasIndex(x => x.sys_commissionId);

            modelBuilder.Entity<tbl_CommissionPaymentAllocations>()
                .HasIndex(x => x.sys_commissionPaymentId);

            modelBuilder.Entity<tbl_BookingNotificationRecipient>()
                .HasIndex(x => x.UserId)
                .IsUnique();
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
        public DbSet<tbl_Agents> tbl_Agents { get; set; }

        public DbSet<tbl_CommissionPayments> tbl_CommissionPayments { get; set; }

        public DbSet<tbl_CommissionsDue> tbl_CommissionsDue { get; set; }

        public DbSet<tbl_ReceiptBooks> tbl_ReceiptBooks { get; set; }

        public DbSet<tbl_Tombstones> tbl_Tombstones { get; set; }

        public DbSet<tbl_TombstoneInstallations> tbl_TombstoneInstallations { get; set; }

        public DbSet<tbl_TombstoneSales> tbl_TombstoneSales { get; set; }

        public DbSet<tbl_ProductsAndServices> tbl_ProductsAndServices { get; set; }
        public DbSet<tbl_StockTransactions> tbl_StockTransactions { get; set; }

        public DbSet<tbl_EvaluationParameterProducts> tbl_EvaluationParameterProducts { get; set; }

        public DbSet<tbl_JobEvaluationProductUsage> tbl_JobEvaluationProductUsage { get; set; }

        public DbSet<tbl_InventoryAuditTrail> tbl_InventoryAuditTrail { get; set; }

        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<tbl_BookingNotificationRecipient> tbl_BookingNotificationRecipients { get; set; }
        public DbSet<tbl_Requisition> tbl_Requisitions { get; set; }
        public DbSet<tbl_RequisitionLine> tbl_RequisitionLines { get; set; }
        public DbSet<tbl_RequisitionWorkflowDefinition> tbl_RequisitionWorkflowDefinitions { get; set; }
        public DbSet<tbl_RequisitionWorkflowStep> tbl_RequisitionWorkflowSteps { get; set; }
        public DbSet<tbl_RequisitionLineChangeLog> tbl_RequisitionLineChangeLogs { get; set; }
        public DbSet<tbl_RequisitionLineComment> tbl_RequisitionLineComments { get; set; }
        public DbSet<tbl_RequisitionHistory> tbl_RequisitionHistories { get; set; }

        public DbSet<tbl_PasswordResetCode> tbl_PasswordResetCodes { get; set; }

        public DbSet<tbl_BurialBookings> tbl_BurialBookings { get; set; }
        public DbSet<tbl_BurialPayments> tbl_BurialPayments { get; set; }

        public DbSet<tbl_WhatsAppSession> tbl_WhatsAppSessions { get; set; }
        public DbSet<tbl_WhatsAppOtp> tbl_WhatsAppOtps { get; set; }

        public DbSet<tbl_RequisitionActionToken> tbl_RequisitionActionToken { get; set; }

        public DbSet<tbl_CommissionPaymentAllocations> tbl_CommissionPaymentAllocations { get; set; }

    }
}



