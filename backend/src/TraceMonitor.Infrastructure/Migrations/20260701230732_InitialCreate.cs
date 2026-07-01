using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TraceMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpGeoCache",
                columns: table => new
                {
                    Ip = table.Column<string>(type: "text", nullable: false),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    City = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Isp = table.Column<string>(type: "text", nullable: true),
                    Org = table.Column<string>(type: "text", nullable: true),
                    Lat = table.Column<double>(type: "double precision", nullable: true),
                    Lon = table.Column<double>(type: "double precision", nullable: true),
                    FetchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpGeoCache", x => x.Ip);
                });

            migrationBuilder.CreateTable(
                name: "Targets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    DestinationHost = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Targets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PathChangeEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetId = table.Column<int>(type: "integer", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousHopsJson = table.Column<string>(type: "text", nullable: false),
                    NewHopsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathChangeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PathChangeEvents_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetId = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PacketsSent = table.Column<int>(type: "integer", nullable: false),
                    HopCount = table.Column<int>(type: "integer", nullable: false),
                    PathHash = table.Column<string>(type: "text", nullable: false),
                    OverallLossPct = table.Column<double>(type: "double precision", nullable: false),
                    OverallAvgRttMs = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceRuns_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TraceHops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TraceRunId = table.Column<long>(type: "bigint", nullable: false),
                    HopIndex = table.Column<int>(type: "integer", nullable: false),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    Hostname = table.Column<string>(type: "text", nullable: true),
                    LossPct = table.Column<double>(type: "double precision", nullable: false),
                    Sent = table.Column<int>(type: "integer", nullable: false),
                    Last = table.Column<double>(type: "double precision", nullable: false),
                    Avg = table.Column<double>(type: "double precision", nullable: false),
                    Best = table.Column<double>(type: "double precision", nullable: false),
                    Worst = table.Column<double>(type: "double precision", nullable: false),
                    StDev = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraceHops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraceHops_TraceRuns_TraceRunId",
                        column: x => x.TraceRunId,
                        principalTable: "TraceRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Targets",
                columns: new[] { "Id", "CreatedAtUtc", "DestinationHost", "IsActive", "Name", "Provider" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "186.19.218.8", true, "Griveo", "Telecentro" },
                    { 2, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "190.210.245.120", true, "Vaclog", "IPLAN" },
                    { 3, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "186.23.255.158", true, "Managio (DC)", "Telecentro" },
                    { 4, new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), "186.23.255.156", true, "Vaclog (DC)", "Telecentro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PathChangeEvents_TargetId_DetectedAtUtc",
                table: "PathChangeEvents",
                columns: new[] { "TargetId", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Targets_DestinationHost",
                table: "Targets",
                column: "DestinationHost",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TraceHops_TraceRunId",
                table: "TraceHops",
                column: "TraceRunId");

            migrationBuilder.CreateIndex(
                name: "IX_TraceRuns_TargetId_StartedAtUtc",
                table: "TraceRuns",
                columns: new[] { "TargetId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpGeoCache");

            migrationBuilder.DropTable(
                name: "PathChangeEvents");

            migrationBuilder.DropTable(
                name: "TraceHops");

            migrationBuilder.DropTable(
                name: "TraceRuns");

            migrationBuilder.DropTable(
                name: "Targets");
        }
    }
}
