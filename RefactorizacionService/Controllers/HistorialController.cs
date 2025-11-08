using Microsoft.AspNetCore.Mvc;
using RefactorizacionService.Services;

namespace RefactorizacionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistorialController : ControllerBase
    {
        private readonly IRefactorizacionHistorialService _historialService;

        public HistorialController(IRefactorizacionHistorialService historialService)
        {
            _historialService = historialService;
        }

        [HttpGet("{nombreProyecto}")]
        public async Task<IActionResult> ObtenerHistorialPorProyecto(string nombreProyecto)
        {
            try
            {
                var historial = await _historialService.ObtenerHistorialPorProyectoAsync(nombreProyecto);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("detalle/{historialId}")]
        public async Task<IActionResult> ObtenerHistorialPorId(int historialId)
        {
            try
            {
                var historial = await _historialService.ObtenerHistorialPorIdAsync(historialId);
                
                if (historial == null)
                    return NotFound(new { mensaje = $"Historial {historialId} no encontrado" });

                return Ok(historial);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}