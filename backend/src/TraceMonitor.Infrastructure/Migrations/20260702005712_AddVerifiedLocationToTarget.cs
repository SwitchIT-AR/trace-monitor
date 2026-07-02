using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedLocationToTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerifiedAddress",
                table: "Targets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VerifiedLat",
                table: "Targets",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VerifiedLon",
                table: "Targets",
                type: "double precision",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Targets",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "VerifiedAddress", "VerifiedLat", "VerifiedLon" },
                values: new object[] { "Gral. José G. Artigas 4901, Villa Pueyrredón, CABA (Farmacia Nueva Social Fenix, suc. Griveo)", -34.581636600000003, -58.499207300000002 });

            migrationBuilder.UpdateData(
                table: "Targets",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "VerifiedAddress", "VerifiedLat", "VerifiedLon" },
                values: new object[] { "Av. Corrientes y 25 de Mayo, San Nicolás, CABA (aprox.)", -34.6038067, -58.3842268 });

            migrationBuilder.UpdateData(
                table: "Targets",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "VerifiedAddress", "VerifiedLat", "VerifiedLon" },
                values: new object[] { "Cnel. Pringles 3407, Lomas del Mirador, La Matanza (Datacenter Telecentro)", -34.671254300000001, -58.5375181 });

            migrationBuilder.UpdateData(
                table: "Targets",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "VerifiedAddress", "VerifiedLat", "VerifiedLon" },
                values: new object[] { "Cnel. Pringles 3407, Lomas del Mirador, La Matanza (Datacenter Telecentro)", -34.671254300000001, -58.5375181 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifiedAddress",
                table: "Targets");

            migrationBuilder.DropColumn(
                name: "VerifiedLat",
                table: "Targets");

            migrationBuilder.DropColumn(
                name: "VerifiedLon",
                table: "Targets");
        }
    }
}
