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
        private readonly IAnalizadorDependenciasService _analizador;  

        public DependenciasController(
            IDependenciaRepository repo,
            IAnalizadorDependenciasService analizador)
        {
            _repo = repo;
            _analizador = analizador;
        }

       
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

        
        [HttpGet("grafo")]
        public async Task<IActionResult> ObtenerGrafo([FromQuery] string proyecto)
        {
            if (string.IsNullOrWhiteSpace(proyecto))
                return BadRequest("Debes proporcionar un nombre de proyecto.");

            
            var grafoDto = await _analizador.ObtenerGrafoAsync(proyecto);

            
            var nodes = grafoDto.Nodes
                .Select(n => new { id = n.Id, label = n.Label, tipo = n.Tipo })
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
        
        [HttpGet("tabla")]
        public async Task<IActionResult> ObtenerTabla([FromQuery] string proyecto)
        {
            if (string.IsNullOrWhiteSpace(proyecto))
                return BadRequest("Se requiere un nombre de proyecto.");

            var resultado = await _analizador.ObtenerListaDependenciasAsync(proyecto);
            return Ok(resultado);
        }

    }
}
