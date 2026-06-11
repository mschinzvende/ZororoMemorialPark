using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class updateSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sys_maxConcurrentBurialsPerSlot",
                table: "tbl_Sections",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sys_maxConcurrentBurialsPerSlot",
                table: "tbl_Sections");
        }
    }
}
