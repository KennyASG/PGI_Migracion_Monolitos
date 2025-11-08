using RefactorizacionService.DTOs;

namespace RefactorizacionService.Services
{
    public interface IRefactorizadorCodigoService
    {
        Task<AplicarRefactorizacionResponseDto> RefactorizarModuloAsync(
            AplicarRefactorizacionRequestDto request);
        
        Task<string> ReemplazarInvocacionDirectaPorHttpAsync(
            string codigoOriginal, 
            string servicioOriginal, 
            string urlMicroservicio);
        
        Task<string> GenerarDiffAsync(
            string codigoOriginal, 
            string codigoRefactorizado);
        
        Task GuardarCodigoRefactorizadoAsync(
            string rutaDestino, 
            string codigoRefactorizado);
    }
}