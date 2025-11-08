using RefactorizacionService.DTOs;
using RefactorizacionService.Models;

namespace RefactorizacionService.Services
{
    public interface IServiciosMigradosService
    {
        Task<ServicioMigrado> RegistrarServicioMigradoAsync(RegistrarServicioMigradoDto dto);
        
        Task<List<ServicioMigrado>> ObtenerServiciosMigradosAsync(string nombreProyecto);
        
        Task<ServicioMigrado?> ObtenerServicioPorModuloAsync(
            string nombreProyecto, 
            string nombreModulo);
        
        Task<bool> DesactivarServicioAsync(int servicioId);
        
        Task<ListarServiciosMigradosResponseDto> ListarTodosLosServiciosAsync();
    }
}