using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TransformadorService.DTOs;

namespace TransformadorService.Services
{
    public class GeneradorFuncionalService : IGeneradorFuncionalService
    {
        private readonly IConfiguration _configuration;

        public GeneradorFuncionalService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GenerarMicroservicioFuncionalAsync(
            string nombreProyecto, 
            string nombreModulo, 
            List<ClaseCompletaDto> clasesCompletas)
        {
            var outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "MicroserviciosGenerados");
            Directory.CreateDirectory(outputRoot);

            var microservicePath = Path.Combine(outputRoot, nombreModulo);
            if (Directory.Exists(microservicePath))
                Directory.Delete(microservicePath, true);
            
            Directory.CreateDirectory(microservicePath);

            // Separar modelos, servicios y controllers
            var modelos = ExtraerModelos(clasesCompletas);
            var servicios = clasesCompletas.Where(c => c.Nombre.EndsWith("Service")).ToList();
            var controllers = clasesCompletas.Where(c => c.Nombre.EndsWith("Controller")).ToList();

            // 1. Generar .csproj
            await GenerarCsproj(microservicePath, nombreModulo);

            // 2. Generar Shared (stubs para dependencias externas)
            await GenerarSharedStubs(microservicePath, nombreModulo);

            // 3. Generar modelos
            await GenerarModelos(microservicePath, modelos, nombreModulo);

            // 4. Generar servicios
            await GenerarServicios(microservicePath, servicios, nombreModulo);

            // 5. Generar controllers
            await GenerarControllers(microservicePath, controllers, nombreModulo);

            // 6. Generar Program.cs
            await GenerarProgramCs(microservicePath, servicios, controllers, nombreModulo);

            return microservicePath;
        }

        private Dictionary<string, ModeloDto> ExtraerModelos(List<ClaseCompletaDto> clasesCompletas)
        {
            var modelos = new Dictionary<string, ModeloDto>();
            var claseModelos = clasesCompletas.FirstOrDefault(c => c.Nombre == "_Modelos");

            if (claseModelos != null)
            {
                foreach (var usingStr in claseModelos.Usings)
                {
                    if (usingStr.StartsWith("Modelo:"))
                    {
                        var parts = usingStr.Split(':', 3);
                        if (parts.Length == 3)
                        {
                            var nombre = parts[1];
                            var codigo = parts[2];
                            modelos[nombre] = new ModeloDto
                            {
                                Nombre = nombre,
                                CodigoCompleto = codigo
                            };
                        }
                    }
                }
            }

            return modelos;
        }

        private async Task GenerarCsproj(string path, string nombreModulo)
        {
            var csproj = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
  </ItemGroup>
</Project>";

            await File.WriteAllTextAsync(Path.Combine(path, $"{nombreModulo}.csproj"), csproj);
        }

        private async Task GenerarSharedStubs(string path, string nombreModulo)
        {
            var sharedPath = Path.Combine(path, "Shared");
            Directory.CreateDirectory(sharedPath);

            // DatabaseContext stub
            var dbContext = $@"namespace {nombreModulo}.Shared;

public class DatabaseContext
{{
    public void SaveChanges()
    {{
        // Stub: Simulación de guardado en BD
        Console.WriteLine(""[STUB] DatabaseContext.SaveChanges() called"");
    }}

    public T FindById<T>(int id, List<T> collection) where T : class
    {{
        Console.WriteLine($""[STUB] DatabaseContext.FindById<{{typeof(T).Name}}>({{id}}) called"");
        return collection.ElementAtOrDefault(id - 1);
    }}
}}";

            await File.WriteAllTextAsync(Path.Combine(sharedPath, "DatabaseContext.cs"), dbContext);

            // LoggerService stub
            var logger = $@"namespace {nombreModulo}.Shared;

public class LoggerService
{{
    public void Log(string message)
    {{
        Console.WriteLine($""[LOG] {{message}}"");
    }}

    public void LogError(string message)
    {{
        Console.WriteLine($""[ERROR] {{message}}"");
    }}

    public void LogWarning(string message)
    {{
        Console.WriteLine($""[WARNING] {{message}}"");
    }}
}}";

            await File.WriteAllTextAsync(Path.Combine(sharedPath, "LoggerService.cs"), logger);

            // EmailService stub
            var email = $@"namespace {nombreModulo}.Shared;

public class EmailService
{{
    private readonly LoggerService _logger;

    public EmailService(LoggerService logger)
    {{
        _logger = logger;
    }}

    public void SendEmail(string to, string subject, string body)
    {{
        _logger.Log($""[STUB] Email sent to: {{to}}, Subject: {{subject}}"");
    }}

    public void SendWelcomeEmail(string email, string name)
    {{
        SendEmail(email, ""Welcome!"", $""Hello {{name}}, welcome!"");
    }}

    public void SendOrderConfirmation(string email, int orderId)
    {{
        SendEmail(email, ""Order Confirmation"", $""Order #{{orderId}} confirmed"");
    }}
}}";

            await File.WriteAllTextAsync(Path.Combine(sharedPath, "EmailService.cs"), email);
        }

        private async Task GenerarModelos(string path, Dictionary<string, ModeloDto> modelos, string nombreModulo)
        {
            if (!modelos.Any()) return;

            var modelosPath = Path.Combine(path, "Models");
            Directory.CreateDirectory(modelosPath);

            foreach (var modelo in modelos.Values)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"namespace {nombreModulo}.Models;");
                sb.AppendLine();
                sb.AppendLine(modelo.CodigoCompleto);

                await File.WriteAllTextAsync(
                    Path.Combine(modelosPath, $"{modelo.Nombre}.cs"), 
                    sb.ToString()
                );
            }
        }

        private async Task GenerarServicios(string path, List<ClaseCompletaDto> servicios, string nombreModulo)
        {
            if (!servicios.Any()) return;

            var servicesPath = Path.Combine(path, "Services");
            Directory.CreateDirectory(servicesPath);

            foreach (var servicio in servicios)
            {
                var sb = new StringBuilder();
                
                // Agregar usings básicos
                sb.AppendLine($"using {nombreModulo}.Models;");
                sb.AppendLine($"using {nombreModulo}.Shared;");
                sb.AppendLine();

                // Namespace
                sb.AppendLine($"namespace {nombreModulo}.Services;");
                sb.AppendLine();

                // Generar interfaz del servicio
                var interfazNombre = $"I{servicio.Nombre}";
                sb.AppendLine($"public interface {interfazNombre}");
                sb.AppendLine("{");
                
                foreach (var metodo in servicio.Metodos)
                {
                    var parametros = string.Join(", ", metodo.Parametros.Select(p => $"{p.Tipo} {p.Nombre}"));
                    sb.AppendLine($"    {metodo.TipoRetorno} {metodo.Nombre}({parametros});");
                }
                
                sb.AppendLine("}");
                sb.AppendLine();

                // Copiar la clase completa y reemplazar namespaces
                var codigoLimpio = servicio.CodigoClaseCompleta
                    .Replace("MonolithPro.Shared", $"{nombreModulo}.Shared")
                    .Replace("MonolithPro.Modules.Users", $"{nombreModulo}.Models")
                    .Replace("MonolithPro.Modules.Orders", $"{nombreModulo}.Models")
                    .Replace("MonolithPro.Modules.Inventory", $"{nombreModulo}.Models");

                sb.AppendLine(codigoLimpio);

                await File.WriteAllTextAsync(
                    Path.Combine(servicesPath, $"{servicio.Nombre}.cs"),
                    sb.ToString()
                );
            }
        }

        private async Task GenerarControllers(string path, List<ClaseCompletaDto> controllers, string nombreModulo)
        {
            if (!controllers.Any()) return;

            var controllersPath = Path.Combine(path, "Controllers");
            Directory.CreateDirectory(controllersPath);

            foreach (var controller in controllers)
            {
                var sb = new StringBuilder();
                
                // Usings
                sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
                sb.AppendLine($"using {nombreModulo}.Services;");
                sb.AppendLine($"using {nombreModulo}.Models;");
                sb.AppendLine();

                // Namespace
                sb.AppendLine($"namespace {nombreModulo}.Controllers;");
                sb.AppendLine();

                // Clase controller
                sb.AppendLine("[ApiController]");
                sb.AppendLine($"[Route(\"api/[controller]\")]");
                sb.AppendLine($"public class {controller.Nombre} : ControllerBase");
                sb.AppendLine("{");

                // Inyectar dependencias (solo servicios del mismo módulo)
                var serviciosModulo = controller.DependenciasConstructor
                    .Where(d => d.Contains("Service") && !d.Contains("Logger") && !d.Contains("Email") && !d.Contains("Database"))
                    .ToList();

                foreach (var dep in serviciosModulo)
                {
                    var depLimpia = dep.Replace("MonolithPro.Modules.Users.", "").Replace("MonolithPro.Modules.Orders.", "").Replace("MonolithPro.Modules.Inventory.", "");
                    var nombreCampo = $"_{char.ToLower(depLimpia[0])}{depLimpia.Substring(1)}";
                    sb.AppendLine($"    private readonly I{depLimpia} {nombreCampo};");
                }
                sb.AppendLine();

                // Constructor
                if (serviciosModulo.Any())
                {
                    var parametrosConstructor = string.Join(", ", 
                        serviciosModulo.Select(d => {
                            var depLimpia = d.Replace("MonolithPro.Modules.Users.", "").Replace("MonolithPro.Modules.Orders.", "").Replace("MonolithPro.Modules.Inventory.", "");
                            return $"I{depLimpia} {char.ToLower(depLimpia[0])}{depLimpia.Substring(1)}";
                        })
                    );
                    
                    sb.AppendLine($"    public {controller.Nombre}({parametrosConstructor})");
                    sb.AppendLine("    {");
                    foreach (var dep in serviciosModulo)
                    {
                        var depLimpia = dep.Replace("MonolithPro.Modules.Users.", "").Replace("MonolithPro.Modules.Orders.", "").Replace("MonolithPro.Modules.Inventory.", "");
                        var nombreParam = $"{char.ToLower(depLimpia[0])}{depLimpia.Substring(1)}";
                        var nombreCampo = $"_{nombreParam}";
                        sb.AppendLine($"        {nombreCampo} = {nombreParam};");
                    }
                    sb.AppendLine("    }");
                    sb.AppendLine();
                }

                // Métodos del controller
                foreach (var metodo in controller.Metodos)
                {
                    if (metodo.HttpVerb == null) continue;

                    // Normalizar atributo HTTP
                    var verb = NormalizarAtributoHttp(metodo.HttpVerb);
                    
                    if (!string.IsNullOrEmpty(metodo.Ruta))
                        sb.AppendLine($"    [Http{verb}(\"{metodo.Ruta}\")]");
                    else
                        sb.AppendLine($"    [Http{verb}]");

                    // Firma del método
                    var parametros = string.Join(", ", metodo.Parametros.Select(p =>
                    {
                        var atributo = p.EsFromBody ? "[FromBody] " : (p.EsFromRoute ? "" : "");
                        return $"{atributo}{p.Tipo} {p.Nombre}";
                    }));

                    sb.AppendLine($"    public {metodo.TipoRetorno} {metodo.Nombre}({parametros})");
                    
                    // Cuerpo del método - copiar tal cual
                    if (!string.IsNullOrEmpty(metodo.CodigoCompleto))
                    {
                        sb.AppendLine("    {");
                        sb.Append(metodo.CodigoCompleto);
                        sb.AppendLine("    }");
                    }
                    else
                    {
                        sb.AppendLine("    {");
                        sb.AppendLine($"        return Ok(new {{ message = \"{metodo.Nombre} ejecutado\" }});");
                        sb.AppendLine("    }");
                    }
                    
                    sb.AppendLine();
                }

                sb.AppendLine("}");

                await File.WriteAllTextAsync(
                    Path.Combine(controllersPath, $"{controller.Nombre}.cs"),
                    sb.ToString()
                );
            }
        }

        private async Task GenerarProgramCs(string path, List<ClaseCompletaDto> servicios, List<ClaseCompletaDto> controllers, string nombreModulo)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"using {nombreModulo}.Services;");
            sb.AppendLine($"using {nombreModulo}.Shared;");
            sb.AppendLine();
            sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
            sb.AppendLine();
            sb.AppendLine("// Registrar servicios compartidos (stubs)");
            sb.AppendLine("builder.Services.AddSingleton<DatabaseContext>();");
            sb.AppendLine("builder.Services.AddSingleton<LoggerService>();");
            sb.AppendLine("builder.Services.AddSingleton<EmailService>();");
            sb.AppendLine();
            sb.AppendLine("// Registrar servicios del módulo");
            sb.AppendLine("builder.Services.AddControllers();");
            sb.AppendLine("builder.Services.AddEndpointsApiExplorer();");
            sb.AppendLine("builder.Services.AddSwaggerGen();");
            sb.AppendLine();

            // Registrar servicios del módulo como Singleton para persistencia en memoria
            foreach (var servicio in servicios)
            {
                sb.AppendLine($"builder.Services.AddSingleton<I{servicio.Nombre}, {servicio.Nombre}>();");
            }

            sb.AppendLine();
            sb.AppendLine("var app = builder.Build();");
            sb.AppendLine();
            sb.AppendLine("if (app.Environment.IsDevelopment())");
            sb.AppendLine("{");
            sb.AppendLine("    app.UseSwagger();");
            sb.AppendLine("    app.UseSwaggerUI();");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("app.UseHttpsRedirection();");
            sb.AppendLine("app.UseAuthorization();");
            sb.AppendLine("app.MapControllers();");
            sb.AppendLine();
            sb.AppendLine("app.Run();");

            await File.WriteAllTextAsync(Path.Combine(path, "Program.cs"), sb.ToString());
        }

        private string NormalizarAtributoHttp(string httpVerb)
        {
            // Convertir Get, HttpGet, HTTP_GET, etc. a formato estándar
            var verb = httpVerb
                .Replace("Http", "", StringComparison.OrdinalIgnoreCase)
                .Replace("_", "")
                .Trim();

            // Capitalizar primera letra
            if (string.IsNullOrEmpty(verb)) return "Get";
            return char.ToUpper(verb[0]) + verb.Substring(1).ToLower();
        }
    }
}