using PGI_Migracion_Monolitos.Models;
using PGI_Migracion_Monolitos.Interfaces.Repository;
using PGI_Migracion_Monolitos.Data;

namespace PGI_Migracion_Monolitos.Repository;

public class DependenciaRepository : IDependenciaRepository
{
    private readonly ApplicationDbContext _context;

    public DependenciaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task GuardarDependenciasAsync(IEnumerable<DependenciaModel> dependencias)
    {
        _context.Dependencias.AddRange(dependencias);
        await _context.SaveChangesAsync();
    }
}