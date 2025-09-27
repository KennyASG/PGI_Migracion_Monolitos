using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGI_Migracion_Monolitos.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProyectoAnalizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProyectoAnalizado",
                table: "Dependencias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProyectoAnalizado",
                table: "Dependencias");
        }
    }
}
