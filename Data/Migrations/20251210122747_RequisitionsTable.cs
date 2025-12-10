using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequisitionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_Requisitions",
                columns: table => new
                {
                    sys_requisitionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_requestedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_requestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sys_title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    sys_description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    sys_rejectReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sys_attachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sys_approver1UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sys_approver2UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sys_currentApproverOrder = table.Column<int>(type: "int", nullable: true),
                    sys_approver1CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sys_approver2CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sys_paymentReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sys_paymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    sys_paidByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Requisitions", x => x.sys_requisitionId);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionItems",
                columns: table => new
                {
                    sys_itemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_requisitionId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CostPerUnit = table.Column<double>(type: "float", nullable: false),
                    TotalCost = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedAmount = table.Column<double>(type: "float", nullable: true),
                    Approver1Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByApprover1 = table.Column<bool>(type: "bit", nullable: false),
                    Approver2Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByApprover2 = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionItems", x => x.sys_itemId);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionItems_tbl_Requisitions_sys_requisitionId",
                        column: x => x.sys_requisitionId,
                        principalTable: "tbl_Requisitions",
                        principalColumn: "sys_requisitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionItems_sys_requisitionId",
                table: "tbl_RequisitionItems",
                column: "sys_requisitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_RequisitionItems");

            migrationBuilder.DropTable(
                name: "tbl_Requisitions");
        }
    }
}
