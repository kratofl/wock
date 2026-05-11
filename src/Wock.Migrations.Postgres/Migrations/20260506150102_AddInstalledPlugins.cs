using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Wock.Data;

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
                    Id = table.Column<int>(type: IntType(), nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PluginId = table.Column<string>(type: StringType(200), maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: StringType(200), maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: StringType(100), maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: StringType(2000), maxLength: 2000, nullable: true),
                    AssemblyPath = table.Column<string>(type: StringType(1000), maxLength: 1000, nullable: false),
                    TypeName = table.Column<string>(type: StringType(500), maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: BoolType(), nullable: false, defaultValue: false),
                    LastLoadStatus = table.Column<string>(type: StringType(20), maxLength: 20, nullable: false, defaultValue: "NotLoaded"),
                    LastLoadError = table.Column<string>(type: StringType(4000), maxLength: 4000, nullable: true),
                    InstalledAt = table.Column<DateTime>(type: DateTimeType(), nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: DateTimeType(), nullable: false)
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

        private string IntType()
        {
            return ActiveProvider switch
            {
                DatabaseOptionsExtensions.SqlServerProviderName => "int",
                DatabaseOptionsExtensions.PostgresProviderName => "integer",
                _ => "INTEGER"
            };
        }

        private string BoolType()
        {
            return ActiveProvider switch
            {
                DatabaseOptionsExtensions.SqlServerProviderName => "bit",
                DatabaseOptionsExtensions.PostgresProviderName => "boolean",
                _ => "INTEGER"
            };
        }

        private string DateTimeType()
        {
            return ActiveProvider switch
            {
                DatabaseOptionsExtensions.SqlServerProviderName => "datetime2",
                DatabaseOptionsExtensions.PostgresProviderName => "timestamp with time zone",
                _ => "TEXT"
            };
        }

        private string StringType(int maxLength)
        {
            return ActiveProvider switch
            {
                DatabaseOptionsExtensions.SqlServerProviderName => $"nvarchar({maxLength})",
                DatabaseOptionsExtensions.PostgresProviderName => $"character varying({maxLength})",
                _ => "TEXT"
            };
        }
    }
}
