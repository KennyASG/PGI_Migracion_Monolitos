using System.Threading.Tasks;
using System.Collections.Generic;
using TransformadorService.DTOs;

namespace TransformadorService.Services
{
    public interface IGeneradorMicroservicioService
    {
        Task<string> GenerarMicroservicioAsync(string nombreProyecto, string nombreModulo, List<ClaseAnalizadaDto> clases);
    }
}