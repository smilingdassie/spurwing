using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosItemVerificationWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantEventTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentID);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantEvents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EventDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActionDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StatusName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantEvents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    RestaurantKey = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DCLink = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RestaurantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetOpeningDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualOpeningDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.RestaurantKey);
                });

            migrationBuilder.CreateTable(
                name: "Systems",
                columns: table => new
                {
                    SystemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Systems", x => x.SystemID);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    TeamID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartmentID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamID);
                    table.ForeignKey(
                        name: "FK_Teams_Departments_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Departments",
                        principalColumn: "DepartmentID");
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOpeningProjects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantKey = table.Column<int>(type: "int", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProjectEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantOpeningProjects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_RestaurantOpeningProjects_Restaurants_RestaurantKey",
                        column: x => x.RestaurantKey,
                        principalTable: "Restaurants",
                        principalColumn: "RestaurantKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOpeningTasks",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentID = table.Column<int>(type: "int", nullable: false),
                    SystemID = table.Column<int>(type: "int", nullable: true),
                    ResponsibleTeamID = table.Column<int>(type: "int", nullable: true),
                    DependsOnTaskID = table.Column<int>(type: "int", nullable: true),
                    StatusOptions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstimatedDurationDays = table.Column<int>(type: "int", nullable: true),
                    IsCriticalPath = table.Column<bool>(type: "bit", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantOpeningTasks", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK_RestaurantOpeningTasks_Departments_DepartmentID",
                        column: x => x.DepartmentID,
                        principalTable: "Departments",
                        principalColumn: "DepartmentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantOpeningTasks_RestaurantOpeningTasks_DependsOnTaskID",
                        column: x => x.DependsOnTaskID,
                        principalTable: "RestaurantOpeningTasks",
                        principalColumn: "TaskId");
                    table.ForeignKey(
                        name: "FK_RestaurantOpeningTasks_Systems_SystemID",
                        column: x => x.SystemID,
                        principalTable: "Systems",
                        principalColumn: "SystemID");
                    table.ForeignKey(
                        name: "FK_RestaurantOpeningTasks_Teams_ResponsibleTeamID",
                        column: x => x.ResponsibleTeamID,
                        principalTable: "Teams",
                        principalColumn: "TeamID");
                });

            migrationBuilder.CreateTable(
                name: "TaskExecutions",
                columns: table => new
                {
                    ExecutionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PercentComplete = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExecutions", x => x.ExecutionId);
                    table.ForeignKey(
                        name: "FK_TaskExecutions_RestaurantOpeningProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "RestaurantOpeningProjects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskExecutions_RestaurantOpeningTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "RestaurantOpeningTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOpeningProjects_RestaurantKey",
                table: "RestaurantOpeningProjects",
                column: "RestaurantKey");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOpeningTasks_DepartmentID",
                table: "RestaurantOpeningTasks",
                column: "DepartmentID");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOpeningTasks_DependsOnTaskID",
                table: "RestaurantOpeningTasks",
                column: "DependsOnTaskID");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOpeningTasks_ResponsibleTeamID",
                table: "RestaurantOpeningTasks",
                column: "ResponsibleTeamID");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOpeningTasks_SystemID",
                table: "RestaurantOpeningTasks",
                column: "SystemID");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_DCLink",
                table: "Restaurants",
                column: "DCLink",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_ProjectId",
                table: "TaskExecutions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutions_TaskId",
                table: "TaskExecutions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DepartmentID",
                table: "Teams",
                column: "DepartmentID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantEvents");

            migrationBuilder.DropTable(
                name: "TaskExecutions");

            migrationBuilder.DropTable(
                name: "RestaurantOpeningProjects");

            migrationBuilder.DropTable(
                name: "RestaurantOpeningTasks");

            migrationBuilder.DropTable(
                name: "Restaurants");

            migrationBuilder.DropTable(
                name: "Systems");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
