using Microsoft.AspNetCore.Mvc;
using TransformadorService.Services;
using TransformadorService.DTOs;

namespace TransformadorService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransformationController : ControllerBase
    {
        private readonly ITransformadorService _transformador;

        public TransformationController(ITransformadorService transformador)
        {
            _transformador = transformador;
        }

        [HttpPost("extract")]
        public async Task<IActionResult> ExtraerModulo([FromBody] TransformRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Proyecto) || string.IsNullOrWhiteSpace(request.Modulo))
                return BadRequest("Debe especificar proyecto y módulo.");

            var resultado = await _transformador.GenerarMicroservicioAsync(request.Proyecto, request.Modulo);
            return Ok(resultado);
        }
    }
}