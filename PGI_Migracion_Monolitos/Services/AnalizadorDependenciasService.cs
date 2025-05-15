using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PGI_Migracion_Monolitos.Infrastructure;
using PGI_Migracion_Monolitos.Interfaces.Repository;
using PGI_Migracion_Monolitos.Interfaces.Services;
using PGI_Migracion_Monolitos.Models;


namespace PGI_Migracion_Monolitos.Services;

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
        using var workspace = MSBuildWorkspace.Create();
        var proyecto = await workspace.OpenProjectAsync(rutaProyecto);
        var compilacion = await proyecto.GetCompilationAsync();

        var dependencias = new List<DependenciaModel>();

        foreach (var documento in proyecto.Documents)
        {
            var syntaxTree = await documento.GetSyntaxTreeAsync();
            var root = await syntaxTree.GetRootAsync();

            var semanticModel = compilacion.GetSemanticModel(syntaxTree);
            var clases = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var clase in clases)
            {
                var origen = clase.Identifier.Text;
                var nsOrigen = clase.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.ToString();

                var identificadores = clase.DescendantNodes().OfType<IdentifierNameSyntax>();

                foreach (var id in identificadores)
                {
                    var simbolo = semanticModel.GetSymbolInfo(id).Symbol;

                    if (simbolo is ITypeSymbol tipo)
                    {
                        var destino = tipo.Name;
                        var nsDestino = tipo.ContainingNamespace?.ToString();

                        if (destino != origen) // evitar autodependencias
                        {
                            dependencias.Add(new DependenciaModel
                            {
                                ClaseOrigen = origen,
                                ClaseDependencia = destino,
                                NamespaceOrigen = nsOrigen,
                                NamespaceDependencia = nsDestino,
                                FechaAnalisis = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
        }

        await _repo.GuardarDependenciasAsync(dependencias);
    }

}
