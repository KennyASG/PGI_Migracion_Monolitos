using PGI_Migracion_Monolitos.Models;

namespace PGI_Migracion_Monolitos.Interfaces.Repository;

public interface IDependenciaRepository
{
    Task GuardarDependenciasAsync(IEnumerable<DependenciaModel> dependencias);
}
