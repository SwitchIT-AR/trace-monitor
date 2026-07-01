using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAsnToIpGeoCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Asn",
                table: "IpGeoCache",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Asn",
                table: "IpGeoCache");
        }
    }
}
