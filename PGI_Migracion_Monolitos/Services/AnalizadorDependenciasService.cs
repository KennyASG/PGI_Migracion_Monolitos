using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using PGI_Migracion_Monolitos.Interfaces.Repository;
using PGI_Migracion_Monolitos.Interfaces.Services;
using PGI_Migracion_Monolitos.Models;
using PGI_Migracion_Monolitos.Infrastructure;

namespace PGI_Migracion_Monolitos.Services
{
    public class AnalizadorDependenciasService : IAnalizadorDependenciasService
    {
        private readonly IDependenciaRepository _repo;

        public AnalizadorDependenciasService(IDependenciaRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Analiza todo el proyecto (ruta o ZIP) con Roslyn y guarda las dependencias en BD.
        /// </summary>
        public async Task AnalizarDependenciasAsync(string rutaProyecto)
        {
            // Registra MSBuild para poder cargar el Workspace
            StartupRoslyn.RegistrarMSBuild();

            var rutaReal = rutaProyecto;
            if (rutaProyecto.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                // Descomprime ZIP a carpeta temporal
                var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                ZipFile.ExtractToDirectory(rutaProyecto, tmp);

                // Busca el .csproj o .sln en la carpeta
                rutaReal = Directory
                    .GetFiles(tmp, "*.csproj", SearchOption.AllDirectories)
                    .FirstOrDefault()
                    ?? Directory.GetFiles(tmp, "*.sln", SearchOption.AllDirectories).FirstOrDefault();

                if (rutaReal == null)
                    throw new FileNotFoundException("No se encontró .csproj o .sln dentro del ZIP.");
            }

            // Abre el proyecto en Roslyn
            using var workspace = MSBuildWorkspace.Create();
            var proyecto = await workspace.OpenProjectAsync(rutaReal);
            var compilacion = await proyecto.GetCompilationAsync();

            var dependencias = new List<DependenciaModel>();
            var nombreProyecto = Path.GetFileNameWithoutExtension(rutaReal);

            foreach (var documento in proyecto.Documents)
            {
                var tree = await documento.GetSyntaxTreeAsync();
                var root = await tree.GetRootAsync();
                var model = compilacion.GetSemanticModel(tree);

                // Recorre todas las clases
                var clases = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (var clase in clases)
                {
                    var origen = clase.Identifier.Text;
                    var nsOrigen = model.GetDeclaredSymbol(clase)?.ContainingNamespace?.ToDisplayString();

                    // Recorre todos los identificadores de tipo dentro de la clase
                    var ids = clase.DescendantNodes().OfType<IdentifierNameSyntax>();
                    foreach (var id in ids)
                    {
                        var simbolo = model.GetSymbolInfo(id).Symbol;
                        if (simbolo is ITypeSymbol tipo)
                        {
                            var destino = tipo.Name;
                            var nsDestino = tipo.ContainingNamespace?.ToString();

                            if (destino != origen)
                            {
                                dependencias.Add(new DependenciaModel
                                {
                                    ClaseOrigen = origen,
                                    ClaseDependencia = destino,
                                    NamespaceOrigen = nsOrigen,
                                    NamespaceDependencia = nsDestino,
                                    FechaAnalisis = DateTime.UtcNow,
                                    ProyectoAnalizado = nombreProyecto
                                });
                            }
                        }
                    }
                }
            }

            // Guarda todas las filas en la base de datos
            await _repo.GuardarDependenciasAsync(dependencias);
        }

        /// <summary>
        /// Construye y retorna el grafo listo para el frontend: nodos y aristas únicas y filtradas.
        /// </summary>
        public async Task<GrafoDto> ObtenerGrafoAsync(string proyecto)
        {
            // 1) Trae todas las dependencias almacenadas
            var rows = await _repo.ObtenerPorProyectoAsync(proyecto);

            // 2) Filtra filas inválidas (origen o destino vacío)
            var validRows = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.ClaseOrigen)
                         && !string.IsNullOrWhiteSpace(r.ClaseDependencia))
                .ToList();

            // 3) Construye lista de IDs únicos (nodos)
            var allIds = validRows
                .Select(r => r.ClaseOrigen)
                .Concat(validRows.Select(r => r.ClaseDependencia))
                .Distinct();

            var nodes = allIds
                .Select(id => new NodoDto { Id = id, Label = id })
                .ToList();

            // 4) Construye lista de enlaces únicos (links)
            var links = validRows
                .Select(r => new { source = r.ClaseOrigen, target = r.ClaseDependencia })
                .Distinct()
                .Select(e => new EnlaceDto { Source = e.source, Target = e.target })
                .ToList();

            return new GrafoDto
            {
                Nodes = nodes,
                Links = links
            };
        }
    }

    /// <summary>
    /// DTO que representa todo el grafo para el frontend.
    /// </summary>
    public class GrafoDto
    {
        public List<NodoDto> Nodes { get; set; }
        public List<EnlaceDto> Links { get; set; }
    }

    /// <summary>
    /// DTO para cada nodo del grafo.
    /// </summary>
    public class NodoDto
    {
        public string Id { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// DTO para cada arista del grafo.
    /// </summary>
    public class EnlaceDto
    {
        public string Source { get; set; }
        public string Target { get; set; }
    }
}
