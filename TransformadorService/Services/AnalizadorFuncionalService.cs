using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using TransformadorService.DTOs;
using System.IO.Compression;

namespace TransformadorService.Services
{
    public class AnalizadorFuncionalService : IAnalizadorFuncionalService
    {
        private readonly IConfiguration _configuration;

        public AnalizadorFuncionalService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<ClaseCompletaDto>> AnalizarModuloCompletoAsync(string nombreProyecto, string nombreModulo)
        {
            var uploadsPath = _configuration["Paths:UploadsFolder"] ?? "../Uploads";
            var zipPath = Path.Combine(uploadsPath, nombreProyecto, $"{nombreProyecto}.zip");

            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"No se encontró el archivo {zipPath}");

            var tempPath = Path.Combine(Path.GetTempPath(), $"Analisis_{nombreProyecto}_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);

            ZipFile.ExtractToDirectory(zipPath, tempPath);

            var modulesRoot = Directory.GetDirectories(tempPath, "Modules", SearchOption.AllDirectories).FirstOrDefault();
            if (modulesRoot == null)
                throw new DirectoryNotFoundException("No se encontró la carpeta 'Modules'");

            var moduloPath = Directory.GetDirectories(modulesRoot)
                .FirstOrDefault(d => d.EndsWith(nombreModulo, StringComparison.OrdinalIgnoreCase));

            if (moduloPath == null)
                throw new DirectoryNotFoundException($"No se encontró el módulo '{nombreModulo}'");

            var clasesCompletas = new List<ClaseCompletaDto>();
            var modelosEncontrados = new Dictionary<string, ModeloDto>();

            // Analizar todos los archivos .cs del módulo
            foreach (var file in Directory.GetFiles(moduloPath, "*.cs", SearchOption.AllDirectories))
            {
                var codigo = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(codigo);
                var root = tree.GetCompilationUnitRoot();

                // Extraer usings
                var usings = root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(u => u.Name.ToString())
                    .ToList();

                var clases = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var clase in clases)
                {
                    var esController = clase.Identifier.Text.EndsWith("Controller");
                    var esService = clase.Identifier.Text.EndsWith("Service");
                    
                    // Si es modelo (no es Controller ni Service)
                    if (!esController && !esService)
                    {
                        var modelo = ExtraerModelo(clase, root);
                        if (!modelosEncontrados.ContainsKey(modelo.Nombre))
                        {
                            modelosEncontrados[modelo.Nombre] = modelo;
                        }
                        continue;
                    }

                    var claseDto = new ClaseCompletaDto
                    {
                        Nombre = clase.Identifier.Text,
                        Namespace = root.DescendantNodes()
                            .OfType<NamespaceDeclarationSyntax>()
                            .FirstOrDefault()?.Name.ToString() 
                            ?? root.DescendantNodes()
                                .OfType<FileScopedNamespaceDeclarationSyntax>()
                                .FirstOrDefault()?.Name.ToString(),
                        Usings = usings,
                        Metodos = new List<MetodoCompletoDto>(),
                        CodigoClaseCompleta = clase.ToFullString()
                    };

                    // Extraer dependencias del constructor
                    var constructor = clase.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                    if (constructor != null)
                    {
                        claseDto.DependenciasConstructor = constructor.ParameterList.Parameters
                            .Select(p => p.Type.ToString())
                            .ToList();
                    }

                    // Analizar métodos (solo controllers por ahora)
                    if (esController)
                    {
                        var metodos = clase.DescendantNodes().OfType<MethodDeclarationSyntax>();

                        foreach (var metodo in metodos)
                        {
                            var metodoDto = ExtraerMetodoCompleto(metodo, clase);
                            if (metodoDto != null)
                            {
                                claseDto.Metodos.Add(metodoDto);
                            }
                        }
                    }
                    // Si es Service, extraer toda la clase
                    else if (esService)
                    {
                        var metodos = clase.DescendantNodes().OfType<MethodDeclarationSyntax>();
                        foreach (var metodo in metodos)
                        {
                            var metodoDto = new MetodoCompletoDto
                            {
                                Nombre = metodo.Identifier.Text,
                                CodigoCompleto = metodo.ToFullString(),
                                TipoRetorno = metodo.ReturnType.ToString(),
                                Parametros = metodo.ParameterList.Parameters
                                    .Select(p => new ParametroDto
                                    {
                                        Nombre = p.Identifier.Text,
                                        Tipo = p.Type.ToString()
                                    })
                                    .ToList()
                            };
                            claseDto.Metodos.Add(metodoDto);
                        }
                    }

                    clasesCompletas.Add(claseDto);
                }
            }

            // Agregar modelos encontrados a una clase especial
            if (modelosEncontrados.Any())
            {
                var claseModelos = new ClaseCompletaDto
                {
                    Nombre = "_Modelos",
                    Namespace = $"{nombreProyecto}.Modules.{nombreModulo}",
                    Metodos = new List<MetodoCompletoDto>()
                };

                // Guardar los modelos para uso posterior
                foreach (var modelo in modelosEncontrados.Values)
                {
                    claseModelos.Usings.Add($"Modelo:{modelo.Nombre}:{modelo.CodigoCompleto}");
                }

                clasesCompletas.Add(claseModelos);
            }

            Directory.Delete(tempPath, true);
            return clasesCompletas;
        }

        private MetodoCompletoDto ExtraerMetodoCompleto(MethodDeclarationSyntax metodo, ClassDeclarationSyntax clase)
        {
            var atributos = metodo.AttributeLists
                .SelectMany(a => a.Attributes)
                .Select(a => a.Name.ToString())
                .ToList();

            var httpVerb = atributos.FirstOrDefault(a =>
                a.StartsWith("Http", StringComparison.OrdinalIgnoreCase));

            if (httpVerb == null)
                return null; // Solo procesamos métodos con atributos HTTP

            var ruta = metodo.AttributeLists
                .SelectMany(a => a.Attributes)
                .Select(a => a.ArgumentList?.Arguments.FirstOrDefault()?.ToString()?.Trim('"'))
                .FirstOrDefault(r => r != null);

            var metodoDto = new MetodoCompletoDto
            {
                Nombre = metodo.Identifier.Text,
                HttpVerb = httpVerb,
                Ruta = ruta,
                CodigoCompleto = metodo.Body?.ToFullString() ?? metodo.ExpressionBody?.ToFullString(),
                TipoRetorno = metodo.ReturnType.ToString(),
                Parametros = new List<ParametroDto>()
            };

            // Extraer parámetros
            foreach (var parametro in metodo.ParameterList.Parameters)
            {
                var paramDto = new ParametroDto
                {
                    Nombre = parametro.Identifier.Text,
                    Tipo = parametro.Type.ToString()
                };

                // Detectar atributos [FromBody], [FromRoute], etc.
                var atributosParam = parametro.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Select(a => a.Name.ToString())
                    .ToList();

                paramDto.EsFromBody = atributosParam.Any(a => a.Contains("FromBody"));
                paramDto.EsFromRoute = atributosParam.Any(a => a.Contains("FromRoute")) 
                    || ruta?.Contains($"{{{parametro.Identifier.Text}}}") == true;

                metodoDto.Parametros.Add(paramDto);
            }

            // Detectar dependencias usadas en el método
            var invocaciones = metodo.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocacion in invocaciones)
            {
                var expresion = invocacion.Expression.ToString();
                if (expresion.StartsWith("_"))
                {
                    var dependencia = expresion.Split('.').FirstOrDefault();
                    if (dependencia != null && !metodoDto.DependenciasUsadas.Contains(dependencia))
                    {
                        metodoDto.DependenciasUsadas.Add(dependencia);
                    }
                }
            }

            return metodoDto;
        }

        private ModeloDto ExtraerModelo(ClassDeclarationSyntax clase, CompilationUnitSyntax root)
        {
            var modelo = new ModeloDto
            {
                Nombre = clase.Identifier.Text,
                Namespace = root.DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault()?.Name.ToString()
                    ?? root.DescendantNodes()
                        .OfType<FileScopedNamespaceDeclarationSyntax>()
                        .FirstOrDefault()?.Name.ToString(),
                CodigoCompleto = clase.ToFullString(),
                Propiedades = new List<PropiedadDto>()
            };

            // Extraer propiedades
            var propiedades = clase.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var prop in propiedades)
            {
                modelo.Propiedades.Add(new PropiedadDto
                {
                    Nombre = prop.Identifier.Text,
                    Tipo = prop.Type.ToString(),
                    TieneGetter = prop.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false,
                    TieneSetter = prop.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false
                });
            }

            return modelo;
        }
    }
}