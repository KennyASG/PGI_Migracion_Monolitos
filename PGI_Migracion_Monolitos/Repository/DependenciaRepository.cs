using Microsoft.EntityFrameworkCore;
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
    
    public async Task<List<DependenciaModel>> ObtenerFiltradasAsync(
        string? claseOrigen,
        string? claseDependencia,
        string? nsOrigen,
        string? nsDependencia,
        string? proyecto)
    {
        var query = _context.Dependencias.AsQueryable();

        if (!string.IsNullOrWhiteSpace(claseOrigen))
            query = query.Where(d => d.ClaseOrigen.Contains(claseOrigen));

        if (!string.IsNullOrWhiteSpace(claseDependencia))
            query = query.Where(d => d.ClaseDependencia.Contains(claseDependencia));

        if (!string.IsNullOrWhiteSpace(nsOrigen))
            query = query.Where(d => d.NamespaceOrigen != null && d.NamespaceOrigen.Contains(nsOrigen));

        if (!string.IsNullOrWhiteSpace(nsDependencia))
            query = query.Where(d => d.NamespaceDependencia != null && d.NamespaceDependencia.Contains(nsDependencia));

        if (!string.IsNullOrWhiteSpace(proyecto))
            query = query.Where(d => d.ProyectoAnalizado.Contains(proyecto));

        return await query.ToListAsync();
    }

}