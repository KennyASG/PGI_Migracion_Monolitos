using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PGI_Migracion_Monolitos.Interfaces.Services;  // <-- Importa el servicio
using PGI_Migracion_Monolitos.Interfaces.Repository;

namespace PGI_Migracion_Monolitos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DependenciasController : ControllerBase
    {
        private readonly IDependenciaRepository _repo;
        private readonly IAnalizadorDependenciasService _analizador;  // <-- Inyecta el servicio

        public DependenciasController(
            IDependenciaRepository repo,
            IAnalizadorDependenciasService analizador)
        {
            _repo = repo;
            _analizador = analizador;
        }

        /// <summary>
        /// Endpoint original para filtrar dependencias.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Obtener(
            [FromQuery] string? claseOrigen,
            [FromQuery] string? claseDependencia,
            [FromQuery] string? nsOrigen,
            [FromQuery] string? nsDependencia,
            [FromQuery] string? proyecto)
        {
            var resultado = await _repo.ObtenerFiltradasAsync(
                claseOrigen, claseDependencia, nsOrigen, nsDependencia, proyecto);
            return Ok(resultado);
        }

        /// <summary>
        /// Devuelve el grafo de nodos y links listo para el frontend.
        /// </summary>
        [HttpGet("grafo")]
        public async Task<IActionResult> ObtenerGrafo([FromQuery] string proyecto)
        {
            if (string.IsNullOrWhiteSpace(proyecto))
                return BadRequest("Debes proporcionar un nombre de proyecto.");

            // Llama al servicio que hace el filtrado, deduplicación y mapeo
            var grafoDto = await _analizador.ObtenerGrafoAsync(proyecto);

            // Ajuste de nombres: nodes y links (no edges)
            var nodes = grafoDto.Nodes
                .Select(n => new { id = n.Id, label = n.Label })
                .ToList();

            var links = grafoDto.Links
                .Select(l => new { source = l.Source, target = l.Target })
                .ToList();

            return Ok(new
            {
                nodes,
                links
            });
        }
    }
}
