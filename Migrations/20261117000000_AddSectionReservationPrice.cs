using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZororoMemorialPark.Data;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261117000000_AddSectionReservationPrice")]
    public partial class AddSectionReservationPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "sys_ReservationPrice",
                table: "tbl_Sections",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql("UPDATE tbl_Sections SET sys_ReservationPrice = sys_SecPrice");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_ReservationPrice",
                table: "tbl_Sections");
        }
    }
}
