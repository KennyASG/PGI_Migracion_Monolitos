using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RefactorizacionService.DTOs;
using System.Text.RegularExpressions;

namespace RefactorizacionService.Services
{
    public class AnalizadorDependenciasService : IAnalizadorDependenciasService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiciosMigradosService _serviciosMigradosService;
        private readonly ILogger<AnalizadorDependenciasService> _logger;

        public AnalizadorDependenciasService(
            IConfiguration configuration,
            IServiciosMigradosService serviciosMigradosService,
            ILogger<AnalizadorDependenciasService> logger)
        {
            _configuration = configuration;
            _serviciosMigradosService = serviciosMigradosService;
            _logger = logger;
        }

        public async Task<AnalizarRefactorizacionResponseDto> AnalizarDependenciasAsync(
            string nombreProyecto, 
            string moduloARefactorizar)
        {
            // Buscar en la carpeta local del RefactorizacionService
            var localMonolithosFolder = Path.Combine(Directory.GetCurrentDirectory(), "Monolithos");
            var rutaProyecto = Path.Combine(localMonolithosFolder, nombreProyecto,nombreProyecto);

            if (!Directory.Exists(rutaProyecto))
                throw new DirectoryNotFoundException($"Proyecto {nombreProyecto} no encontrado en {localMonolithosFolder}. Por favor descomprímalo manualmente.");

            var rutaModulo = Path.Combine(rutaProyecto, "Modules", moduloARefactorizar);
            
            if (!Directory.Exists(rutaModulo))
                throw new DirectoryNotFoundException($"Módulo {moduloARefactorizar} no encontrado en {rutaProyecto}");

            var serviciosMigrados = await _serviciosMigradosService.ObtenerServiciosMigradosAsync(nombreProyecto);
            var nombreServiciosMigrados = serviciosMigrados.Select(s => $"{s.NombreModulo}Service").ToList();

            var todasLasDependencias = new List<DependenciaDetectadaDto>();

            var archivosCs = Directory.GetFiles(rutaModulo, "*.cs", SearchOption.AllDirectories);

            foreach (var archivo in archivosCs)
            {
                var dependencias = await DetectarInvocacionesServiciosAsync(archivo, nombreServiciosMigrados);
                todasLasDependencias.AddRange(dependencias);
            }

            var nivelComplejidad = await CalcularNivelComplejidadAsync(todasLasDependencias.Count);

            var recomendaciones = GenerarRecomendaciones(todasLasDependencias, serviciosMigrados);

            return new AnalizarRefactorizacionResponseDto
            {
                NombreProyecto = nombreProyecto,
                ModuloAnalizado = moduloARefactorizar,
                DependenciasDetectadas = todasLasDependencias,
                TotalDependencias = todasLasDependencias.Count,
                NivelComplejidad = nivelComplejidad,
                ServiciosMigradosDisponibles = serviciosMigrados.Select(s => s.NombreModulo).ToList(),
                Recomendaciones = recomendaciones
            };
        }

        public async Task<List<DependenciaDetectadaDto>> DetectarInvocacionesServiciosAsync(
            string rutaArchivo, 
            List<string> serviciosMigrados)
        {
            var dependencias = new List<DependenciaDetectadaDto>();

            var codigoFuente = await File.ReadAllTextAsync(rutaArchivo);
            var arbolSintactico = CSharpSyntaxTree.ParseText(codigoFuente);
            var raiz = await arbolSintactico.GetRootAsync();

            var nombreArchivo = Path.GetFileName(rutaArchivo);

            var clases = raiz.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var clase in clases)
            {
                var nombreClase = clase.Identifier.Text;

                var campos = clase.DescendantNodes().OfType<FieldDeclarationSyntax>();
                
                foreach (var campo in campos)
                {
                    var tipoCampo = campo.Declaration.Type.ToString();
                    
                    if (serviciosMigrados.Any(s => tipoCampo.Contains(s)))
                    {
                        var servicioDetectado = serviciosMigrados.First(s => tipoCampo.Contains(s));
                        
                        var metodos = clase.DescendantNodes().OfType<MethodDeclarationSyntax>();
                        
                        foreach (var metodo in metodos)
                        {
                            var invocaciones = metodo.DescendantNodes().OfType<InvocationExpressionSyntax>();
                            
                            foreach (var invocacion in invocaciones)
                            {
                                var textoInvocacion = invocacion.ToString();
                                
                                if (textoInvocacion.Contains(campo.Declaration.Variables.First().Identifier.Text))
                                {
                                    var lineaInvocacion = arbolSintactico.GetLineSpan(invocacion.Span).StartLinePosition.Line + 1;
                                    
                                    dependencias.Add(new DependenciaDetectadaDto
                                    {
                                        NombreClase = nombreClase,
                                        NombreMetodo = metodo.Identifier.Text,
                                        ServicioDependiente = servicioDetectado,
                                        TipoInvocacion = "Inyectado",
                                        ServicioYaMigrado = true,
                                        LineasCodigo = new List<string> { textoInvocacion }
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return dependencias;
        }

        public async Task<string> CalcularNivelComplejidadAsync(int totalDependencias)
        {
            return await Task.FromResult(totalDependencias switch
            {
                0 => "Ninguna",
                >= 1 and <= 5 => "Baja",
                >= 6 and <= 15 => "Media",
                _ => "Alta"
            });
        }

        private List<string> GenerarRecomendaciones(
            List<DependenciaDetectadaDto> dependencias, 
            List<Models.ServicioMigrado> serviciosMigrados)
        {
            var recomendaciones = new List<string>();

            var serviciosConDependencias = dependencias
                .Select(d => d.ServicioDependiente)
                .Distinct()
                .ToList();

            foreach (var servicio in serviciosConDependencias)
            {
                var servicioMigrado = serviciosMigrados.FirstOrDefault(s => $"{s.NombreModulo}Service" == servicio);
                
                if (servicioMigrado != null)
                {
                    recomendaciones.Add($"Refactorizar llamadas a {servicio} para usar HTTP: {servicioMigrado.UrlMicroservicio}");
                }
                else
                {
                    recomendaciones.Add($"El servicio {servicio} aún no ha sido migrado. Considere migrarlo primero.");
                }
            }

            if (dependencias.Count > 15)
            {
                recomendaciones.Add("Alto nivel de acoplamiento detectado. Considere dividir el módulo en componentes más pequeños.");
            }

            if (!dependencias.Any())
            {
                recomendaciones.Add("No se detectaron dependencias a servicios migrados. El módulo puede extraerse directamente.");
            }

            return recomendaciones;
        }
    }
}