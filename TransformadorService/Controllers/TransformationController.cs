using Microsoft.AspNetCore.Mvc;
using TransformadorService.Services;

namespace TransformadorService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransformationController : ControllerBase
    {
        private readonly IAnalizadorCodigoService _analizador;
        private readonly IGeneradorMicroservicioService _generador;

        public TransformationController(IAnalizadorCodigoService analizador, IGeneradorMicroservicioService generador)
        {
            _analizador = analizador;
            _generador = generador;
        }

        [HttpPost("{proyecto}/{modulo}")]
        public async Task<IActionResult> GenerarMicroservicio(string proyecto, string modulo)
        {
            var clases = await _analizador.AnalizarModuloAsync(proyecto, modulo);
            var ruta = await _generador.GenerarMicroservicioAsync(proyecto, modulo, clases);

            return Ok(new
            {
                message = $"Microservicio '{modulo}' generado correctamente.",
                rutaGenerada = ruta
            });
        }
    }
}