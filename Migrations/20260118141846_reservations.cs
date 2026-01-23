using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class reservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "sys_reservedOn",
                table: "tbl_GraveReservations",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AlterColumn<int>(
                name: "sys_allocationId",
                table: "tbl_GraveAllocationInvoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "sys_bookingId",
                table: "tbl_CommissionsDue",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "sys_receiptPath",
                table: "tbl_BurialBookings",
                type: "nvarchar(max)",
                nullable: true);

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
                    sys_lastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_WhatsAppSessions", x => x.sys_sessionId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_WhatsAppOtps");

            migrationBuilder.DropTable(
                name: "tbl_WhatsAppSessions");

            migrationBuilder.DropColumn(
                name: "sys_reservedOn",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_bookingId",
                table: "tbl_CommissionsDue");

            migrationBuilder.DropColumn(
                name: "sys_receiptPath",
                table: "tbl_BurialBookings");

            migrationBuilder.AlterColumn<int>(
                name: "sys_allocationId",
                table: "tbl_GraveAllocationInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
