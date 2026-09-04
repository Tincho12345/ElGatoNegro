using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajosWeb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPreciosYOfertasATrabajo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EtiquetaOferta",
                table: "Trabajos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Precio",
                table: "Trabajos",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioAnterior",
                table: "Trabajos",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextoOferta",
                table: "Trabajos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EtiquetaOferta",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "Precio",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "PrecioAnterior",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "TextoOferta",
                table: "Trabajos");
        }
    }
}
