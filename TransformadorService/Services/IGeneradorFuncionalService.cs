using System.Threading.Tasks;
using System.Collections.Generic;
using TransformadorService.DTOs;

namespace TransformadorService.Services
{
    public interface IGeneradorFuncionalService
    {
        Task<string> GenerarMicroservicioFuncionalAsync(
            string nombreProyecto, 
            string nombreModulo, 
            List<ClaseCompletaDto> clasesCompletas);
    }
}