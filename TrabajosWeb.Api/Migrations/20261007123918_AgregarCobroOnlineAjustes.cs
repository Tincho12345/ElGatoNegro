using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrabajosWeb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCobroOnlineAjustes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CobroOnlineMercadoPago",
                table: "AjustesSitio",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CobroOnlineMercadoPago",
                table: "AjustesSitio");
        }
    }
}
