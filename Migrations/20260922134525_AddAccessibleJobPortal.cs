using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessibleJobPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    JobType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WorkplaceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Salary = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccessibilityFeatures = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FlexibleWorkingHours = table.Column<bool>(type: "boolean", nullable: false),
                    AssistiveTechnologySupport = table.Column<bool>(type: "boolean", nullable: false),
                    AssistiveTechDetails = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequiredSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EducationRequirement = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ApplicationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VacancyCount = table.Column<int>(type: "integer", nullable: false),
                    ExperienceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    JobStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    ApplicantFullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApplicantEmail = table.Column<string>(type: "text", nullable: false),
                    ApplicantPhone = table.Column<string>(type: "text", nullable: false),
                    ResumePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ResumeFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CoverLetter = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AccommodationRequested = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrganizationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplications_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobApplications_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<int>(type: "integer", nullable: false),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedJobs_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_DisabilityUserId",
                table: "JobApplications",
                column: "DisabilityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobId_DisabilityUserId",
                table: "JobApplications",
                columns: new[] { "JobId", "DisabilityUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_OrganizationId",
                table: "Jobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_DisabilityUserId",
                table: "SavedJobs",
                column: "DisabilityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_JobId_DisabilityUserId",
                table: "SavedJobs",
                columns: new[] { "JobId", "DisabilityUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropTable(
                name: "SavedJobs");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
