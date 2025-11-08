using Microsoft.EntityFrameworkCore;
using RefactorizacionService.Data;
using RefactorizacionService.Models;

namespace RefactorizacionService.Services
{
    public class RefactorizacionHistorialService : IRefactorizacionHistorialService
    {
        private readonly RefactorizacionDbContext _context;

        public RefactorizacionHistorialService(RefactorizacionDbContext context)
        {
            _context = context;
        }

        public async Task<RefactorizacionHistorial> CrearHistorialAsync(
            string nombreProyecto, 
            string moduloRefactorizado)
        {
            var historial = new RefactorizacionHistorial
            {
                NombreProyecto = nombreProyecto,
                ModuloRefactorizado = moduloRefactorizado,
                Estado = "Pendiente",
                FechaRefactorizacion = DateTime.UtcNow,
                CantidadDependenciasRefactorizadas = 0,
                DependenciasRefactorizadas = new List<DependenciaRefactorizada>()
            };

            _context.RefactorizacionHistorial.Add(historial);
            await _context.SaveChangesAsync();

            return historial;
        }

        public async Task ActualizarHistorialAsync(
            int historialId, 
            string estado, 
            string? codigoDiff = null, 
            string? mensajeError = null)
        {
            var historial = await _context.RefactorizacionHistorial.FindAsync(historialId);
            
            if (historial == null)
                throw new InvalidOperationException($"Historial {historialId} no encontrado");

            historial.Estado = estado;
            historial.CodigoDiff = codigoDiff;
            historial.MensajeError = mensajeError;

            await _context.SaveChangesAsync();
        }

        public async Task AgregarDependenciaRefactorizadaAsync(
            int historialId, 
            DependenciaRefactorizada dependencia)
        {
            var historial = await _context.RefactorizacionHistorial.FindAsync(historialId);
            
            if (historial == null)
                throw new InvalidOperationException($"Historial {historialId} no encontrado");

            dependencia.RefactorizacionHistorialId = historialId;
            _context.DependenciasRefactorizadas.Add(dependencia);
            
            historial.CantidadDependenciasRefactorizadas++;
            
            await _context.SaveChangesAsync();
        }

        public async Task<List<RefactorizacionHistorial>> ObtenerHistorialPorProyectoAsync(string nombreProyecto)
        {
            return await _context.RefactorizacionHistorial
                .Include(h => h.DependenciasRefactorizadas)
                .Where(h => h.NombreProyecto == nombreProyecto)
                .OrderByDescending(h => h.FechaRefactorizacion)
                .ToListAsync();
        }

        public async Task<RefactorizacionHistorial?> ObtenerHistorialPorIdAsync(int historialId)
        {
            return await _context.RefactorizacionHistorial
                .Include(h => h.DependenciasRefactorizadas)
                .FirstOrDefaultAsync(h => h.Id == historialId);
        }
    }
}