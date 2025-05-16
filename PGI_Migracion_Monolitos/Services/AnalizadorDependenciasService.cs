using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PGI_Migracion_Monolitos.Infrastructure;
using PGI_Migracion_Monolitos.Interfaces.Repository;
using PGI_Migracion_Monolitos.Interfaces.Services;
using PGI_Migracion_Monolitos.Models;
using System.IO.Compression;

namespace PGI_Migracion_Monolitos.Services
{
    public class AnalizadorDependenciasService : IAnalizadorDependenciasService
    {
        private readonly IDependenciaRepository _repo;

        public AnalizadorDependenciasService(IDependenciaRepository repo)
        {
            _repo = repo;
        }

        public async Task AnalizarDependenciasAsync(string rutaProyecto)
        {
            StartupRoslyn.RegistrarMSBuild();

            string rutaRealProyecto = rutaProyecto;

            if (rutaProyecto.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var carpetaTemporal = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                ZipFile.ExtractToDirectory(rutaProyecto, carpetaTemporal);

                var archivoProyecto = Directory
                    .GetFiles(carpetaTemporal, "*.csproj", SearchOption.AllDirectories)
                    .FirstOrDefault()
                    ?? Directory.GetFiles(carpetaTemporal, "*.sln", SearchOption.AllDirectories).FirstOrDefault();

                if (archivoProyecto == null)
                    throw new FileNotFoundException("No se encontró un archivo .csproj o .sln dentro del ZIP.");

                rutaRealProyecto = archivoProyecto;
            }

            using var workspace = MSBuildWorkspace.Create();
            var proyecto = await workspace.OpenProjectAsync(rutaRealProyecto);
            var compilacion = await proyecto.GetCompilationAsync();

            var dependencias = new List<DependenciaModel>();

            foreach (var documento in proyecto.Documents)
            {
                var syntaxTree = await documento.GetSyntaxTreeAsync();
                var root = await syntaxTree.GetRootAsync();
                var semanticModel = compilacion.GetSemanticModel(syntaxTree);
                var clases = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                var nombreProyecto = Path.GetFileNameWithoutExtension(rutaRealProyecto);

                foreach (var clase in clases)
                {
                    var origen = clase.Identifier.Text;
                    var nsOrigen = semanticModel.GetDeclaredSymbol(clase)?.ContainingNamespace?.ToDisplayString();

                    var identificadores = clase.DescendantNodes().OfType<IdentifierNameSyntax>();

                    foreach (var id in identificadores)
                    {
                        var simbolo = semanticModel.GetSymbolInfo(id).Symbol;

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

            await _repo.GuardarDependenciasAsync(dependencias);
        }
    }
}
