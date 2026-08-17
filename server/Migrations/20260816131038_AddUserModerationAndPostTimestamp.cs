using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserModerationAndPostTimestamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Posts",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            // Backfill existing rows from the day-only CreatedDate they already have, instead
            // of leaving them all stamped with today's date via the column default above.
            migrationBuilder.Sql("UPDATE `Posts` SET `CreatedAt` = `CreatedDate`;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeactivated",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Posts");
        }
    }
}
