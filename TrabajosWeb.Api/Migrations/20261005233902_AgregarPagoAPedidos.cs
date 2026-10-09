using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajosWeb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPagoAPedidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoPago",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPaymentId",
                table: "Pedidos",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MetodoPago",
                table: "Pedidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PagadoEl",
                table: "Pedidos",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MercadoPagoPaymentId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "PagadoEl",
                table: "Pedidos");
        }
    }
}
