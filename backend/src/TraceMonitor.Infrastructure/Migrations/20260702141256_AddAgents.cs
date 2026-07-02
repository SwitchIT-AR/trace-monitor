using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TraceMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TraceRuns_TargetId_StartedAtUtc",
                table: "TraceRuns");

            migrationBuilder.DropIndex(
                name: "IX_PathChangeEvents_TargetId_DetectedAtUtc",
                table: "PathChangeEvents");

            // defaultValue backfills existing rows to the built-in "Oficina" agent (Id 1, inserted
            // below) so the AddForeignKey calls at the end of this migration succeed; it is a
            // one-time backfill value, not a lasting column default (the app always sets AgentId
            // explicitly on new rows).
            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "TraceRuns",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "PathChangeEvents",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ApiKeyHash = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Lat = table.Column<double>(type: "double precision", nullable: true),
                    Lon = table.Column<double>(type: "double precision", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Agents",
                columns: new[] { "Id", "Address", "ApiKeyHash", "CreatedAtUtc", "IsActive", "IsBuiltIn", "LastSeenAtUtc", "Lat", "Location", "Lon", "Name", "Provider" },
                values: new object[] { 1, null, "builtin-no-auth", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, null, null, "Oficina", null, "Oficina", "Oficina" });

            migrationBuilder.CreateIndex(
                name: "IX_TraceRuns_AgentId",
                table: "TraceRuns",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRuns_TargetId_AgentId_StartedAtUtc",
                table: "TraceRuns",
                columns: new[] { "TargetId", "AgentId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PathChangeEvents_AgentId",
                table: "PathChangeEvents",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_PathChangeEvents_TargetId_AgentId_DetectedAtUtc",
                table: "PathChangeEvents",
                columns: new[] { "TargetId", "AgentId", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_ApiKeyHash",
                table: "Agents",
                column: "ApiKeyHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PathChangeEvents_Agents_AgentId",
                table: "PathChangeEvents",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TraceRuns_Agents_AgentId",
                table: "TraceRuns",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PathChangeEvents_Agents_AgentId",
                table: "PathChangeEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_TraceRuns_Agents_AgentId",
                table: "TraceRuns");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_TraceRuns_AgentId",
                table: "TraceRuns");

            migrationBuilder.DropIndex(
                name: "IX_TraceRuns_TargetId_AgentId_StartedAtUtc",
                table: "TraceRuns");

            migrationBuilder.DropIndex(
                name: "IX_PathChangeEvents_AgentId",
                table: "PathChangeEvents");

            migrationBuilder.DropIndex(
                name: "IX_PathChangeEvents_TargetId_AgentId_DetectedAtUtc",
                table: "PathChangeEvents");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "TraceRuns");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "PathChangeEvents");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRuns_TargetId_StartedAtUtc",
                table: "TraceRuns",
                columns: new[] { "TargetId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PathChangeEvents_TargetId_DetectedAtUtc",
                table: "PathChangeEvents",
                columns: new[] { "TargetId", "DetectedAtUtc" });
        }
    }
}
