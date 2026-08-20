using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class LinkExpensesToApprovedRequisitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sys_requisitionId",
                table: "tbl_Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sys_requisitionLineId",
                table: "tbl_Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Expenses_sys_requisitionId",
                table: "tbl_Expenses",
                column: "sys_requisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Expenses_sys_requisitionLineId",
                table: "tbl_Expenses",
                column: "sys_requisitionLineId",
                unique: true,
                filter: "[sys_requisitionLineId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_Expenses_tbl_RequisitionLines_sys_requisitionLineId",
                table: "tbl_Expenses",
                column: "sys_requisitionLineId",
                principalTable: "tbl_RequisitionLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_tbl_Expenses_tbl_Requisitions_sys_requisitionId",
                table: "tbl_Expenses",
                column: "sys_requisitionId",
                principalTable: "tbl_Requisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tbl_Expenses_tbl_RequisitionLines_sys_requisitionLineId",
                table: "tbl_Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_tbl_Expenses_tbl_Requisitions_sys_requisitionId",
                table: "tbl_Expenses");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Expenses_sys_requisitionId",
                table: "tbl_Expenses");

            migrationBuilder.DropIndex(
                name: "IX_tbl_Expenses_sys_requisitionLineId",
                table: "tbl_Expenses");

            migrationBuilder.DropColumn(
                name: "sys_requisitionId",
                table: "tbl_Expenses");

            migrationBuilder.DropColumn(
                name: "sys_requisitionLineId",
                table: "tbl_Expenses");
        }
    }
}
