using System.Threading.Tasks;
using PGI_Migracion_Monolitos.Services;  // Para GrafoDto

namespace PGI_Migracion_Monolitos.Interfaces.Services
{
    public interface IAnalizadorDependenciasService
    {
        
        /// Analiza el proyecto con Roslyn y persiste en BD todas las dependencias encontradas.
        Task AnalizarDependenciasAsync(string rutaProyecto);
        
        /// Construye y retorna el grafo de dependencias (nodos + links).
        Task<GrafoDto> ObtenerGrafoAsync(string proyecto);
        
        /// Obtenemos el listado de dependencias para la tabla de visualización
        Task<List<DependenciaPlanoDto>> ObtenerListaDependenciasAsync(string proyecto);

    }
}