using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class TemporaryBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "sys_isTemporary",
                table: "tbl_BurialBookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "sys_suspense_amount",
                table: "tbl_BurialBookings",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_isTemporary",
                table: "tbl_BurialBookings");

            migrationBuilder.DropColumn(
                name: "sys_suspense_amount",
                table: "tbl_BurialBookings");
        }
    }
}
