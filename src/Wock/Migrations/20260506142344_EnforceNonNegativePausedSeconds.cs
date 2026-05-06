using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class EnforceNonNegativePausedSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_WorkEntries_TotalPausedSeconds_NonNegative",
                table: "WorkEntries",
                sql: "TotalPausedSeconds >= 0");
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
