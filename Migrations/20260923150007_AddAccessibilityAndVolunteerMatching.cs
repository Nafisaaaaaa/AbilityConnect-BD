using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessibilityAndVolunteerMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssistanceTypes",
                table: "Volunteers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Volunteers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Volunteers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Volunteers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Volunteers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Volunteers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Volunteers",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VolunteerStatus",
                table: "Volunteers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AccessibilityPlaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlaceName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PlaceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    WheelchairRamp = table.Column<bool>(type: "boolean", nullable: false),
                    Elevator = table.Column<bool>(type: "boolean", nullable: false),
                    AccessibleToilet = table.Column<bool>(type: "boolean", nullable: false),
                    AccessibleParking = table.Column<bool>(type: "boolean", nullable: false),
                    AccessibilityScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityPlaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerSupportRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AssistanceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Urgency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssignedVolunteerId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerSupportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerSupportRequests_DisabilityUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VolunteerSupportRequests_Volunteers_AssignedVolunteerId",
                        column: x => x.AssignedVolunteerId,
                        principalTable: "Volunteers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AccessibilityReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccessibilityPlaceId = table.Column<int>(type: "integer", nullable: false),
                    SubmittedByUserId = table.Column<int>(type: "integer", nullable: false),
                    IssueType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PhotoPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AdminNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessibilityReports_AccessibilityPlaces_AccessibilityPlace~",
                        column: x => x.AccessibilityPlaceId,
                        principalTable: "AccessibilityPlaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessibilityReports_DisabilityUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VolunteerSupportRequestId = table.Column<int>(type: "integer", nullable: false),
                    SenderUserId = table.Column<int>(type: "integer", nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SenderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MessageText = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VolunteerChatMessages_VolunteerSupportRequests_VolunteerSup~",
                        column: x => x.VolunteerSupportRequestId,
                        principalTable: "VolunteerSupportRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessibilityReportVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccessibilityReportId = table.Column<int>(type: "integer", nullable: false),
                    VerifiedByUserId = table.Column<int>(type: "integer", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comments = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityReportVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessibilityReportVerifications_AccessibilityReports_Acces~",
                        column: x => x.AccessibilityReportId,
                        principalTable: "AccessibilityReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessibilityReportVerifications_DisabilityUsers_VerifiedBy~",
                        column: x => x.VerifiedByUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccessibilityPlaces",
                columns: new[] { "Id", "AccessibilityScore", "AccessibleParking", "AccessibleToilet", "Address", "City", "CreatedAt", "Description", "District", "Elevator", "Latitude", "Longitude", "PlaceName", "PlaceType", "UpdatedAt", "WheelchairRamp" },
                values: new object[,]
                {
                    { 1, 95, true, true, "Secretariat Road, Ramna", "Dhaka", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Major government hospital with dedicated accessibility ramps at main OPD, modern elevators, and reserved wheelchair parking.", "Dhaka", true, 23.7258, 90.397599999999997, "Dhaka Medical College Hospital", "Hospital", null, true },
                    { 2, 80, false, true, "Shahbagh Avenue", "Dhaka", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cultural heritage museum featuring flat wide entry ramps, tactile walking floor indicators, and accessible restrooms on each floor.", "Dhaka", true, 23.738099999999999, 90.395200000000003, "National Museum of Bangladesh", "Public Park", null, true },
                    { 3, 95, true, true, "Panthapath, Kawran Bazar", "Dhaka", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "One of South Asia's largest malls with full wheelchair accessibility, wide elevators, accessible toilets on all levels, and underground accessible parking.", "Dhaka", true, 23.750800000000002, 90.391199999999998, "Bashundhara City Shopping Complex", "Shopping Mall", null, true },
                    { 4, 70, true, true, "Kamalapur, Motijheel", "Dhaka", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Central railway terminal with ground floor platform ramp access and accessible waiting area, but overhead bridge elevators can experience periodic maintenance.", "Dhaka", false, 23.7317, 90.425799999999995, "Kamalapur Central Railway Station", "Transit Station", null, true },
                    { 5, 90, true, true, "57 K.B. Fazlul Kader Road", "Chittagong", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Premier tertiary medical center in southeastern Bangladesh with emergency ramps, accessible diagnostic labs, and dedicated elevator banks.", "Chittagong", true, 22.359200000000001, 91.8215, "Chittagong Medical College Hospital", "Hospital", null, true },
                    { 6, 60, true, false, "Agrabad Commercial Area", "Chittagong", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Regional public administration complex with street level entrance, elevator access, and disability services counter on level 1.", "Chittagong", true, 22.3276, 91.812299999999993, "Agrabad Government Commercial Center", "Government Office", null, false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityReports_AccessibilityPlaceId",
                table: "AccessibilityReports",
                column: "AccessibilityPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityReports_SubmittedByUserId",
                table: "AccessibilityReports",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityReportVerifications_AccessibilityReportId_Veri~",
                table: "AccessibilityReportVerifications",
                columns: new[] { "AccessibilityReportId", "VerifiedByUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityReportVerifications_VerifiedByUserId",
                table: "AccessibilityReportVerifications",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerChatMessages_VolunteerSupportRequestId",
                table: "VolunteerChatMessages",
                column: "VolunteerSupportRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerSupportRequests_AssignedVolunteerId",
                table: "VolunteerSupportRequests",
                column: "AssignedVolunteerId");

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerSupportRequests_RequestedByUserId",
                table: "VolunteerSupportRequests",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessibilityReportVerifications");

            migrationBuilder.DropTable(
                name: "VolunteerChatMessages");

            migrationBuilder.DropTable(
                name: "AccessibilityReports");

            migrationBuilder.DropTable(
                name: "VolunteerSupportRequests");

            migrationBuilder.DropTable(
                name: "AccessibilityPlaces");

            migrationBuilder.DropColumn(
                name: "AssistanceTypes",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Volunteers");

            migrationBuilder.DropColumn(
                name: "VolunteerStatus",
                table: "Volunteers");
        }
    }
}
