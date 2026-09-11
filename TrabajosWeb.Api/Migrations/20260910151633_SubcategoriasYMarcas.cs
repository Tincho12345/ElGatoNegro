using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajosWeb.Api.Migrations
{
    /// <inheritdoc />
    public partial class SubcategoriasYMarcas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MarcaId",
                table: "Trabajos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubcategoriaId",
                table: "Trabajos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subcategorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subcategorias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trabajos_MarcaId",
                table: "Trabajos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Trabajos_Precio",
                table: "Trabajos",
                column: "Precio");

            migrationBuilder.CreateIndex(
                name: "IX_Trabajos_SubcategoriaId",
                table: "Trabajos",
                column: "SubcategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Orden",
                table: "Marcas",
                column: "Orden");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Slug",
                table: "Marcas",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subcategorias_Orden",
                table: "Subcategorias",
                column: "Orden");

            migrationBuilder.CreateIndex(
                name: "IX_Subcategorias_Slug",
                table: "Subcategorias",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajos_Marcas_MarcaId",
                table: "Trabajos",
                column: "MarcaId",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trabajos_Subcategorias_SubcategoriaId",
                table: "Trabajos",
                column: "SubcategoriaId",
                principalTable: "Subcategorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trabajos_Marcas_MarcaId",
                table: "Trabajos");

            migrationBuilder.DropForeignKey(
                name: "FK_Trabajos_Subcategorias_SubcategoriaId",
                table: "Trabajos");

            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.DropTable(
                name: "Subcategorias");

            migrationBuilder.DropIndex(
                name: "IX_Trabajos_MarcaId",
                table: "Trabajos");

            migrationBuilder.DropIndex(
                name: "IX_Trabajos_Precio",
                table: "Trabajos");

            migrationBuilder.DropIndex(
                name: "IX_Trabajos_SubcategoriaId",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "Trabajos");

            migrationBuilder.DropColumn(
                name: "SubcategoriaId",
                table: "Trabajos");
        }
    }
}
