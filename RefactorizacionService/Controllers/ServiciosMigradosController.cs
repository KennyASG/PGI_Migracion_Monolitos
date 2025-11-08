using Microsoft.AspNetCore.Mvc;
using RefactorizacionService.DTOs;
using RefactorizacionService.Services;

namespace RefactorizacionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiciosMigradosController : ControllerBase
    {
        private readonly IServiciosMigradosService _serviciosMigradosService;

        public ServiciosMigradosController(IServiciosMigradosService serviciosMigradosService)
        {
            _serviciosMigradosService = serviciosMigradosService;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarServicioMigrado([FromBody] RegistrarServicioMigradoDto dto)
        {
            try
            {
                var servicio = await _serviciosMigradosService.RegistrarServicioMigradoAsync(dto);
                return Ok(new { mensaje = "Servicio migrado registrado exitosamente", servicio });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{nombreProyecto}")]
        public async Task<IActionResult> ObtenerServiciosMigrados(string nombreProyecto)
        {
            try
            {
                var servicios = await _serviciosMigradosService.ObtenerServiciosMigradosAsync(nombreProyecto);
                return Ok(servicios);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{nombreProyecto}/{nombreModulo}")]
        public async Task<IActionResult> ObtenerServicioPorModulo(string nombreProyecto, string nombreModulo)
        {
            try
            {
                var servicio = await _serviciosMigradosService.ObtenerServicioPorModuloAsync(nombreProyecto, nombreModulo);
                
                if (servicio == null)
                    return NotFound(new { mensaje = $"Servicio {nombreModulo} no encontrado para el proyecto {nombreProyecto}" });

                return Ok(servicio);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ListarTodosLosServicios()
        {
            try
            {
                var response = await _serviciosMigradosService.ListarTodosLosServiciosAsync();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{servicioId}")]
        public async Task<IActionResult> DesactivarServicio(int servicioId)
        {
            try
            {
                var resultado = await _serviciosMigradosService.DesactivarServicioAsync(servicioId);
                
                if (!resultado)
                    return NotFound(new { mensaje = $"Servicio {servicioId} no encontrado" });

                return Ok(new { mensaje = "Servicio desactivado exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}