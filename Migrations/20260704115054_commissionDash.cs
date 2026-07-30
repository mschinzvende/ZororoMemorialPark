using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class commissionDash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tbl_CommissionsDue_sys_agentId",
                table: "tbl_CommissionsDue",
                column: "sys_agentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_CommissionPayments_sys_agentId",
                table: "tbl_CommissionPayments",
                column: "sys_agentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_CommissionPaymentAllocations_sys_commissionId",
                table: "tbl_CommissionPaymentAllocations",
                column: "sys_commissionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_CommissionPaymentAllocations_sys_commissionPaymentId",
                table: "tbl_CommissionPaymentAllocations",
                column: "sys_commissionPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tbl_CommissionsDue_sys_agentId",
                table: "tbl_CommissionsDue");

            migrationBuilder.DropIndex(
                name: "IX_tbl_CommissionPayments_sys_agentId",
                table: "tbl_CommissionPayments");

            migrationBuilder.DropIndex(
                name: "IX_tbl_CommissionPaymentAllocations_sys_commissionId",
                table: "tbl_CommissionPaymentAllocations");

            migrationBuilder.DropIndex(
                name: "IX_tbl_CommissionPaymentAllocations_sys_commissionPaymentId",
                table: "tbl_CommissionPaymentAllocations");
        }
    }
}
