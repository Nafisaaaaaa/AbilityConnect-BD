using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationServicesAndLearningHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AwarenessEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EndTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Eligibility = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContactInfo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwarenessEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwarenessEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    YouTubeUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningVideos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedOpportunities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    OpportunityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OpportunityId = table.Column<int>(type: "integer", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedOpportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedOpportunities_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scholarships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ScholarshipType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FieldOfStudy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Benefits = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Eligibility = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequiredQualifications = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApplicationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactInfo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scholarships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scholarships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TrainingCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SkillsCovered = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    DeliveryMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Eligibility = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: true),
                    ContactInfo = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingPrograms_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AwarenessEventId = table.Column<int>(type: "integer", nullable: false),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventRegistrations_AwarenessEvents_AwarenessEventId",
                        column: x => x.AwarenessEventId,
                        principalTable: "AwarenessEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventRegistrations_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScholarshipApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScholarshipId = table.Column<int>(type: "integer", nullable: false),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    HighestQualification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Institution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldOfStudy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PassingYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcademicResult = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TechnicalSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProfessionalSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OtherSkills = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Motivation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RelevantExperience = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CVFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CVOriginalFileName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScholarshipApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScholarshipApplications_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScholarshipApplications_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrainingProgramId = table.Column<int>(type: "integer", nullable: false),
                    DisabilityUserId = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRegistrations_DisabilityUsers_DisabilityUserId",
                        column: x => x.DisabilityUserId,
                        principalTable: "DisabilityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingRegistrations_TrainingPrograms_TrainingProgramId",
                        column: x => x.TrainingProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LearningVideos",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Duration", "IsPublished", "Title", "UpdatedAt", "YouTubeUrl" },
                values: new object[,]
                {
                    { 1, "Braille", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Learn the fundamentals of Unified English Braille, understanding the 6-dot cell system, letter symbols, and tactile reading techniques.", "18 mins", true, "Introduction to Unified English Braille (UEB)", null, "https://www.youtube.com/watch?v=1n_2hL8N3Jk" },
                    { 2, "Sign Language", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A friendly beginner's guide to fingerspelling the alphabet, daily greetings, and key conversational signs for effective visual communication.", "15 mins", true, "Basic Sign Language Alphabet and Common Phrases", null, "https://www.youtube.com/watch?v=0FcwzMq4iWg" },
                    { 3, "Computer Skills", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Comprehensive walkthrough of using the free NonVisual Desktop Access (NVDA) screen reader on Windows for browsing, word processing, and file navigation.", "22 mins", true, "Mastering NVDA Screen Reader: Essential Shortcuts", null, "https://www.youtube.com/watch?v=dEbl5jvLKGQ" },
                    { 4, "Freelancing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Practical roadmap to setting up remote freelance profiles in data entry, virtual assistance, content writing, and graphic design from home.", "25 mins", true, "Freelancing Opportunities for People with Disabilities", null, "https://www.youtube.com/watch?v=J---aiyznGQ" },
                    { 5, "Digital Literacy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Explore built-in accessibility suites on modern smartphones, including TalkBack, VoiceOver, Magnifier, Live Captions, and High Contrast displays.", "20 mins", true, "Smartphone Accessibility: Android & iOS Features", null, "https://www.youtube.com/watch?v=9No-FiEInLA" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AwarenessEvents_OrganizationId",
                table: "AwarenessEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_AwarenessEventId_DisabilityUserId",
                table: "EventRegistrations",
                columns: new[] { "AwarenessEventId", "DisabilityUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_DisabilityUserId",
                table: "EventRegistrations",
                column: "DisabilityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningVideos_Category",
                table: "LearningVideos",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SavedOpportunities_DisabilityUserId_OpportunityType_Opportu~",
                table: "SavedOpportunities",
                columns: new[] { "DisabilityUserId", "OpportunityType", "OpportunityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipApplications_DisabilityUserId",
                table: "ScholarshipApplications",
                column: "DisabilityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ScholarshipApplications_ScholarshipId_DisabilityUserId",
                table: "ScholarshipApplications",
                columns: new[] { "ScholarshipId", "DisabilityUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_OrganizationId",
                table: "Scholarships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_OrganizationId",
                table: "TrainingPrograms",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRegistrations_DisabilityUserId",
                table: "TrainingRegistrations",
                column: "DisabilityUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRegistrations_TrainingProgramId_DisabilityUserId",
                table: "TrainingRegistrations",
                columns: new[] { "TrainingProgramId", "DisabilityUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRegistrations");

            migrationBuilder.DropTable(
                name: "LearningVideos");

            migrationBuilder.DropTable(
                name: "SavedOpportunities");

            migrationBuilder.DropTable(
                name: "ScholarshipApplications");

            migrationBuilder.DropTable(
                name: "TrainingRegistrations");

            migrationBuilder.DropTable(
                name: "AwarenessEvents");

            migrationBuilder.DropTable(
                name: "Scholarships");

            migrationBuilder.DropTable(
                name: "TrainingPrograms");
        }
    }
}
