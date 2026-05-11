using Microsoft.EntityFrameworkCore.Migrations;
using Wock.Data;

#nullable disable

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class EnforceNonNegativePausedSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var totalPausedSecondsColumn = ActiveProvider == DatabaseOptionsExtensions.PostgresProviderName
                ? "\"TotalPausedSeconds\""
                : "TotalPausedSeconds";

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkEntries_TotalPausedSeconds_NonNegative",
                table: "WorkEntries",
                sql: $"{totalPausedSecondsColumn} >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_WorkEntries_TotalPausedSeconds_NonNegative",
                table: "WorkEntries");
        }
    }
}
