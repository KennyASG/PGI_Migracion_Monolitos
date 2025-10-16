using TransformadorService.DTOs;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransformadorService.Services
{
    public class TransformadorService : ITransformadorService
    {
        private readonly string baseRuta = Path.Combine(Path.GetTempPath(), "MicroserviciosGenerados");

        public TransformadorService()
        {
            if (!Directory.Exists(baseRuta))
                Directory.CreateDirectory(baseRuta);
        }

        public async Task<MicroserviceGenerationResult> GenerarMicroservicioAsync(string proyecto, string nombreModulo)
        {
            // Crear carpeta específica para este microservicio
            var rutaMicroservicio = Path.Combine(baseRuta, $"{nombreModulo}_{Guid.NewGuid()}");
            Directory.CreateDirectory(rutaMicroservicio);

            // Para el POC: copiamos el módulo “quemado” de ejemplo
            var clasesExtraidas = new List<ClaseMicroservicioDto>();

            // Aquí se simula la extracción de clases del monolito
            clasesExtraidas.Add(new ClaseMicroservicioDto
            {
                Nombre = "PaymentsManager",
                Namespace = "Monolito.Modules.Payments",
                Metodos = new List<MetodoMicroservicioDto>
                {
                    new MetodoMicroservicioDto { Nombre = "ProcessPayment", HttpMethod = "POST", Route = "/payments/process" },
                    new MetodoMicroservicioDto { Nombre = "RefundPayment", HttpMethod = "POST", Route = "/payments/refund" }
                }
            });

            // Generar Program.cs del microservicio mínimo
            var programCs = @$"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Endpoints generados automáticamente
app.MapPost(""/payments/process"", () => Results.Ok(""Pago procesado""));
app.MapPost(""/payments/refund"", () => Results.Ok(""Pago devuelto""));

app.Run();
";
            await File.WriteAllTextAsync(Path.Combine(rutaMicroservicio, "Program.cs"), programCs);

            // Generar .csproj mínimo
            var csproj = @$"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
";
            await File.WriteAllTextAsync(Path.Combine(rutaMicroservicio, $"{nombreModulo}.csproj"), csproj);

            // Devolver resultado
            return new MicroserviceGenerationResult
            {
                RutaMicroservicio = rutaMicroservicio,
                ClasesExtraidas = clasesExtraidas,
                RefactorizacionExitosa = true
            };
        }
    }
}
