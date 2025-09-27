using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PGI_Migracion_Monolitos.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dependencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaseOrigen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ClaseDependencia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NamespaceOrigen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NamespaceDependencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaAnalisis = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencias", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dependencias");
        }
    }
}
