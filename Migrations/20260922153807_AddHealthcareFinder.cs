using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthcareFinder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthcareProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ProviderType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    OrganizationOrClinic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Specialization = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    AvailableServices = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ConsultationFee = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AvailabilitySchedule = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ProfileImage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrganizationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthcareProviders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthcareProviders_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HealthcareAppointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HealthcareProviderId = table.Column<int>(type: "integer", nullable: false),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeSlot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReasonForVisit = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PatientNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DoctorNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthcareAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthcareAppointments_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthcareAppointments_HealthcareProviders_HealthcareProvid~",
                        column: x => x.HealthcareProviderId,
                        principalTable: "HealthcareProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HealthcareReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HealthcareProviderId = table.Column<int>(type: "integer", nullable: false),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    ReviewText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthcareReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthcareReviews_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HealthcareReviews_HealthcareProviders_HealthcareProviderId",
                        column: x => x.HealthcareProviderId,
                        principalTable: "HealthcareProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareAppointments_DisabilityUserId",
                table: "HealthcareAppointments",
                column: "DisabilityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareAppointments_HealthcareProviderId",
                table: "HealthcareAppointments",
                column: "HealthcareProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareProviders_OrganizationId",
                table: "HealthcareProviders",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareReviews_DisabilityUserId",
                table: "HealthcareReviews",
                column: "DisabilityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthcareReviews_HealthcareProviderId_DisabilityUserId",
                table: "HealthcareReviews",
                columns: new[] { "HealthcareProviderId", "DisabilityUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthcareAppointments");

            migrationBuilder.DropTable(
                name: "HealthcareReviews");

            migrationBuilder.DropTable(
                name: "HealthcareProviders");
        }
    }
}
