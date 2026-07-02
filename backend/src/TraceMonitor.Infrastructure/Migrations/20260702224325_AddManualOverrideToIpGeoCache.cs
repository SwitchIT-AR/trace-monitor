using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualOverrideToIpGeoCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManualOverride",
                table: "IpGeoCache",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManualOverride",
                table: "IpGeoCache");
        }
    }
}
