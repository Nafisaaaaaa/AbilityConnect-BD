using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SDP1.Migrations
{
    /// <inheritdoc />
    public partial class AddOtherTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Facebook",
                table: "Organizations",
                newName: "OtherOrganizationType");

            migrationBuilder.AddColumn<string>(
                name: "FacebookProfileLink",
                table: "Organizations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherDisabilityType",
                table: "DisabilityUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookProfileLink",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "OtherDisabilityType",
                table: "DisabilityUsers");

            migrationBuilder.RenameColumn(
                name: "OtherOrganizationType",
                table: "Organizations",
                newName: "Facebook");
        }
    }
}
