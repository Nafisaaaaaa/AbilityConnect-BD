using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_20260921135643 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisabilityUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    DisabilityType = table.Column<string>(type: "text", nullable: false),
                    OtherDisabilityType = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: true),
                    Education = table.Column<string>(type: "text", nullable: true),
                    Interests = table.Column<string>(type: "text", nullable: true),
                    ProfilePicture = table.Column<string>(type: "text", nullable: true),
                    DisabilityCertificate = table.Column<string>(type: "text", nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAction = table.Column<string>(type: "text", nullable: true),
                    LastPasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResetToken = table.Column<string>(type: "text", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisabilityUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrganizationType = table.Column<string>(type: "text", nullable: false),
                    OtherOrganizationType = table.Column<string>(type: "text", nullable: true),
                    OrganizationDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Division = table.Column<string>(type: "text", nullable: false),
                    Website = table.Column<string>(type: "text", nullable: true),
                    FacebookProfileLink = table.Column<string>(type: "text", nullable: true),
                    ContactPersonName = table.Column<string>(type: "text", nullable: false),
                    ContactPersonDesignation = table.Column<string>(type: "text", nullable: false),
                    ContactPersonPhone = table.Column<string>(type: "text", nullable: false),
                    ContactPersonEmail = table.Column<string>(type: "text", nullable: false),
                    Logo = table.Column<string>(type: "text", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAction = table.Column<string>(type: "text", nullable: true),
                    LastPasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResetToken = table.Column<string>(type: "text", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Volunteers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    Availability = table.Column<string>(type: "text", nullable: true),
                    Experience = table.Column<string>(type: "text", nullable: true),
                    LanguagesSpoken = table.Column<string>(type: "text", nullable: true),
                    ProfilePicture = table.Column<string>(type: "text", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAction = table.Column<string>(type: "text", nullable: true),
                    LastPasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResetToken = table.Column<string>(type: "text", nullable: true),
                    ResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volunteers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "LastPasswordChangedAt", "PasswordHash", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@abilityconnect.com", "Administrator", null, "$2a$11$5Y8qZ5Z5Z5Z5Z5Z5Z5Z5ZuY8qZ5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5Z5", null });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Email",
                table: "Admins",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisabilityUsers_Email",
                table: "DisabilityUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Email",
                table: "Organizations",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Volunteers_Email",
                table: "Volunteers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminActivityLogs");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "DisabilityUsers");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Volunteers");
        }
    }
}
