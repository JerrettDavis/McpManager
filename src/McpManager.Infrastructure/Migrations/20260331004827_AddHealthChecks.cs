using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthChecks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ServerId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsHealthy = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthChecks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_CheckedAt",
                table: "HealthChecks",
                column: "CheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_ServerId",
                table: "HealthChecks",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_ServerId_CheckedAt",
                table: "HealthChecks",
                columns: new[] { "ServerId", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthChecks");
        }
    }
}
