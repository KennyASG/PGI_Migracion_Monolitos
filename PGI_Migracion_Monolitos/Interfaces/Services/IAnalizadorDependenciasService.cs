using System.Threading.Tasks;
using PGI_Migracion_Monolitos.Services;  // Para GrafoDto

namespace PGI_Migracion_Monolitos.Interfaces.Services
{
    public interface IAnalizadorDependenciasService
    {
        /// <summary>
        /// Analiza el proyecto con Roslyn y persiste en BD todas las dependencias encontradas.
        /// </summary>
        Task AnalizarDependenciasAsync(string rutaProyecto);

        /// <summary>
        /// Construye y retorna el grafo de dependencias (nodos + links) listo para el frontend.
        /// </summary>
        Task<GrafoDto> ObtenerGrafoAsync(string proyecto);
    }
}