using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleReservationsFromGraves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "sys_graveId",
                table: "tbl_GraveReservations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<double>(
                name: "sys_agreedPrice",
                table: "tbl_GraveReservations",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "sys_expiryDate",
                table: "tbl_GraveReservations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "sys_lastInstallmentReminderSentOn",
                table: "tbl_GraveReservations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "sys_nextInstallmentDueDate",
                table: "tbl_GraveReservations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sys_preferredSectionId",
                table: "tbl_GraveReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sys_reservationReference",
                table: "tbl_GraveReservations",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sys_status",
                table: "tbl_GraveReservations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sys_customerEmail",
                table: "tbl_Customers",
                type: "nvarchar(max)",
                nullable: true);

            // Preserve the section and price that each existing grave-based reservation had
            // when it was migrated to the standalone reservation workflow. A unique, readable
            // legacy reference is required before the unique index is added below.
            migrationBuilder.Sql(@"
                UPDATE reservation
                SET
                    sys_preferredSectionId = grave.sys_sectionId,
                    sys_agreedPrice = ISNULL(sectionRecord.sys_SecPrice, 0),
                    sys_reservationReference = CONCAT('LEGACY-', reservation.sys_reservationId),
                    sys_status = 'Active'
                FROM tbl_GraveReservations AS reservation
                LEFT JOIN tbl_Graves AS grave ON grave.sys_graveId = reservation.sys_graveId
                LEFT JOIN tbl_Sections AS sectionRecord ON sectionRecord.sys_sectionId = grave.sys_sectionId;");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_GraveReservations_sys_graveId",
                table: "tbl_GraveReservations",
                column: "sys_graveId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_GraveReservations_sys_preferredSectionId",
                table: "tbl_GraveReservations",
                column: "sys_preferredSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_GraveReservations_sys_reservationReference",
                table: "tbl_GraveReservations",
                column: "sys_reservationReference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_GraveReservations_sys_graveId",
                table: "tbl_GraveReservations");

            migrationBuilder.DropIndex(
                name: "IX_tbl_GraveReservations_sys_preferredSectionId",
                table: "tbl_GraveReservations");

            migrationBuilder.DropIndex(
                name: "IX_tbl_GraveReservations_sys_reservationReference",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_agreedPrice",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_expiryDate",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_lastInstallmentReminderSentOn",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_nextInstallmentDueDate",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_preferredSectionId",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_reservationReference",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_status",
                table: "tbl_GraveReservations");

            migrationBuilder.DropColumn(
                name: "sys_customerEmail",
                table: "tbl_Customers");

            migrationBuilder.AlterColumn<int>(
                name: "sys_graveId",
                table: "tbl_GraveReservations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
