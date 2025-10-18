using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TransformadorService.DTOs;

namespace TransformadorService.Services
{
    public class GeneradorMicroservicioService : IGeneradorMicroservicioService
    {
        private readonly IConfiguration _configuration;

        public GeneradorMicroservicioService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GenerarMicroservicioAsync(string nombreProyecto, string nombreModulo, List<ClaseAnalizadaDto> clases)
        {
            var outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "MicroserviciosGenerados");
            Directory.CreateDirectory(outputRoot);

            var microservicePath = Path.Combine(outputRoot, nombreModulo);
            Directory.CreateDirectory(microservicePath);

            // --- 1️ .csproj ---
            var csprojContent = $@"
            <Project Sdk=""Microsoft.NET.Sdk.Web"">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            </Project>";
            await File.WriteAllTextAsync(Path.Combine(microservicePath, $"{nombreModulo}.csproj"), csprojContent);

            // --- 2️Program.cs ---
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Http;");
            sb.AppendLine();
            sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
            sb.AppendLine("var app = builder.Build();");
            sb.AppendLine();

            foreach (var clase in clases.Where(c => c.Metodos.Any(m => m.HttpVerb != null)))
            {
                foreach (var metodo in clase.Metodos.Where(m => m.HttpVerb != null))
                {
                    var verb = metodo.HttpVerb.Replace("Http", "", StringComparison.OrdinalIgnoreCase).ToUpper();
                    var ruta = metodo.Ruta ?? $"/api/{nombreModulo.ToLower()}/{metodo.Nombre.ToLower()}";

                    switch (verb)
                    {
                        case "GET":
                            sb.AppendLine($"app.MapGet(\"{ruta}\", () => Results.Ok(\"{metodo.Nombre} ejecutado\"));");
                            break;
                        case "POST":
                            sb.AppendLine($"app.MapPost(\"{ruta}\", () => Results.Ok(\"{metodo.Nombre} ejecutado\"));");
                            break;
                        case "PUT":
                            sb.AppendLine($"app.MapPut(\"{ruta}\", () => Results.Ok(\"{metodo.Nombre} ejecutado\"));");
                            break;
                        case "DELETE":
                            sb.AppendLine($"app.MapDelete(\"{ruta}\", () => Results.Ok(\"{metodo.Nombre} ejecutado\"));");
                            break;
                        default:
                            sb.AppendLine($"// No se generó endpoint para {metodo.Nombre} (verbo {verb})");
                            break;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("app.Run();");

            await File.WriteAllTextAsync(Path.Combine(microservicePath, "Program.cs"), sb.ToString());

            return microservicePath;
        }
    }
}
