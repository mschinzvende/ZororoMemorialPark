using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class bookingsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_BurialBookings",
                columns: table => new
                {
                    sys_bookingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_deceasedFirstName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    sys_deceasedLastName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    sys_requestedBurialDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_agentId = table.Column<int>(type: "int", nullable: true),
                    sys_customerId = table.Column<int>(type: "int", nullable: false),
                    sys_bookingStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    sys_invoiceId = table.Column<int>(type: "int", nullable: true),
                    sys_createdOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_expiresOn = table.Column<DateTime>(type: "datetime2", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_BurialBookings");

            migrationBuilder.DropTable(
                name: "tbl_BurialPayments");
        }
    }
}
