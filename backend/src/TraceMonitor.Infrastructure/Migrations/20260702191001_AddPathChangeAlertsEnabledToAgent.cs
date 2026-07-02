using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPathChangeAlertsEnabledToAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defaults to true for every existing agent (built-in office + any already-deployed
            // remote agents) — silencing is an explicit opt-out via the agent edit form, not
            // something a schema migration should do to agents it doesn't know about.
            migrationBuilder.AddColumn<bool>(
                name: "PathChangeAlertsEnabled",
                table: "Agents",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PathChangeAlertsEnabled",
                table: "Agents");
        }
    }
}
