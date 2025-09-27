using PGI_Migracion_Monolitos.Models;

namespace PGI_Migracion_Monolitos.Interfaces.Repository;

public interface IDependenciaRepository
{
    Task GuardarDependenciasAsync(IEnumerable<DependenciaModel> dependencias);
    Task<List<DependenciaModel>> ObtenerFiltradasAsync(
        string? claseOrigen,
        string? claseDependencia,
        string? nsOrigen,
        string? nsDependencia,
        string? proyecto);
    Task<List<DependenciaModel>> ObtenerPorProyectoAsync(string proyecto);

}
