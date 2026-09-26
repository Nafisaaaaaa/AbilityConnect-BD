using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHealthcareProviderVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "HealthcareProviders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "HealthcareProviders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
