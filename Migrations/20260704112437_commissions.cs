using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class commissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_isPaid",
                table: "tbl_CommissionsDue");

            migrationBuilder.RenameColumn(
                name: "sys_commissionId",
                table: "tbl_CommissionPayments",
                newName: "sys_agentId");

            migrationBuilder.AddColumn<DateTime>(
                name: "sys_createdOn",
                table: "tbl_CommissionsDue",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "sys_notes",
                table: "tbl_CommissionsDue",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sys_createdOn",
                table: "tbl_CommissionPayments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "sys_notes",
                table: "tbl_CommissionPayments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sys_recordedByUserId",
                table: "tbl_CommissionPayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sys_reference",
                table: "tbl_CommissionPayments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tbl_CommissionPaymentAllocations",
                columns: table => new
                {
                    sys_allocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_commissionPaymentId = table.Column<int>(type: "int", nullable: false),
                    sys_commissionId = table.Column<int>(type: "int", nullable: false),
                    sys_amountAllocated = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_CommissionPaymentAllocations", x => x.sys_allocationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_CommissionPaymentAllocations");

            migrationBuilder.DropColumn(
                name: "sys_createdOn",
                table: "tbl_CommissionsDue");

            migrationBuilder.DropColumn(
                name: "sys_notes",
                table: "tbl_CommissionsDue");

            migrationBuilder.DropColumn(
                name: "sys_createdOn",
                table: "tbl_CommissionPayments");

            migrationBuilder.DropColumn(
                name: "sys_notes",
                table: "tbl_CommissionPayments");

            migrationBuilder.DropColumn(
                name: "sys_recordedByUserId",
                table: "tbl_CommissionPayments");

            migrationBuilder.DropColumn(
                name: "sys_reference",
                table: "tbl_CommissionPayments");

            migrationBuilder.RenameColumn(
                name: "sys_agentId",
                table: "tbl_CommissionPayments",
                newName: "sys_commissionId");

            migrationBuilder.AddColumn<bool>(
                name: "sys_isPaid",
                table: "tbl_CommissionsDue",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
