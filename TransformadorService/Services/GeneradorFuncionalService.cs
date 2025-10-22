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

            // 1. Generar .csproj CON ENTITY FRAMEWORK
            await GenerarCsprojConEF(microservicePath, nombreModulo);

            // 2. Generar DbContext
            await GenerarDbContext(microservicePath, nombreModulo, modelos);

            // 3. Generar Shared (stubs simplificados)
            await GenerarSharedStubs(microservicePath, nombreModulo);

            // 4. Generar modelos
            await GenerarModelos(microservicePath, modelos, nombreModulo);

            // 5. Generar servicios CON EF CORE
            await GenerarServiciosConEF(microservicePath, servicios, nombreModulo);

            // 6. Generar controllers
            await GenerarControllers(microservicePath, controllers, nombreModulo);

            // 7. Generar Program.cs CON EF CORE
            await GenerarProgramCsConEF(microservicePath, servicios, controllers, nombreModulo);

            // 8. Generar appsettings.json con variables de entorno
            await GenerarAppSettings(microservicePath);

            // 9. Generar Dockerfile
            await GenerarDockerfile(microservicePath, nombreModulo);

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

        private async Task GenerarCsprojConEF(string path, string nombreModulo)
        {
            var csproj = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""8.0.0"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>";

            await File.WriteAllTextAsync(Path.Combine(path, $"{nombreModulo}.csproj"), csproj);
        }

        private async Task GenerarDbContext(string path, string nombreModulo, Dictionary<string, ModeloDto> modelos)
        {
            var dataPath = Path.Combine(path, "Data");
            Directory.CreateDirectory(dataPath);

            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine($"using {nombreModulo}.Models;");
            sb.AppendLine();
            sb.AppendLine($"namespace {nombreModulo}.Data;");
            sb.AppendLine();
            sb.AppendLine($"public class {nombreModulo}DbContext : DbContext");
            sb.AppendLine("{");
            sb.AppendLine($"    public {nombreModulo}DbContext(DbContextOptions<{nombreModulo}DbContext> options) : base(options)");
            sb.AppendLine("    {");
            sb.AppendLine("    }");
            sb.AppendLine();

            // DbSets para cada modelo
            foreach (var modelo in modelos.Values)
            {
                sb.AppendLine($"    public DbSet<{modelo.Nombre}> {modelo.Nombre}s {{ get; set; }}");
            }

            sb.AppendLine();
            sb.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
            sb.AppendLine("    {");
            sb.AppendLine("        base.OnModelCreating(modelBuilder);");
            sb.AppendLine();

            // Configuración de cada entidad
            foreach (var modelo in modelos.Values)
            {
                sb.AppendLine($"        modelBuilder.Entity<{modelo.Nombre}>(entity =>");
                sb.AppendLine("        {");
                sb.AppendLine($"            entity.ToTable(\"{modelo.Nombre}s\");");
                sb.AppendLine("            entity.HasKey(e => e.Id);");

                // Propiedades string con max length
                var propiedadesString = modelo.Propiedades?.Where(p => p.Tipo == "string" && p.Nombre != "Id") ?? new List<PropiedadDto>();
                foreach (var prop in propiedadesString)
                {
                    sb.AppendLine($"            entity.Property(e => e.{prop.Nombre}).HasMaxLength(200);");
                }

                sb.AppendLine("        });");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            await File.WriteAllTextAsync(Path.Combine(dataPath, $"{nombreModulo}DbContext.cs"), sb.ToString());
        }

        private async Task GenerarSharedStubs(string path, string nombreModulo)
        {
            var sharedPath = Path.Combine(path, "Shared");
            Directory.CreateDirectory(sharedPath);

            // LoggerService stub
            var logger = $@"namespace {nombreModulo}.Shared;

public class LoggerService
{{
    public void Log(string message)
    {{
        Console.WriteLine($""[{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}] LOG: {{message}}"");
    }}

    public void LogError(string message)
    {{
        Console.WriteLine($""[{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}] ERROR: {{message}}"");
    }}

    public void LogWarning(string message)
    {{
        Console.WriteLine($""[{{DateTime.Now:yyyy-MM-dd HH:mm:ss}}] WARNING: {{message}}"");
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

        private async Task GenerarServiciosConEF(string path, List<ClaseCompletaDto> servicios, string nombreModulo)
        {
            if (!servicios.Any()) return;

            var servicesPath = Path.Combine(path, "Services");
            Directory.CreateDirectory(servicesPath);

            foreach (var servicio in servicios)
            {
                var sb = new StringBuilder();

                // Agregar usings
                sb.AppendLine($"using {nombreModulo}.Models;");
                sb.AppendLine($"using {nombreModulo}.Shared;");
                sb.AppendLine($"using {nombreModulo}.Data;");
                sb.AppendLine("using Microsoft.EntityFrameworkCore;");
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

                // Copiar la clase completa y reemplazar referencias
                var codigoLimpio = servicio.CodigoClaseCompleta
                    .Replace("MonolithPro.Shared", $"{nombreModulo}.Shared")
                    .Replace("MonolithPro.Modules.Users", $"{nombreModulo}.Models")
                    .Replace("MonolithPro.Modules.Orders", $"{nombreModulo}.Models")
                    .Replace("MonolithPro.Modules.Inventory", $"{nombreModulo}.Models")
                    .Replace("MonolithPro.Data", $"{nombreModulo}.Data");

                // Reemplazar referencias al DbContext del monolito por el del microservicio
                codigoLimpio = codigoLimpio.Replace(
                    "MonolithProDbContext",
                    $"{nombreModulo}DbContext"
                );

                // Agregar implementación de la interfaz
                codigoLimpio = Regex.Replace(
                    codigoLimpio,
                    $@"public\s+class\s+{servicio.Nombre}\s*\r?\n",
                    $"public class {servicio.Nombre} : {interfazNombre}\n",
                    RegexOptions.Multiline
                );

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

                // Inyectar dependencias
                var serviciosModulo = controller.DependenciasConstructor
                    .Where(d => d.Contains("Service") && !d.Contains("Logger") && !d.Contains("Email"))
                    .ToList();

                foreach (var dep in serviciosModulo)
                {
                    var depLimpia = dep.Replace("MonolithPro.Modules.Users.", "")
                        .Replace("MonolithPro.Modules.Orders.", "")
                        .Replace("MonolithPro.Modules.Inventory.", "");
                    var nombreCampo = $"_{char.ToLower(depLimpia[0])}{depLimpia.Substring(1)}";
                    sb.AppendLine($"    private readonly I{depLimpia} {nombreCampo};");
                }
                sb.AppendLine();

                // Constructor
                if (serviciosModulo.Any())
                {
                    var parametrosConstructor = string.Join(", ",
                        serviciosModulo.Select(d => {
                            var depLimpia = d.Replace("MonolithPro.Modules.Users.", "")
                                .Replace("MonolithPro.Modules.Orders.", "")
                                .Replace("MonolithPro.Modules.Inventory.", "");
                            return $"I{depLimpia} {char.ToLower(depLimpia[0])}{depLimpia.Substring(1)}";
                        })
                    );

                    sb.AppendLine($"    public {controller.Nombre}({parametrosConstructor})");
                    sb.AppendLine("    {");
                    foreach (var dep in serviciosModulo)
                    {
                        var depLimpia = dep.Replace("MonolithPro.Modules.Users.", "")
                            .Replace("MonolithPro.Modules.Orders.", "")
                            .Replace("MonolithPro.Modules.Inventory.", "");
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

                    var verb = NormalizarAtributoHttp(metodo.HttpVerb);

                    if (!string.IsNullOrEmpty(metodo.Ruta))
                        sb.AppendLine($"    [Http{verb}(\"{metodo.Ruta}\")]");
                    else
                        sb.AppendLine($"    [Http{verb}]");

                    var parametros = string.Join(", ", metodo.Parametros.Select(p =>
                    {
                        var atributo = p.EsFromBody ? "[FromBody] " : (p.EsFromRoute ? "" : "");
                        return $"{atributo}{p.Tipo} {p.Nombre}";
                    }));

                    sb.AppendLine($"    public {metodo.TipoRetorno} {metodo.Nombre}({parametros})");

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

                // *** GENERAR DTOs INLINE AL FINAL DEL ARCHIVO ***
                var dtosGenerados = new HashSet<string>();

                foreach (var metodo in controller.Metodos)
                {
                    foreach (var parametro in metodo.Parametros)
                    {
                        var tipoDato = parametro.Tipo;

                        // Detectar si es un DTO (Request, Response, Dto)
                        if ((tipoDato.EndsWith("Request") || tipoDato.EndsWith("Response") || tipoDato.EndsWith("Dto"))
                            && !dtosGenerados.Contains(tipoDato))
                        {
                            dtosGenerados.Add(tipoDato);

                            // Generar DTO simple
                            sb.AppendLine();
                            sb.AppendLine($"public class {tipoDato}");
                            sb.AppendLine("{");

                            // Para Request típicos de Create/Update, agregar propiedades comunes
                            if (tipoDato.Contains("User"))
                            {
                                sb.AppendLine("    public string Name { get; set; } = string.Empty;");
                                sb.AppendLine("    public string Email { get; set; } = string.Empty;");
                            }
                            else if (tipoDato.Contains("Order"))
                            {
                                sb.AppendLine("    public int UserId { get; set; }");
                                sb.AppendLine("    public int ProductId { get; set; }");
                                sb.AppendLine("    public int Quantity { get; set; }");
                            }
                            else if (tipoDato.Contains("Product") || tipoDato.Contains("Inventory"))
                            {
                                sb.AppendLine("    public int Quantity { get; set; }");
                            }
                            else
                            {
                                // DTO genérico
                                sb.AppendLine("    // TODO: Add properties based on your needs");
                            }

                            sb.AppendLine("}");
                        }
                    }
                }

                await File.WriteAllTextAsync(
                    Path.Combine(controllersPath, $"{controller.Nombre}.cs"),
                    sb.ToString()
                );
            }
        }

        private async Task GenerarProgramCsConEF(string path, List<ClaseCompletaDto> servicios, List<ClaseCompletaDto> controllers, string nombreModulo)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"using {nombreModulo}.Services;");
            sb.AppendLine($"using {nombreModulo}.Shared;");
            sb.AppendLine($"using {nombreModulo}.Data;");
            sb.AppendLine("using Microsoft.EntityFrameworkCore;");
            sb.AppendLine();
            sb.AppendLine("var builder = WebApplication.CreateBuilder(args);");
            sb.AppendLine();
            sb.AppendLine("// *** CONFIGURACIÓN DE ENTITY FRAMEWORK CORE ***");
            sb.AppendLine("// Connection string desde variable de entorno o appsettings.json");
            sb.AppendLine("var connectionString = Environment.GetEnvironmentVariable(\"ConnectionStrings__DefaultConnection\")");
            sb.AppendLine("    ?? builder.Configuration.GetConnectionString(\"DefaultConnection\");");
            sb.AppendLine();
            sb.AppendLine($"builder.Services.AddDbContext<{nombreModulo}DbContext>(options =>");
            sb.AppendLine("    options.UseSqlServer(");
            sb.AppendLine("        connectionString,");
            sb.AppendLine("        sqlOptions => sqlOptions.EnableRetryOnFailure(");
            sb.AppendLine("            maxRetryCount: 5,");
            sb.AppendLine("            maxRetryDelay: TimeSpan.FromSeconds(30),");
            sb.AppendLine("            errorNumbersToAdd: null");
            sb.AppendLine("        )");
            sb.AppendLine("    )");
            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine("// Registrar servicios compartidos");
            sb.AppendLine("builder.Services.AddSingleton<LoggerService>();");
            sb.AppendLine("builder.Services.AddSingleton<EmailService>();");
            sb.AppendLine();
            sb.AppendLine("// Registrar servicios del módulo");
            sb.AppendLine("builder.Services.AddControllers();");
            sb.AppendLine("builder.Services.AddEndpointsApiExplorer();");
            sb.AppendLine("builder.Services.AddSwaggerGen();");
            sb.AppendLine();

            // Registrar servicios del módulo como Scoped
            foreach (var servicio in servicios)
            {
                sb.AppendLine($"builder.Services.AddScoped<I{servicio.Nombre}, {servicio.Nombre}>();");
            }

            sb.AppendLine();
            sb.AppendLine("var app = builder.Build();");
            sb.AppendLine();
            sb.AppendLine("// *** APLICAR MIGRACIONES AUTOMÁTICAMENTE ***");
            sb.AppendLine("using (var scope = app.Services.CreateScope())");
            sb.AppendLine("{");
            sb.AppendLine($"    var dbContext = scope.ServiceProvider.GetRequiredService<{nombreModulo}DbContext>();");
            sb.AppendLine("    var logger = scope.ServiceProvider.GetRequiredService<LoggerService>();");
            sb.AppendLine("    ");
            sb.AppendLine("    try");
            sb.AppendLine("    {");
            sb.AppendLine("        logger.Log(\"Testing database connection...\");");
            sb.AppendLine("        ");
            sb.AppendLine("        if (dbContext.Database.CanConnect())");
            sb.AppendLine("        {");
            sb.AppendLine("            logger.Log(\"✅ Database connection successful\");");
            sb.AppendLine("            ");
            sb.AppendLine("            if (dbContext.Database.GetPendingMigrations().Any())");
            sb.AppendLine("            {");
            sb.AppendLine("                logger.Log(\"Applying migrations...\");");
            sb.AppendLine("                dbContext.Database.Migrate();");
            sb.AppendLine("                logger.Log(\"✅ Migrations applied\");");
            sb.AppendLine("            }");
            sb.AppendLine("            else");
            sb.AppendLine("            {");
            sb.AppendLine("                logger.Log(\"✅ Database up to date\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            sb.AppendLine("            logger.LogError(\"❌ Cannot connect to database\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    catch (Exception ex)");
            sb.AppendLine("    {");
            sb.AppendLine("        logger.LogError($\"❌ Database error: {ex.Message}\");");
            sb.AppendLine("    }");
            sb.AppendLine("}");
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

        private async Task GenerarAppSettings(string path)
        {
            // appsettings.json para Docker (con host.docker.internal)
            var appSettings = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning"",
      ""Microsoft.EntityFrameworkCore.Database.Command"": ""Information""
    }
  },
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=host.docker.internal\\SQLEXPRESS;Database=MonolithProDB;User Id=sa;Password=Pg1_Database;TrustServerCertificate=True;Encrypt=False;""
  }
}";

            await File.WriteAllTextAsync(Path.Combine(path, "appsettings.json"), appSettings);

            // appsettings.Development.json para desarrollo local
            var appSettingsDev = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning"",
      ""Microsoft.EntityFrameworkCore.Database.Command"": ""Information""
    }
  },
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=.\\SQLEXPRESS;Database=MonolithProDB;User Id=sa;Password=Pg1_Database;TrustServerCertificate=True;Encrypt=False;""
  }
}";

            await File.WriteAllTextAsync(Path.Combine(path, "appsettings.Development.json"), appSettingsDev);
        }

        private async Task GenerarDockerfile(string path, string nombreModulo)
        {
            var dockerfile = $@"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""{nombreModulo}.csproj"", ""./""]
RUN dotnet restore ""{nombreModulo}.csproj""
COPY . .
RUN dotnet build ""{nombreModulo}.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""{nombreModulo}.csproj"" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{nombreModulo}.dll""]";

            await File.WriteAllTextAsync(Path.Combine(path, "Dockerfile"), dockerfile);
        }

        private string NormalizarAtributoHttp(string httpVerb)
        {
            var verb = httpVerb
                .Replace("Http", "", StringComparison.OrdinalIgnoreCase)
                .Replace("_", "")
                .Trim();

            if (string.IsNullOrEmpty(verb)) return "Get";
            return char.ToUpper(verb[0]) + verb.Substring(1).ToLower();
        }
    }
}