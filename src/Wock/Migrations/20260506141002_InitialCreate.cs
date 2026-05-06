using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BookingSoftware = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BookingTicketId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingTargets_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookingTargetId = table.Column<int>(type: "INTEGER", nullable: true),
                    ExternalTicketId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StoppedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalPausedSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkEntries_BookingTargets_BookingTargetId",
                        column: x => x.BookingTargetId,
                        principalTable: "BookingTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkEntryPauses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkEntryId = table.Column<int>(type: "INTEGER", nullable: false),
                    PausedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResumedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkEntryPauses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkEntryPauses_WorkEntries_WorkEntryId",
                        column: x => x.WorkEntryId,
                        principalTable: "WorkEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingTargets_CustomerId",
                table: "BookingTargets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTargets_IsActive",
                table: "BookingTargets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsActive",
                table: "Customers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_BookingTargetId",
                table: "WorkEntries",
                column: "BookingTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_CustomerId",
                table: "WorkEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_CustomerId_StartedAt",
                table: "WorkEntries",
                columns: new[] { "CustomerId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ExternalTicketId",
                table: "WorkEntries",
                column: "ExternalTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_StartedAt",
                table: "WorkEntries",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_Status",
                table: "WorkEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_Status_StartedAt",
                table: "WorkEntries",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntryPauses_PausedAt",
                table: "WorkEntryPauses",
                column: "PausedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntryPauses_WorkEntryId",
                table: "WorkEntryPauses",
                column: "WorkEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkEntryPauses");

            migrationBuilder.DropTable(
                name: "WorkEntries");

            migrationBuilder.DropTable(
                name: "BookingTargets");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
