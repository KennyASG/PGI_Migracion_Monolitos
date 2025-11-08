using RefactorizacionService.DTOs;

namespace RefactorizacionService.Services
{
    public interface IAnalizadorDependenciasService
    {
        Task<AnalizarRefactorizacionResponseDto> AnalizarDependenciasAsync(
            string nombreProyecto, 
            string moduloARefactorizar);
        
        Task<List<DependenciaDetectadaDto>> DetectarInvocacionesServiciosAsync(
            string rutaArchivo, 
            List<string> serviciosMigrados);
        
        Task<string> CalcularNivelComplejidadAsync(int totalDependencias);
    }
}