
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class allocationBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sys_bookingId",
                table: "tbl_GraveAllocations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_bookingId",
                table: "tbl_GraveAllocations");
        }
    }
}
