using RefactorizacionService.Models;

namespace RefactorizacionService.Services
{
    public interface IRefactorizacionHistorialService
    {
        Task<RefactorizacionHistorial> CrearHistorialAsync(
            string nombreProyecto, 
            string moduloRefactorizado);
        
        Task ActualizarHistorialAsync(
            int historialId, 
            string estado, 
            string? codigoDiff = null, 
            string? mensajeError = null);
        
        Task AgregarDependenciaRefactorizadaAsync(
            int historialId, 
            DependenciaRefactorizada dependencia);
        
        Task<List<RefactorizacionHistorial>> ObtenerHistorialPorProyectoAsync(
            string nombreProyecto);
        
        Task<RefactorizacionHistorial?> ObtenerHistorialPorIdAsync(int historialId);
    }
}