using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndPasswordFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastAction",
                table: "Volunteers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAt",
                table: "Volunteers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Volunteers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Volunteers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Volunteers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastAction",
                table: "Organizations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Organizations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "DisabilityUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "LastAction",
                table: "DisabilityUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAt",
                table: "DisabilityUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "DisabilityUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "DisabilityUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DisabilityUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAt",
                table: "Admins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Admins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TargetUserId = table.Column<int>(type: "integer", nullable: true),
                    TargetUserName = table.Column<string>(type: "text", nullable: true),
                    TargetUserType = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActivityLogs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastPasswordChangedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { null, "$2a$11$5Y8qZ5Z5Z5Z5Z5Z5Z5Z5ZuY8qZ5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminActivityLogs");

            migrationBuilder.DropColumn(
                name: "LastAction",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAt",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "LastAction",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastAction",
                table: "DisabilityUsers");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAt",
                table: "DisabilityUsers");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "DisabilityUsers");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "DisabilityUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DisabilityUsers");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAt",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Admins");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "DisabilityUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "Admin@123");
        }
    }
}
