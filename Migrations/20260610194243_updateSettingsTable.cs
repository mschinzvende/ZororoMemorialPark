using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class updateSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dayEndTime",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "dayStartTime",
                table: "SystemSettings",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dayEndTime",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "dayStartTime",
                table: "SystemSettings");
        }
    }
}
