using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RefactorizacionService.DTOs;
using System.Text;

namespace RefactorizacionService.Services
{
    public class RefactorizadorCodigoService : IRefactorizadorCodigoService
    {
        private readonly IConfiguration _configuration;
        private readonly IRefactorizacionHistorialService _historialService;

        public RefactorizadorCodigoService(
            IConfiguration configuration,
            IRefactorizacionHistorialService historialService)
        {
            _configuration = configuration;
            _historialService = historialService;
        }

        public async Task<AplicarRefactorizacionResponseDto> RefactorizarModuloAsync(
            AplicarRefactorizacionRequestDto request)
        {
            var monolithosFolder = _configuration["Paths:MonolithosFolder"];
            var rutaProyecto = Path.Combine(monolithosFolder, request.NombreProyecto);
            var rutaModulo = Path.Combine(rutaProyecto, "Modules", request.ModuloARefactorizar);

            if (!Directory.Exists(rutaModulo))
                throw new DirectoryNotFoundException($"Módulo {request.ModuloARefactorizar} no encontrado");

            var historial = await _historialService.CrearHistorialAsync(
                request.NombreProyecto,
                request.ModuloARefactorizar);

            var cambiosRealizados = new List<CambioRealizadoDto>();
            var archivosModificados = new List<ArchivoModificadoDto>();
            var todosLosDiffs = new StringBuilder();

            try
            {
                var archivosCs = Directory.GetFiles(rutaModulo, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                    .ToList();

                foreach (var archivo in archivosCs)
                {
                    var codigoOriginal = await File.ReadAllTextAsync(archivo);
                    var codigoRefactorizado = codigoOriginal;
                    var cambiosEnArchivo = 0;

                    foreach (var dependencia in request.Dependencias)
                    {
                        codigoRefactorizado = await ReemplazarInvocacionDirectaPorHttpAsync(
                            codigoRefactorizado,
                            dependencia.ServicioOriginal,
                            dependencia.UrlMicroservicio);
                    }

                    if (codigoOriginal != codigoRefactorizado)
                    {
                        var diff = await GenerarDiffAsync(codigoOriginal, codigoRefactorizado);
                        todosLosDiffs.AppendLine($"=== {Path.GetFileName(archivo)} ===");
                        todosLosDiffs.AppendLine(diff);
                        todosLosDiffs.AppendLine();

                        cambiosRealizados.Add(new CambioRealizadoDto
                        {
                            NombreArchivo = Path.GetFileName(archivo),
                            NombreClase = ExtraerNombreClase(codigoOriginal),
                            NombreMetodo = "Multiple",
                            CodigoAntes = codigoOriginal.Substring(0, Math.Min(500, codigoOriginal.Length)),
                            CodigoDespues = codigoRefactorizado.Substring(0, Math.Min(500, codigoRefactorizado.Length)),
                            LineaInicio = 1,
                            LineaFin = codigoRefactorizado.Split('\n').Length
                        });

                        cambiosEnArchivo++;

                        if (!request.GenerarSoloPreview)
                        {
                            await GuardarCodigoRefactorizadoAsync(archivo, codigoRefactorizado);
                        }

                        archivosModificados.Add(new ArchivoModificadoDto
                        {
                            RutaArchivo = archivo,
                            NombreArchivo = Path.GetFileName(archivo),
                            CantidadCambios = cambiosEnArchivo
                        });
                    }
                }

                var estado = cambiosRealizados.Any() ? "Completado" : "Sin cambios";
                await _historialService.ActualizarHistorialAsync(
                    historial.Id,
                    estado,
                    todosLosDiffs.ToString());

                return new AplicarRefactorizacionResponseDto
                {
                    Exitoso = true,
                    Mensaje = request.GenerarSoloPreview
                        ? "Preview generado exitosamente"
                        : $"Refactorización completada. {cambiosRealizados.Count} cambios realizados.",
                    RefactorizacionHistorialId = historial.Id,
                    CambiosRealizados = cambiosRealizados,
                    CodigoDiff = todosLosDiffs.ToString(),
                    ArchivosModificados = archivosModificados,
                    RutaCodigoRefactorizado = request.GenerarSoloPreview ? null : rutaModulo
                };
            }
            catch (Exception ex)
            {
                await _historialService.ActualizarHistorialAsync(
                    historial.Id,
                    "Error",
                    null,
                    ex.Message);

                return new AplicarRefactorizacionResponseDto
                {
                    Exitoso = false,
                    Mensaje = $"Error durante la refactorización: {ex.Message}",
                    RefactorizacionHistorialId = historial.Id,
                    CambiosRealizados = new List<CambioRealizadoDto>(),
                    ArchivosModificados = new List<ArchivoModificadoDto>()
                };
            }
        }

        public async Task<string> ReemplazarInvocacionDirectaPorHttpAsync(
            string codigoOriginal,
            string servicioOriginal,
            string urlMicroservicio)
        {
            var arbol = CSharpSyntaxTree.ParseText(codigoOriginal);
            var raiz = await arbol.GetRootAsync();

            var clases = raiz.DescendantNodes().OfType<ClassDeclarationSyntax>();

            var nuevoRaiz = raiz;

            foreach (var clase in clases)
            {
                var campos = clase.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .Where(f => f.Declaration.Type.ToString().Contains(servicioOriginal));

                foreach (var campo in campos)
                {
                    var nombreCampo = campo.Declaration.Variables.First().Identifier.Text;

                    var metodos = clase.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var metodo in metodos)
                    {
                        var invocaciones = metodo.DescendantNodes().OfType<InvocationExpressionSyntax>()
                            .Where(inv => inv.Expression.ToString().StartsWith(nombreCampo));

                        foreach (var invocacion in invocaciones)
                        {
                            var nuevoMetodo = await ConvertirInvocacionAHttpAsync(
                                metodo,
                                invocacion,
                                nombreCampo,
                                urlMicroservicio);

                            nuevoRaiz = nuevoRaiz.ReplaceNode(metodo, nuevoMetodo);
                        }
                    }
                }

                var nuevoClase = await AgregarHttpClientAlConstructorAsync(clase, servicioOriginal);
                nuevoRaiz = nuevoRaiz.ReplaceNode(clase, nuevoClase);
            }

            return nuevoRaiz.ToFullString();
        }

        private async Task<MethodDeclarationSyntax> ConvertirInvocacionAHttpAsync(
            MethodDeclarationSyntax metodo,
            InvocationExpressionSyntax invocacion,
            string nombreCampo,
            string urlMicroservicio)
        {
            var expresionOriginal = invocacion.Expression.ToString();
            var argumentos = invocacion.ArgumentList.Arguments;

            var nuevoMetodo = metodo;

            if (!metodo.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
            {
                nuevoMetodo = metodo.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            }

            var tipoRetorno = metodo.ReturnType.ToString();
            if (!tipoRetorno.StartsWith("Task"))
            {
                var nuevoTipoRetorno = SyntaxFactory.ParseTypeName($"Task<{tipoRetorno}>");
                nuevoMetodo = nuevoMetodo.WithReturnType(nuevoTipoRetorno);
            }

            return await Task.FromResult(nuevoMetodo);
        }

        private async Task<ClassDeclarationSyntax> AgregarHttpClientAlConstructorAsync(
            ClassDeclarationSyntax clase,
            string servicioOriginal)
        {
            var constructores = clase.DescendantNodes().OfType<ConstructorDeclarationSyntax>();

            if (!constructores.Any())
            {
                return clase;
            }

            var constructor = constructores.First();

            var tieneHttpClient = constructor.ParameterList.Parameters
                .Any(p => p.Type?.ToString() == "IHttpClientFactory");

            if (!tieneHttpClient)
            {
                var nuevoParametro = SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier("httpClientFactory"))
                    .WithType(SyntaxFactory.ParseTypeName("IHttpClientFactory"));

                var nuevosParametros = constructor.ParameterList.AddParameters(nuevoParametro);
                var nuevoConstructor = constructor.WithParameterList(nuevosParametros);

                clase = clase.ReplaceNode(constructor, nuevoConstructor);
            }

            return await Task.FromResult(clase);
        }

        public async Task<string> GenerarDiffAsync(string codigoOriginal, string codigoRefactorizado)
        {
            var lineasOriginales = codigoOriginal.Split('\n');
            var lineasRefactorizadas = codigoRefactorizado.Split('\n');

            var diff = new StringBuilder();
            diff.AppendLine("--- Original");
            diff.AppendLine("+++ Refactorizado");
            diff.AppendLine();

            var maxLineas = Math.Max(lineasOriginales.Length, lineasRefactorizadas.Length);

            for (int i = 0; i < maxLineas; i++)
            {
                var lineaOriginal = i < lineasOriginales.Length ? lineasOriginales[i] : "";
                var lineaRefactorizada = i < lineasRefactorizadas.Length ? lineasRefactorizadas[i] : "";

                if (lineaOriginal != lineaRefactorizada)
                {
                    diff.AppendLine($"- {lineaOriginal}");
                    diff.AppendLine($"+ {lineaRefactorizada}");
                }
            }

            return await Task.FromResult(diff.ToString());
        }

        public async Task GuardarCodigoRefactorizadoAsync(string rutaDestino, string codigoRefactorizado)
        {
            await File.WriteAllTextAsync(rutaDestino, codigoRefactorizado);
        }

        private string ExtraerNombreClase(string codigo)
        {
            var arbol = CSharpSyntaxTree.ParseText(codigo);
            var raiz = arbol.GetRoot();
            var clase = raiz.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return clase?.Identifier.Text ?? "Unknown";
        }
    }
}