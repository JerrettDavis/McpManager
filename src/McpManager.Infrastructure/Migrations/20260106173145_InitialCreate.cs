using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CachedRegistryServers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RegistryName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ServerId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    InstallCommand = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: false),
                    DownloadCount = table.Column<long>(type: "INTEGER", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    LastUpdatedInRegistry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedRegistryServers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstalledServers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    InstallCommand = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TagsJson = table.Column<string>(type: "TEXT", nullable: false),
                    InstalledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: false),
                    RegistrySource = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InstallLocation = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstalledServers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistryMetadata",
                columns: table => new
                {
                    RegistryName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LastRefreshAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NextRefreshAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalServersCached = table.Column<int>(type: "INTEGER", nullable: false),
                    LastRefreshSuccessful = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRefreshError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RefreshIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistryMetadata", x => x.RegistryName);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CachedRegistryServers_FetchedAt",
                table: "CachedRegistryServers",
                column: "FetchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CachedRegistryServers_Name",
                table: "CachedRegistryServers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CachedRegistryServers_RegistryName_ServerId",
                table: "CachedRegistryServers",
                columns: new[] { "RegistryName", "ServerId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstalledServers_InstalledAt",
                table: "InstalledServers",
                column: "InstalledAt");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledServers_Name",
                table: "InstalledServers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RegistryMetadata_NextRefreshAt",
                table: "RegistryMetadata",
                column: "NextRefreshAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CachedRegistryServers");

            migrationBuilder.DropTable(
                name: "InstalledServers");

            migrationBuilder.DropTable(
                name: "RegistryMetadata");
        }
    }
}
