using System.Threading.Tasks;
using System.Collections.Generic;
using TransformadorService.DTOs;

namespace TransformadorService.Services
{
    public interface IAnalizadorCodigoService
    {
        Task<List<ClaseAnalizadaDto>> AnalizarModuloAsync(string nombreProyecto, string nombreModulo);
    }
}