using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TraceMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteSignatureAndRouteLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RouteSignatureHash",
                table: "TraceRuns",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RouteLabels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetId = table.Column<int>(type: "integer", nullable: false),
                    AgentId = table.Column<int>(type: "integer", nullable: false),
                    RouteSignatureHash = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteLabels_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteLabels_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TraceRuns_TargetId_AgentId_RouteSignatureHash",
                table: "TraceRuns",
                columns: new[] { "TargetId", "AgentId", "RouteSignatureHash" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteLabels_AgentId",
                table: "RouteLabels",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteLabels_TargetId_AgentId_RouteSignatureHash",
                table: "RouteLabels",
                columns: new[] { "TargetId", "AgentId", "RouteSignatureHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteLabels");

            migrationBuilder.DropIndex(
                name: "IX_TraceRuns_TargetId_AgentId_RouteSignatureHash",
                table: "TraceRuns");

            migrationBuilder.DropColumn(
                name: "RouteSignatureHash",
                table: "TraceRuns");
        }
    }
}
