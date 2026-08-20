using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZororoMemorialPark.Data;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260820143000_AddExpenseCategories")]
    public partial class AddExpenseCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ExpenseCategories",
                columns: table => new
                {
                    sys_expenseCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    sys_expenseCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sys_displayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ExpenseCategories", x => x.sys_expenseCategoryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ExpenseCategories_sys_expenseCategoryName",
                table: "tbl_ExpenseCategories",
                column: "sys_expenseCategoryName",
                unique: true);

            migrationBuilder.InsertData(
                table: "tbl_ExpenseCategories",
                columns: new[] { "sys_expenseCategoryId", "sys_displayOrder", "sys_expenseCategoryName" },
                columnTypes: new[] { "int", "int", "nvarchar(100)" },
                values: new object[,]
                {
                    { 1, 10, "Ground maintenance" },
                    { 2, 20, "Equipment and fuel" },
                    { 3, 30, "Utilities" },
                    { 4, 40, "Office and administration" },
                    { 5, 50, "Security" },
                    { 6, 60, "Staff welfare" },
                    { 7, 70, "Marketing" },
                    { 8, 80, "Other" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_ExpenseCategories");
        }
    }
}
