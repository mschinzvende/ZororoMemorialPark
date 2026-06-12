using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class update67 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_deceasedDOB",
                table: "tbl_Deceased");

            migrationBuilder.DropColumn(
                name: "sys_deceasedMaritalStatus",
                table: "tbl_Deceased");

            migrationBuilder.DropColumn(
                name: "sys_deceasedNameOfSpouse",
                table: "tbl_Deceased");

            migrationBuilder.DropColumn(
                name: "sys_deceasedNextOfKinEmail",
                table: "tbl_Deceased");

            migrationBuilder.DropColumn(
                name: "sys_commission",
                table: "tbl_Agents");

            migrationBuilder.DropColumn(
                name: "sys_isPercentage",
                table: "tbl_Agents");

            migrationBuilder.AddColumn<string>(
                name: "sys_burialOrderNo",
                table: "tbl_Deceased",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sys_depth",
                table: "tbl_Deceased",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sys_minister",
                table: "tbl_Deceased",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sys_undertaker",
                table: "tbl_Deceased",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_burialOrderNo",
                table: "tbl_Deceased");

            migrationBuilder.DropColumn(
                name: "sys_depth",
                table: "tbl_Deceased");

            migrationBuilder.DropColumn(
                name: "sys_minister",
                table: "tbl_Deceased");

            migrationBuilder.DropColumn(
                name: "sys_undertaker",
                table: "tbl_Deceased");

            migrationBuilder.AddColumn<DateOnly>(
                name: "sys_deceasedDOB",
                table: "tbl_Deceased",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "sys_deceasedMaritalStatus",
                table: "tbl_Deceased",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sys_deceasedNameOfSpouse",
                table: "tbl_Deceased",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "sys_deceasedNextOfKinEmail",
                table: "tbl_Deceased",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "sys_commission",
                table: "tbl_Agents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "sys_isPercentage",
                table: "tbl_Agents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
