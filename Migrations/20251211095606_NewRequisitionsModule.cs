using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZororoMemorialPark.Migrations
{
    /// <inheritdoc />
    public partial class NewRequisitionsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            

           

           

           
            migrationBuilder.CreateTable(
                name: "tbl_RequisitionWorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequesterRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionWorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionWorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowDefinitionId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepType = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionWorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionWorkflowSteps_tbl_RequisitionWorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "tbl_RequisitionWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

      
            migrationBuilder.CreateTable(
                name: "tbl_Requisitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequesterUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    WorkflowDefinitionId = table.Column<int>(type: "int", nullable: false),
                    CurrentStepId = table.Column<int>(type: "int", nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Requisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_Requisitions_tbl_RequisitionWorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalTable: "tbl_RequisitionWorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_Requisitions_tbl_RequisitionWorkflowSteps_CurrentStepId",
                        column: x => x.CurrentStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id");
                });

          
            migrationBuilder.CreateTable(
                name: "tbl_RequisitionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionHistories_tbl_RequisitionWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionHistories_tbl_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "tbl_Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OriginalQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OriginalUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LatestComment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLines_tbl_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "tbl_Requisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionLineChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionLineId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewUnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionLineChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineChangeLogs_tbl_RequisitionLines_RequisitionLineId",
                        column: x => x.RequisitionLineId,
                        principalTable: "tbl_RequisitionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineChangeLogs_tbl_RequisitionWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RequisitionLineComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequisitionLineId = table.Column<int>(type: "int", nullable: false),
                    WorkflowStepId = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RequisitionLineComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineComments_tbl_RequisitionLines_RequisitionLineId",
                        column: x => x.RequisitionLineId,
                        principalTable: "tbl_RequisitionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tbl_RequisitionLineComments_tbl_RequisitionWorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "tbl_RequisitionWorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionHistories_RequisitionId",
                table: "tbl_RequisitionHistories",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionHistories_WorkflowStepId",
                table: "tbl_RequisitionHistories",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineChangeLogs_RequisitionLineId",
                table: "tbl_RequisitionLineChangeLogs",
                column: "RequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineChangeLogs_WorkflowStepId",
                table: "tbl_RequisitionLineChangeLogs",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineComments_RequisitionLineId",
                table: "tbl_RequisitionLineComments",
                column: "RequisitionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLineComments_WorkflowStepId",
                table: "tbl_RequisitionLineComments",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionLines_RequisitionId",
                table: "tbl_RequisitionLines",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Requisitions_CurrentStepId",
                table: "tbl_Requisitions",
                column: "CurrentStepId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Requisitions_WorkflowDefinitionId",
                table: "tbl_Requisitions",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RequisitionWorkflowSteps_WorkflowDefinitionId",
                table: "tbl_RequisitionWorkflowSteps",
                column: "WorkflowDefinitionId");
        }

        /// <inheritdoc />
       
    }
}
