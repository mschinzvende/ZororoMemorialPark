using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class NewUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhysicalAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpServer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpUsername = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpPassword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UseSSL = table.Column<bool>(type: "bit", nullable: false),
                    waApiUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    waAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    systemUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxBurialsPerDay = table.Column<int>(type: "int", nullable: false),
                    MinutesBetweenBurials = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Agents",
                columns: table => new
                {
                    sys_agentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_agentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_agentContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_agentPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_agentEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_agentAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_commission = table.Column<int>(type: "int", nullable: false),
                    sys_isPercentage = table.Column<bool>(type: "bit", nullable: false),
                    sys_whatsAppPinHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sys_whatsAppPinSet = table.Column<bool>(type: "bit", nullable: false),
                    sys_isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Agents", x => x.sys_agentId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Beneficiaries",
                columns: table => new
                {
                    sys_benefitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_benefitFullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_benefitDOB = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_benefitRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_reservationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Beneficiaries", x => x.sys_benefitId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_BurialBookings",
                columns: table => new
                {
                    sys_bookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_deceasedFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_requestedBurialDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_sectionId = table.Column<int>(type: "int", nullable: false),
                    sys_customerId = table.Column<int>(type: "int", nullable: false),
                    sys_agentId = table.Column<int>(type: "int", nullable: true),
                    sys_bookingStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: true),
                    sys_createdOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_expiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_receiptPath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_BurialBookings", x => x.sys_bookingId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_BurialPayments",
                columns: table => new
                {
                    sys_paymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: false),
                    sys_receiptBookId = table.Column<int>(type: "int", nullable: false),
                    sys_receiptNumber = table.Column<int>(type: "int", nullable: false),
                    sys_amountPaid = table.Column<double>(type: "float", nullable: false),
                    sys_paymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_paymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    sys_capturedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_BurialPayments", x => x.sys_paymentId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_CommissionPayments",
                columns: table => new
                {
                    sys_commissionPaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_commissionId = table.Column<int>(type: "int", nullable: false),
                    sys_paymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_amountPaid = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_CommissionPayments", x => x.sys_commissionPaymentId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_CommissionsDue",
                columns: table => new
                {
                    sys_commissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_graveId = table.Column<int>(type: "int", nullable: false),
                    sys_agentId = table.Column<int>(type: "int", nullable: false),
                    sys_amount = table.Column<double>(type: "float", nullable: false),
                    sys_isPaid = table.Column<bool>(type: "bit", nullable: false),
                    sys_bookingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_CommissionsDue", x => x.sys_commissionId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Customers",
                columns: table => new
                {
                    sys_customerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_paymentMadeBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_customerPhone = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Customers", x => x.sys_customerId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Deceased",
                columns: table => new
                {
                    sys_deceasedId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_deceasedFullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedDOB = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_deceasedDOD = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_deceasedGender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedNextOfKinName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedNextOfKinAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedNextOfKinEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedNextOfKinPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedMaritalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedNameOfSpouse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_deceasedNationality = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Deceased", x => x.sys_deceasedId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_DevelopmentStages",
                columns: table => new
                {
                    sys_stageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_stageName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_stageOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_DevelopmentStages", x => x.sys_stageId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Expenses",
                columns: table => new
                {
                    sys_expenseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_supplierId = table.Column<int>(type: "int", nullable: false),
                    sys_jobId = table.Column<int>(type: "int", nullable: false),
                    sys_notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_amountDue = table.Column<double>(type: "float", nullable: false),
                    sys_amountPaid = table.Column<double>(type: "float", nullable: false),
                    sys_balanceDue = table.Column<double>(type: "float", nullable: false),
                    sys_paymentMethodId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Expenses", x => x.sys_expenseId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_GraveAllocationInvoices",
                columns: table => new
                {
                    sys_GAInvoicesId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: false),
                    sys_allocationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_GraveAllocationInvoices", x => x.sys_GAInvoicesId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_GraveAllocations",
                columns: table => new
                {
                    sys_allocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_graveId = table.Column<int>(type: "int", nullable: false),
                    sys_deceasedId = table.Column<int>(type: "int", nullable: false),
                    sys_allocatedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_customerId = table.Column<int>(type: "int", nullable: false),
                    sys_repatriationSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_dateTimeOfBurial = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_burialOrder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_tombstoneStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_bookingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_GraveAllocations", x => x.sys_allocationId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_GraveReservationInvoices",
                columns: table => new
                {
                    sys_GRInvoicesId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: false),
                    sys_reservationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_GraveReservationInvoices", x => x.sys_GRInvoicesId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_GraveReservations",
                columns: table => new
                {
                    sys_reservationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_graveId = table.Column<int>(type: "int", nullable: false),
                    sys_customerId = table.Column<int>(type: "int", nullable: false),
                    sys_reservedOn = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_GraveReservations", x => x.sys_reservationId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Graves",
                columns: table => new
                {
                    sys_graveId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_stageId = table.Column<int>(type: "int", nullable: false),
                    sys_previousStageId = table.Column<int>(type: "int", nullable: false),
                    sys_sectionId = table.Column<int>(type: "int", nullable: false),
                    sys_plotNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_graveSaleStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_latitude = table.Column<double>(type: "float", nullable: false),
                    sys_longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Graves", x => x.sys_graveId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_IncomePayments",
                columns: table => new
                {
                    sys_incomeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: false),
                    sys_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_amount = table.Column<double>(type: "float", nullable: false),
                    sys_paymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_receiptNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_IncomePayments", x => x.sys_incomeId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_InventoryAuditTrail",
                columns: table => new
                {
                    sys_auditId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_itemId = table.Column<int>(type: "int", nullable: false),
                    sys_jobId = table.Column<int>(type: "int", nullable: false),
                    sys_action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_beforeQty = table.Column<double>(type: "float", nullable: false),
                    sys_afterQty = table.Column<double>(type: "float", nullable: false),
                    sys_changeQty = table.Column<double>(type: "float", nullable: false),
                    sys_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_InventoryAuditTrail", x => x.sys_auditId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_InvoiceItems",
                columns: table => new
                {
                    sys_itemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: false),
                    sys_itemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_itemCost = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_InvoiceItems", x => x.sys_itemId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Invoices",
                columns: table => new
                {
                    sys_invoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_invoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_customerId = table.Column<int>(type: "int", nullable: false),
                    sys_invoiceDueDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Invoices", x => x.sys_invoiceId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_jobEvaluationParameters",
                columns: table => new
                {
                    sys_jobEvaluationParameterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_parameterName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_jobEvaluationParameters", x => x.sys_jobEvaluationParameterId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_JobEvaluationProductUsage",
                columns: table => new
                {
                    sys_usageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_jobId = table.Column<int>(type: "int", nullable: false),
                    sys_jobEvaluationParameterId = table.Column<int>(type: "int", nullable: false),
                    sys_itemId = table.Column<int>(type: "int", nullable: false),
                    sys_quantityUsed = table.Column<double>(type: "float", nullable: false),
                    sys_dateUsed = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_JobEvaluationProductUsage", x => x.sys_usageId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_PasswordResetCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Used = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PasswordResetCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_PaymentMethods",
                columns: table => new
                {
                    sys_paymentMethodId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_paymentMethodName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PaymentMethods", x => x.sys_paymentMethodId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ProductsAndServices",
                columns: table => new
                {
                    sys_itemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_itemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_itemType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_unitCost = table.Column<double>(type: "float", nullable: false),
                    sys_sellingPrice = table.Column<double>(type: "float", nullable: false),
                    sys_stockQuantity = table.Column<double>(type: "float", nullable: false),
                    sys_unitOfMeasure = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ProductsAndServices", x => x.sys_itemId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionActionToken",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionActionToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionWorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequesterRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionWorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Sections",
                columns: table => new
                {
                    sys_sectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_sectionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_jobReminder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_reminderPeriod = table.Column<int>(type: "int", nullable: false),
                    sys_SecPrice = table.Column<double>(type: "float", nullable: false),
                    sys_lastGraveNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Sections", x => x.sys_sectionId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_StockTransactions",
                columns: table => new
                {
                    sys_transactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_itemId = table.Column<int>(type: "int", nullable: false),
                    sys_transactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_transactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_quantity = table.Column<double>(type: "float", nullable: false),
                    sys_unitCost = table.Column<double>(type: "float", nullable: false),
                    sys_referenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_notes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_StockTransactions", x => x.sys_transactionId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Suppliers",
                columns: table => new
                {
                    sys_supplierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_supplierName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_supplierAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_supplierPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_supplierEmail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Suppliers", x => x.sys_supplierId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_TombstoneInstallations",
                columns: table => new
                {
                    sys_installationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_saleId = table.Column<int>(type: "int", nullable: false),
                    sys_installationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_installedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_graveId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_TombstoneInstallations", x => x.sys_installationId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Tombstones",
                columns: table => new
                {
                    sys_tombstoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_tombstoneName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_material = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_size = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_price = table.Column<double>(type: "float", nullable: false),
                    sys_quantityAvailable = table.Column<int>(type: "int", nullable: false),
                    sys_isCustomizable = table.Column<bool>(type: "bit", nullable: false),
                    sys_imagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Tombstones", x => x.sys_tombstoneId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_TombstoneSales",
                columns: table => new
                {
                    sys_saleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_tombstoneId = table.Column<int>(type: "int", nullable: false),
                    sys_saleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_amountPaid = table.Column<double>(type: "float", nullable: false),
                    sys_buyerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_buyerContact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_paymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_installationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_TombstoneSales", x => x.sys_saleId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_WhatsAppOtps",
                columns: table => new
                {
                    sys_otpId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_agentId = table.Column<int>(type: "int", nullable: false),
                    sys_phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_otpHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_expiresOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_isUsed = table.Column<bool>(type: "bit", nullable: false),
                    sys_createdOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_WhatsAppOtps", x => x.sys_otpId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_WhatsAppSessions",
                columns: table => new
                {
                    sys_sessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_agentId = table.Column<int>(type: "int", nullable: true),
                    sys_state = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sys_isActive = table.Column<bool>(type: "bit", nullable: false),
                    sys_lastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_isAuthenticated = table.Column<bool>(type: "bit", nullable: false),
                    sys_authenticatedUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_WhatsAppSessions", x => x.sys_sessionId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ReceiptBooks",
                columns: table => new
                {
                    sys_receiptBookId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_agentId = table.Column<int>(type: "int", nullable: false),
                    sys_bookNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_startNumber = table.Column<int>(type: "int", nullable: false),
                    sys_endNumber = table.Column<int>(type: "int", nullable: false),
                    sys_currentNumber = table.Column<int>(type: "int", nullable: false),
                    sys_issueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_isClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ReceiptBooks", x => x.sys_receiptBookId);
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptBooks_tbl_Agents_sys_agentId",
                        column: x => x.sys_agentId,
                        principalTable: "tbl_Agents",
                        principalColumn: "sys_agentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_EvaluationParameterProducts",
                columns: table => new
                {
                    sys_evalParamProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_jobEvaluationParameterId = table.Column<int>(type: "int", nullable: false),
                    sys_itemId = table.Column<int>(type: "int", nullable: false),
                    sys_defaultQty = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_EvaluationParameterProducts", x => x.sys_evalParamProductId);
                    table.ForeignKey(
                        name: "FK_tbl_EvaluationParameterProducts_tbl_ProductsAndServices_sys_itemId",
                        column: x => x.sys_itemId,
                        principalTable: "tbl_ProductsAndServices",
                        principalColumn: "sys_itemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_EvaluationParameterProducts_tbl_jobEvaluationParameters_sys_jobEvaluationParameterId",
                        column: x => x.sys_jobEvaluationParameterId,
                        principalTable: "tbl_jobEvaluationParameters",
                        principalColumn: "sys_jobEvaluationParameterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowDefinitionId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepType = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionWorkflowSteps_tbl_RequisitionWorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "tbl_RequisitionWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Jobs",
                columns: table => new
                {
                    sys_jobId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_graveId = table.Column<int>(type: "int", nullable: false),
                    sys_supplierId = table.Column<int>(type: "int", nullable: false),
                    sys_stageId = table.Column<int>(type: "int", nullable: false),
                    sys_jobStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_startDate = table.Column<DateOnly>(type: "date", nullable: false),
                    sys_endDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Jobs", x => x.sys_jobId);
                    table.ForeignKey(
                        name: "FK_tbl_Jobs_tbl_DevelopmentStages_sys_stageId",
                        column: x => x.sys_stageId,
                        principalTable: "tbl_DevelopmentStages",
                        principalColumn: "sys_stageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_Jobs_tbl_Graves_sys_graveId",
                        column: x => x.sys_graveId,
                        principalTable: "tbl_Graves",
                        principalColumn: "sys_graveId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_Jobs_tbl_Suppliers_sys_supplierId",
                        column: x => x.sys_supplierId,
                        principalTable: "tbl_Suppliers",
                        principalColumn: "sys_supplierId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Requisitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequesterUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WorkflowDefinitionId = table.Column<int>(type: "int", nullable: false),
                    CurrentStepId = table.Column<int>(type: "int", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Requisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_Requisitions_tbl_RequisitionWorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "tbl_RequisitionWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_Requisitions_tbl_RequisitionWorkflowSteps_CurrentStepId",
                        column: x => x.CurrentStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_JobEvaluations",
                columns: table => new
                {
                    sys_jobEvaluationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_jobEvaluationParameterId = table.Column<int>(type: "int", nullable: false),
                    sys_jobId = table.Column<int>(type: "int", nullable: false),
                    sys_jobEvaluation = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_JobEvaluations", x => x.sys_jobEvaluationId);
                    table.ForeignKey(
                        name: "FK_tbl_JobEvaluations_tbl_Jobs_sys_jobId",
                        column: x => x.sys_jobId,
                        principalTable: "tbl_Jobs",
                        principalColumn: "sys_jobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_JobEvaluations_tbl_jobEvaluationParameters_sys_jobEvaluationParameterId",
                        column: x => x.sys_jobEvaluationParameterId,
                        principalTable: "tbl_jobEvaluationParameters",
                        principalColumn: "sys_jobEvaluationParameterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionHistories_tbl_RequisitionWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionHistories_tbl_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "tbl_Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LatestComment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLines_tbl_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "tbl_Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionLineChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionLineId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionLineChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineChangeLogs_tbl_RequisitionLines_RequisitionLineId",
                        column: x => x.RequisitionLineId,
                        principalTable: "tbl_RequisitionLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineChangeLogs_tbl_RequisitionWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionLineComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionLineId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionLineComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineComments_tbl_RequisitionLines_RequisitionLineId",
                        column: x => x.RequisitionLineId,
                        principalTable: "tbl_RequisitionLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineComments_tbl_RequisitionWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_EvaluationParameterProducts_sys_itemId",
                table: "tbl_EvaluationParameterProducts",
                column: "sys_itemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_EvaluationParameterProducts_sys_jobEvaluationParameterId",
                table: "tbl_EvaluationParameterProducts",
                column: "sys_jobEvaluationParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JobEvaluations_sys_jobEvaluationParameterId",
                table: "tbl_JobEvaluations",
                column: "sys_jobEvaluationParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JobEvaluations_sys_jobId",
                table: "tbl_JobEvaluations",
                column: "sys_jobId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Jobs_sys_graveId",
                table: "tbl_Jobs",
                column: "sys_graveId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Jobs_sys_stageId",
                table: "tbl_Jobs",
                column: "sys_stageId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Jobs_sys_supplierId",
                table: "tbl_Jobs",
                column: "sys_supplierId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptBooks_sys_agentId",
                table: "tbl_ReceiptBooks",
                column: "sys_agentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionHistories_RequisitionId",
                table: "tbl_RequisitionHistories",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionHistories_WorkflowStepId",
                table: "tbl_RequisitionHistories",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineChangeLogs_RequisitionLineId",
                table: "tbl_RequisitionLineChangeLogs",
                column: "RequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineChangeLogs_WorkflowStepId",
                table: "tbl_RequisitionLineChangeLogs",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineComments_RequisitionLineId",
                table: "tbl_RequisitionLineComments",
                column: "RequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineComments_WorkflowStepId",
                table: "tbl_RequisitionLineComments",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLines_RequisitionId",
                table: "tbl_RequisitionLines",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Requisitions_CurrentStepId",
                table: "tbl_Requisitions",
                column: "CurrentStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Requisitions_WorkflowDefinitionId",
                table: "tbl_Requisitions",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionWorkflowSteps_WorkflowDefinitionId",
                table: "tbl_RequisitionWorkflowSteps",
                column: "WorkflowDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "tbl_AuditLogs");

            migrationBuilder.DropTable(
                name: "tbl_Beneficiaries");

            migrationBuilder.DropTable(
                name: "tbl_BurialBookings");

            migrationBuilder.DropTable(
                name: "tbl_BurialPayments");

            migrationBuilder.DropTable(
                name: "tbl_CommissionPayments");

            migrationBuilder.DropTable(
                name: "tbl_CommissionsDue");

            migrationBuilder.DropTable(
                name: "tbl_Customers");

            migrationBuilder.DropTable(
                name: "tbl_Deceased");

            migrationBuilder.DropTable(
                name: "tbl_EvaluationParameterProducts");

            migrationBuilder.DropTable(
                name: "tbl_Expenses");

            migrationBuilder.DropTable(
                name: "tbl_GraveAllocationInvoices");

            migrationBuilder.DropTable(
                name: "tbl_GraveAllocations");

            migrationBuilder.DropTable(
                name: "tbl_GraveReservationInvoices");

            migrationBuilder.DropTable(
                name: "tbl_GraveReservations");

            migrationBuilder.DropTable(
                name: "tbl_IncomePayments");

            migrationBuilder.DropTable(
                name: "tbl_InventoryAuditTrail");

            migrationBuilder.DropTable(
                name: "tbl_InvoiceItems");

            migrationBuilder.DropTable(
                name: "tbl_Invoices");

            migrationBuilder.DropTable(
                name: "tbl_JobEvaluationProductUsage");

            migrationBuilder.DropTable(
                name: "tbl_JobEvaluations");

            migrationBuilder.DropTable(
                name: "tbl_PasswordResetCodes");

            migrationBuilder.DropTable(
                name: "tbl_PaymentMethods");

            migrationBuilder.DropTable(
                name: "tbl_ReceiptBooks");

            migrationBuilder.DropTable(
                name: "tbl_RequisitionActionToken");

            migrationBuilder.DropTable(
                name: "tbl_RequisitionHistories");

            migrationBuilder.DropTable(
                name: "tbl_RequisitionLineChangeLogs");

            migrationBuilder.DropTable(
                name: "tbl_RequisitionLineComments");

            migrationBuilder.DropTable(
                name: "tbl_Sections");

            migrationBuilder.DropTable(
                name: "tbl_StockTransactions");

            migrationBuilder.DropTable(
                name: "tbl_TombstoneInstallations");

            migrationBuilder.DropTable(
                name: "tbl_Tombstones");

            migrationBuilder.DropTable(
                name: "tbl_TombstoneSales");

            migrationBuilder.DropTable(
                name: "tbl_WhatsAppOtps");

            migrationBuilder.DropTable(
                name: "tbl_WhatsAppSessions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "tbl_ProductsAndServices");

            migrationBuilder.DropTable(
                name: "tbl_Jobs");

            migrationBuilder.DropTable(
                name: "tbl_jobEvaluationParameters");

            migrationBuilder.DropTable(
                name: "tbl_Agents");

            migrationBuilder.DropTable(
                name: "tbl_RequisitionLines");

            migrationBuilder.DropTable(
                name: "tbl_DevelopmentStages");

            migrationBuilder.DropTable(
                name: "tbl_Graves");

            migrationBuilder.DropTable(
                name: "tbl_Suppliers");

            migrationBuilder.DropTable(
                name: "tbl_Requisitions");

            migrationBuilder.DropTable(
                name: "tbl_RequisitionWorkflowSteps");

            migrationBuilder.DropTable(
                name: "tbl_RequisitionWorkflowDefinitions");
        }
    }
}
