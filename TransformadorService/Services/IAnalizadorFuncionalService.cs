using System.Threading.Tasks;
using System.Collections.Generic;
using TransformadorService.DTOs;

namespace TransformadorService.Services
{
    public interface IAnalizadorFuncionalService
    {
        Task<List<ClaseCompletaDto>> AnalizarModuloCompletoAsync(string nombreProyecto, string nombreModulo);
    }
}