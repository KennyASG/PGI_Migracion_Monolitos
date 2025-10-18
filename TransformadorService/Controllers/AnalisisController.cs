using Microsoft.AspNetCore.Mvc;
using TransformadorService.Services;

namespace TransformadorService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalisisController : ControllerBase
    {
        private readonly IAnalizadorCodigoService _analizador;

        public AnalisisController(IAnalizadorCodigoService analizador)
        {
            _analizador = analizador;
        }

        [HttpGet("{proyecto}/{modulo}")]
        public async Task<IActionResult> AnalizarModulo(string proyecto, string modulo)
        {
            var resultado = await _analizador.AnalizarModuloAsync(proyecto, modulo);
            return Ok(resultado);
        }
    }
}