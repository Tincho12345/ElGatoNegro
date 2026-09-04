using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajosWeb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposServicioACategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MostrarEnServicios",
                table: "Categorias",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TextoServicio",
                table: "Categorias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MostrarEnServicios",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "TextoServicio",
                table: "Categorias");
        }
    }
}