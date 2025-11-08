using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RefactorizacionService.Migrations
{
    /// <inheritdoc />
    public partial class InitialRefactorizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefactorizacionHistorial",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreProyecto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModuloRefactorizado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FechaRefactorizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CodigoDiff = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MensajeError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CantidadDependenciasRefactorizadas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefactorizacionHistorial", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiciosMigrados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreProyecto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreModulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UrlMicroservicio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Puerto = table.Column<int>(type: "int", nullable: false),
                    FechaMigracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstaActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiciosMigrados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DependenciasRefactorizadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefactorizacionHistorialId = table.Column<int>(type: "int", nullable: false),
                    NombreClase = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreMetodo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServicioOriginal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CodigoOriginal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoRefactorizado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoRefactorizacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LineaInicio = table.Column<int>(type: "int", nullable: false),
                    LineaFin = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependenciasRefactorizadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DependenciasRefactorizadas_RefactorizacionHistorial_RefactorizacionHistorialId",
                        column: x => x.RefactorizacionHistorialId,
                        principalTable: "RefactorizacionHistorial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DependenciasRefactorizadas_RefactorizacionHistorialId",
                table: "DependenciasRefactorizadas",
                column: "RefactorizacionHistorialId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosMigrados_NombreProyecto_NombreModulo",
                table: "ServiciosMigrados",
                columns: new[] { "NombreProyecto", "NombreModulo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DependenciasRefactorizadas");

            migrationBuilder.DropTable(
                name: "ServiciosMigrados");

            migrationBuilder.DropTable(
                name: "RefactorizacionHistorial");
        }
    }
}
