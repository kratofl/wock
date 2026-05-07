using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveWorkEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveSlot",
                table: "WorkEntries",
                type: "INTEGER",
                nullable: true,
                computedColumnSql: "CASE WHEN Status IN ('Running', 'Paused') THEN 1 ELSE NULL END");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_OneActiveEntry",
                table: "WorkEntries",
                column: "ActiveSlot",
                unique: true,
                filter: "ActiveSlot IS NOT NULL");
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
    }
}
