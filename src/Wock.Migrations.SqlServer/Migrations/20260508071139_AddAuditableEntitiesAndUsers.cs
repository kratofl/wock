using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Wock.Data;

#nullable disable

namespace Wock.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditableEntitiesAndUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "WorkEntries",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "WorkEntries",
                type: DateTimeType(),
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "WorkEntries",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "WorkEntries",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.Sql($"UPDATE {Identifier("WorkEntries")} SET {Identifier("ModifiedAt")} = {Identifier("UpdatedAt")}");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkEntries");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InstalledPlugins",
                type: DateTimeType(),
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "InstalledPlugins",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "InstalledPlugins",
                type: DateTimeType(),
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "InstalledPlugins",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.Sql(
                $"UPDATE {Identifier("InstalledPlugins")} SET {Identifier("CreatedAt")} = {Identifier("InstalledAt")}, {Identifier("ModifiedAt")} = {Identifier("UpdatedAt")}");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "InstalledPlugins");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Customers",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "Customers",
                type: DateTimeType(),
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "Customers",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Customers",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BookingTargets",
                type: DateTimeType(),
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql($"UPDATE {Identifier("BookingTargets")} SET {Identifier("CreatedAt")} = {UtcNowSql()}");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "BookingTargets",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "BookingTargets",
                type: DateTimeType(),
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "BookingTargets",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "BookingTargets",
                type: StringType(450),
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: StringType(450), maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: StringType(200), maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: StringType(200), maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: StringType(320), maxLength: 320, nullable: true),
                    IsActive = table.Column<bool>(type: BoolType(), nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: DateTimeType(), nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_CreatedByUserId",
                table: "WorkEntries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_ModifiedByUserId",
                table: "WorkEntries",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkEntries_OwnerUserId",
                table: "WorkEntries",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledPlugins_CreatedByUserId",
                table: "InstalledPlugins",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledPlugins_ModifiedByUserId",
                table: "InstalledPlugins",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedByUserId",
                table: "Customers",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ModifiedByUserId",
                table: "Customers",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OwnerUserId",
                table: "Customers",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTargets_CreatedByUserId",
                table: "BookingTargets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTargets_ModifiedByUserId",
                table: "BookingTargets",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTargets_OwnerUserId",
                table: "BookingTargets",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingTargets_Users_CreatedByUserId",
                table: "BookingTargets",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingTargets_Users_ModifiedByUserId",
                table: "BookingTargets",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingTargets_Users_OwnerUserId",
                table: "BookingTargets",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_CreatedByUserId",
                table: "Customers",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_ModifiedByUserId",
                table: "Customers",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_OwnerUserId",
                table: "Customers",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InstalledPlugins_Users_CreatedByUserId",
                table: "InstalledPlugins",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InstalledPlugins_Users_ModifiedByUserId",
                table: "InstalledPlugins",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_Users_CreatedByUserId",
                table: "WorkEntries",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_Users_ModifiedByUserId",
                table: "WorkEntries",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkEntries_Users_OwnerUserId",
                table: "WorkEntries",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingTargets_Users_CreatedByUserId",
                table: "BookingTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingTargets_Users_ModifiedByUserId",
                table: "BookingTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingTargets_Users_OwnerUserId",
                table: "BookingTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_CreatedByUserId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_ModifiedByUserId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_OwnerUserId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_InstalledPlugins_Users_CreatedByUserId",
                table: "InstalledPlugins");

            migrationBuilder.DropForeignKey(
                name: "FK_InstalledPlugins_Users_ModifiedByUserId",
                table: "InstalledPlugins");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_Users_CreatedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_Users_ModifiedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkEntries_Users_OwnerUserId",
                table: "WorkEntries");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_CreatedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_ModifiedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorkEntries_OwnerUserId",
                table: "WorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_InstalledPlugins_CreatedByUserId",
                table: "InstalledPlugins");

            migrationBuilder.DropIndex(
                name: "IX_InstalledPlugins_ModifiedByUserId",
                table: "InstalledPlugins");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CreatedByUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_ModifiedByUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_OwnerUserId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_BookingTargets_CreatedByUserId",
                table: "BookingTargets");

            migrationBuilder.DropIndex(
                name: "IX_BookingTargets_ModifiedByUserId",
                table: "BookingTargets");

            migrationBuilder.DropIndex(
                name: "IX_BookingTargets_OwnerUserId",
                table: "BookingTargets");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkEntries",
                type: DateTimeType(),
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                $"UPDATE {Identifier("WorkEntries")} SET {Identifier("UpdatedAt")} = COALESCE({Identifier("ModifiedAt")}, {Identifier("CreatedAt")})");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "WorkEntries");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "InstalledPlugins");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "InstalledPlugins",
                type: DateTimeType(),
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                $"UPDATE {Identifier("InstalledPlugins")} SET {Identifier("UpdatedAt")} = COALESCE({Identifier("ModifiedAt")}, {Identifier("CreatedAt")}, {Identifier("InstalledAt")})");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InstalledPlugins");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "InstalledPlugins");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "InstalledPlugins");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BookingTargets");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "BookingTargets");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "BookingTargets");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "BookingTargets");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "BookingTargets");
        }

        private string Identifier(string name)
        {
            return ActiveProvider == DatabaseOptionsExtensions.PostgresProviderName
                ? $"\"{name}\""
                : name;
        }

        private string UtcNowSql()
        {
            return ActiveProvider switch
            {
                DatabaseOptionsExtensions.SqlServerProviderName => "SYSUTCDATETIME()",
                DatabaseOptionsExtensions.PostgresProviderName => "NOW()",
                _ => "CURRENT_TIMESTAMP"
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
