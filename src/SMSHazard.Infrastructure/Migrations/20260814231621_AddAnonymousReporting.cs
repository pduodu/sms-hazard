using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMSHazard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "HazardReports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "HazardReports",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HazardReports_TrackingCode",
                table: "HazardReports",
                column: "TrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HazardReports_TrackingCode",
                table: "HazardReports");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "HazardReports");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "HazardReports");
        }
    }
}
