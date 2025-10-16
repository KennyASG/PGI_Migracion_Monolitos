using TransformadorService.DTOs;

namespace TransformadorService.Services
{
    public interface ITransformadorService
    {
        Task<MicroserviceGenerationResult> GenerarMicroservicioAsync(string proyecto, string nombreModulo);
    }
}