using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsBillingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_OneActiveEntry",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ActiveSlot",
                table: "WorkEntries");

            migrationBuilder.AddColumn<int>(
                name: "ActivityCategoryId",
                table: "WorkEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "WorkEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "WorkEntries",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCategory",
                table: "WorkEntries",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "WorkEntries",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBillable",
                table: "WorkEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "WorkEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectTaskId",
                table: "WorkEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "WorkEntries",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "WorkEntries",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "Customers",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Customers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultHourlyRate",
                table: "Customers",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Customers",
                type: "TEXT",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Customers",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActiveOwnerSlot",
                table: "WorkEntries",
                type: "TEXT",
                maxLength: 450,
                nullable: true,
                computedColumnSql: "CASE WHEN Status IN ('Running', 'Paused') THEN COALESCE(OwnerUserId, '') ELSE NULL END");

            migrationBuilder.CreateTable(
                name: "ActivityCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndsOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    BudgetHours = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    BillingModel = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DefaultHourlyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ActivityCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    AssignedUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_ActivityCategories_ActivityCategoryId",
                        column: x => x.ActivityCategoryId,
                        principalTable: "ActivityCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTasks_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ActivityCategories",
                columns: new[] { "Id", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, "Beratung", 10 },
                    { 2, true, "Entwicklung", 20 },
                    { 3, true, "Design", 30 },
                    { 4, true, "Projektmanagement", 40 },
                    { 5, true, "Support", 50 },
                    { 6, true, "Testing", 60 },
                    { 7, true, "Dokumentation", 70 },
                    { 8, true, "Administration", 80 },
                    { 9, true, "Vertrieb", 90 },
                    { 10, true, "Meeting", 100 },
                    { 11, true, "Sonstiges", 110 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ActivityCategoryId",
                table: "WorkEntries",
                column: "ActivityCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ApprovedByUserId",
                table: "WorkEntries",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_OneActiveEntry",
                table: "WorkEntries",
                column: "ActiveOwnerSlot",
                unique: true,
                filter: "ActiveOwnerSlot IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_OwnerUserId_ReviewStatus",
                table: "WorkEntries",
                columns: new[] { "OwnerUserId", "ReviewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ProjectId",
                table: "WorkEntries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ProjectId_StartedAt",
                table: "WorkEntries",
                columns: new[] { "ProjectId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ProjectTaskId",
                table: "WorkEntries",
                column: "ProjectTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ReviewStatus",
                table: "WorkEntries",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityCategories_IsActive",
                table: "ActivityCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityCategories_Name",
                table: "ActivityCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityCategories_SortOrder",
                table: "ActivityCategories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedByUserId",
                table: "Projects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CustomerId",
                table: "Projects",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CustomerId_Name",
                table: "Projects",
                columns: new[] { "CustomerId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ModifiedByUserId",
                table: "Projects",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ActivityCategoryId",
                table: "ProjectTasks",
                column: "ActivityCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_AssignedUserId",
                table: "ProjectTasks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_CreatedByUserId",
                table: "ProjectTasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ModifiedByUserId",
                table: "ProjectTasks",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_OwnerUserId",
                table: "ProjectTasks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId",
                table: "ProjectTasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_Title",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_Status",
                table: "ProjectTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_Role",
                table: "UserRoles",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_Role",
                table: "UserRoles",
                columns: new[] { "UserId", "Role" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_ActivityCategories_ActivityCategoryId",
                table: "WorkEntries",
                column: "ActivityCategoryId",
                principalTable: "ActivityCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_ProjectTasks_ProjectTaskId",
                table: "WorkEntries",
                column: "ProjectTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_Projects_ProjectId",
                table: "WorkEntries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_Users_ApprovedByUserId",
                table: "WorkEntries",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_ActivityCategories_ActivityCategoryId",
                table: "WorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_ProjectTasks_ProjectTaskId",
                table: "WorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_Projects_ProjectId",
                table: "WorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_Users_ApprovedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropTable(
                name: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "ActivityCategories");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_ActivityCategoryId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_ApprovedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_OneActiveEntry",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_OwnerUserId_ReviewStatus",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_ProjectId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_ProjectId_StartedAt",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_ProjectTaskId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_ReviewStatus",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ActiveOwnerSlot",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ActivityCategoryId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "BillingCategory",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "IsBillable",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ProjectTaskId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DefaultHourlyRate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Customers");

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
    }
}
