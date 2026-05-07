using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class AddInstalledPlugins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstalledPlugins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PluginId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AssemblyPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastLoadStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotLoaded"),
                    LastLoadError = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    InstalledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstalledPlugins", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstalledPlugins_IsEnabled",
                table: "InstalledPlugins",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledPlugins_LastLoadStatus",
                table: "InstalledPlugins",
                column: "LastLoadStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledPlugins_PluginId",
                table: "InstalledPlugins",
                column: "PluginId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstalledPlugins");
        }
    }
}
