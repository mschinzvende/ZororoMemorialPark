using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingNotificationRecipients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_BookingNotificationRecipients",
                columns: table => new
                {
                    sys_bookingNotificationRecipientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_tbl_BookingNotificationRecipients",
                        x => x.sys_bookingNotificationRecipientId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_BookingNotificationRecipients_UserId",
                table: "tbl_BookingNotificationRecipients",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_BookingNotificationRecipients");
        }
    }
}
