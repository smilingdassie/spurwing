using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PosItemVerificationWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantEventExecution2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantEventExecutions",
                columns: table => new
                {
                    ExecutionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantKey = table.Column<int>(type: "int", nullable: false),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CompletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantEventExecutions", x => x.ExecutionID);
                    table.ForeignKey(
                        name: "FK_RestaurantEventExecutions_RestaurantEvents_EventID",
                        column: x => x.EventID,
                        principalTable: "RestaurantEvents",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantEventExecutions_Restaurants_RestaurantKey",
                        column: x => x.RestaurantKey,
                        principalTable: "Restaurants",
                        principalColumn: "RestaurantKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEventExecutions_EventID",
                table: "RestaurantEventExecutions",
                column: "EventID");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantEventExecutions_RestaurantKey",
                table: "RestaurantEventExecutions",
                column: "RestaurantKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantEventExecutions");
        }
    }
}
