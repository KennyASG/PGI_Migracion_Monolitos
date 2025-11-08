using RefactorizacionService.DTOs;

namespace RefactorizacionService.Services
{
    public interface IGeneradorClientesHttpService
    {
        Task<GenerarClientesHttpResponseDto> GenerarClienteHttpAsync(
            string servicioDestino, 
            string urlMicroservicio, 
            string tipoCliente);
        
        Task<string> GenerarInterfazClienteAsync(
            string nombreServicio, 
            List<string> metodos);
        
        Task<string> GenerarImplementacionClienteNativoAsync(
            string nombreServicio, 
            string urlBase, 
            List<string> metodos);
        
        Task<string> GenerarImplementacionClienteRefitAsync(
            string nombreServicio, 
            string urlBase);
    }
}