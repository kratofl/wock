using Microsoft.EntityFrameworkCore.Migrations;
using Wock.Data;

#nullable disable

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveWorkEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var isPostgres = ActiveProvider == DatabaseOptionsExtensions.PostgresProviderName;
            var statusColumn = isPostgres ? "\"Status\"" : "Status";
            var activeSlotColumn = isPostgres ? "\"ActiveSlot\"" : "ActiveSlot";

            migrationBuilder.AddColumn<int>(
                name: "ActiveSlot",
                table: "WorkEntries",
                type: IntType(),
                nullable: true,
                computedColumnSql: $"CASE WHEN {statusColumn} IN ('Running', 'Paused') THEN 1 ELSE NULL END",
                stored: isPostgres ? true : null);

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_OneActiveEntry",
                table: "WorkEntries",
                column: "ActiveSlot",
                unique: true,
                filter: $"{activeSlotColumn} IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_OneActiveEntry",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ActiveSlot",
                table: "WorkEntries");
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
    }
}
