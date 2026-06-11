using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class updateReceiptBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_ReceiptBooks_tbl_Agents_sys_agentId",
                table: "tbl_ReceiptBooks");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ReceiptBooks_sys_agentId",
                table: "tbl_ReceiptBooks");

            migrationBuilder.DropColumn(
                name: "sys_agentId",
                table: "tbl_ReceiptBooks");

            migrationBuilder.AddColumn<string>(
                name: "issuedTo",
                table: "tbl_ReceiptBooks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "issuedTo",
                table: "tbl_ReceiptBooks");

            migrationBuilder.AddColumn<int>(
                name: "sys_agentId",
                table: "tbl_ReceiptBooks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptBooks_sys_agentId",
                table: "tbl_ReceiptBooks",
                column: "sys_agentId");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_ReceiptBooks_tbl_Agents_sys_agentId",
                table: "tbl_ReceiptBooks",
                column: "sys_agentId",
                principalTable: "tbl_Agents",
                principalColumn: "sys_agentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
