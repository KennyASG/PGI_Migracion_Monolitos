using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ContainerizationService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MicroserviceContainers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreModulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombreProyecto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RutaMicroservicio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImagenDocker = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContenedorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PuertoAsignado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    FechaUltimaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DockerComposeFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DockerfilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MicroserviceContainers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Puerto = table.Column<int>(type: "int", nullable: false),
                    EnUso = table.Column<bool>(type: "bit", nullable: false),
                    MicroserviceContainerId = table.Column<int>(type: "int", nullable: true),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaLiberacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortAssignments_MicroserviceContainers_MicroserviceContainerId",
                        column: x => x.MicroserviceContainerId,
                        principalTable: "MicroserviceContainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "PortAssignments",
                columns: new[] { "Id", "EnUso", "FechaAsignacion", "FechaLiberacion", "MicroserviceContainerId", "Puerto" },
                values: new object[,]
                {
                    { 1, false, null, null, null, 6000 },
                    { 2, false, null, null, null, 6001 },
                    { 3, false, null, null, null, 6002 },
                    { 4, false, null, null, null, 6003 },
                    { 5, false, null, null, null, 6004 },
                    { 6, false, null, null, null, 6005 },
                    { 7, false, null, null, null, 6006 },
                    { 8, false, null, null, null, 6007 },
                    { 9, false, null, null, null, 6008 },
                    { 10, false, null, null, null, 6009 },
                    { 11, false, null, null, null, 6010 },
                    { 12, false, null, null, null, 6011 },
                    { 13, false, null, null, null, 6012 },
                    { 14, false, null, null, null, 6013 },
                    { 15, false, null, null, null, 6014 },
                    { 16, false, null, null, null, 6015 },
                    { 17, false, null, null, null, 6016 },
                    { 18, false, null, null, null, 6017 },
                    { 19, false, null, null, null, 6018 },
                    { 20, false, null, null, null, 6019 },
                    { 21, false, null, null, null, 6020 },
                    { 22, false, null, null, null, 6021 },
                    { 23, false, null, null, null, 6022 },
                    { 24, false, null, null, null, 6023 },
                    { 25, false, null, null, null, 6024 },
                    { 26, false, null, null, null, 6025 },
                    { 27, false, null, null, null, 6026 },
                    { 28, false, null, null, null, 6027 },
                    { 29, false, null, null, null, 6028 },
                    { 30, false, null, null, null, 6029 },
                    { 31, false, null, null, null, 6030 },
                    { 32, false, null, null, null, 6031 },
                    { 33, false, null, null, null, 6032 },
                    { 34, false, null, null, null, 6033 },
                    { 35, false, null, null, null, 6034 },
                    { 36, false, null, null, null, 6035 },
                    { 37, false, null, null, null, 6036 },
                    { 38, false, null, null, null, 6037 },
                    { 39, false, null, null, null, 6038 },
                    { 40, false, null, null, null, 6039 },
                    { 41, false, null, null, null, 6040 },
                    { 42, false, null, null, null, 6041 },
                    { 43, false, null, null, null, 6042 },
                    { 44, false, null, null, null, 6043 },
                    { 45, false, null, null, null, 6044 },
                    { 46, false, null, null, null, 6045 },
                    { 47, false, null, null, null, 6046 },
                    { 48, false, null, null, null, 6047 },
                    { 49, false, null, null, null, 6048 },
                    { 50, false, null, null, null, 6049 },
                    { 51, false, null, null, null, 6050 },
                    { 52, false, null, null, null, 6051 },
                    { 53, false, null, null, null, 6052 },
                    { 54, false, null, null, null, 6053 },
                    { 55, false, null, null, null, 6054 },
                    { 56, false, null, null, null, 6055 },
                    { 57, false, null, null, null, 6056 },
                    { 58, false, null, null, null, 6057 },
                    { 59, false, null, null, null, 6058 },
                    { 60, false, null, null, null, 6059 },
                    { 61, false, null, null, null, 6060 },
                    { 62, false, null, null, null, 6061 },
                    { 63, false, null, null, null, 6062 },
                    { 64, false, null, null, null, 6063 },
                    { 65, false, null, null, null, 6064 },
                    { 66, false, null, null, null, 6065 },
                    { 67, false, null, null, null, 6066 },
                    { 68, false, null, null, null, 6067 },
                    { 69, false, null, null, null, 6068 },
                    { 70, false, null, null, null, 6069 },
                    { 71, false, null, null, null, 6070 },
                    { 72, false, null, null, null, 6071 },
                    { 73, false, null, null, null, 6072 },
                    { 74, false, null, null, null, 6073 },
                    { 75, false, null, null, null, 6074 },
                    { 76, false, null, null, null, 6075 },
                    { 77, false, null, null, null, 6076 },
                    { 78, false, null, null, null, 6077 },
                    { 79, false, null, null, null, 6078 },
                    { 80, false, null, null, null, 6079 },
                    { 81, false, null, null, null, 6080 },
                    { 82, false, null, null, null, 6081 },
                    { 83, false, null, null, null, 6082 },
                    { 84, false, null, null, null, 6083 },
                    { 85, false, null, null, null, 6084 },
                    { 86, false, null, null, null, 6085 },
                    { 87, false, null, null, null, 6086 },
                    { 88, false, null, null, null, 6087 },
                    { 89, false, null, null, null, 6088 },
                    { 90, false, null, null, null, 6089 },
                    { 91, false, null, null, null, 6090 },
                    { 92, false, null, null, null, 6091 },
                    { 93, false, null, null, null, 6092 },
                    { 94, false, null, null, null, 6093 },
                    { 95, false, null, null, null, 6094 },
                    { 96, false, null, null, null, 6095 },
                    { 97, false, null, null, null, 6096 },
                    { 98, false, null, null, null, 6097 },
                    { 99, false, null, null, null, 6098 },
                    { 100, false, null, null, null, 6099 },
                    { 101, false, null, null, null, 6100 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MicroserviceContainers_ContenedorId",
                table: "MicroserviceContainers",
                column: "ContenedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MicroserviceContainers_Estado",
                table: "MicroserviceContainers",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_MicroserviceContainers_NombreModulo",
                table: "MicroserviceContainers",
                column: "NombreModulo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortAssignments_EnUso",
                table: "PortAssignments",
                column: "EnUso");

            migrationBuilder.CreateIndex(
                name: "IX_PortAssignments_MicroserviceContainerId",
                table: "PortAssignments",
                column: "MicroserviceContainerId",
                unique: true,
                filter: "[MicroserviceContainerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PortAssignments_Puerto",
                table: "PortAssignments",
                column: "Puerto",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortAssignments");

            migrationBuilder.DropTable(
                name: "MicroserviceContainers");
        }
    }
}
