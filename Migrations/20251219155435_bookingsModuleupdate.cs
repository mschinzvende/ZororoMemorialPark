using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class bookingsModuleupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.AlterColumn<string>(
                name: "sys_deceasedLastName",
                table: "tbl_BurialBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "sys_deceasedFirstName",
                table: "tbl_BurialBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "sys_bookingStatus",
                table: "tbl_BurialBookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<int>(
                name: "sys_sectionId",
                table: "tbl_BurialBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_SecPrice",
                table: "tbl_Sections");

            migrationBuilder.DropColumn(
                name: "sys_sectionId",
                table: "tbl_BurialBookings");

            migrationBuilder.AddColumn<double>(
                name: "sys_price",
                table: "tbl_Graves",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "sys_deceasedLastName",
                table: "tbl_BurialBookings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "sys_deceasedFirstName",
                table: "tbl_BurialBookings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "sys_bookingStatus",
                table: "tbl_BurialBookings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
