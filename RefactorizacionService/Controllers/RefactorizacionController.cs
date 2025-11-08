using Microsoft.AspNetCore.Mvc;
using RefactorizacionService.DTOs;
using RefactorizacionService.Services;

namespace RefactorizacionService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RefactorizacionController : ControllerBase
    {
        private readonly IAnalizadorDependenciasService _analizadorService;
        private readonly IGeneradorClientesHttpService _generadorClientesService;
        private readonly IRefactorizadorCodigoService _refactorizadorService;

        public RefactorizacionController(
            IAnalizadorDependenciasService analizadorService,
            IGeneradorClientesHttpService generadorClientesService,
            IRefactorizadorCodigoService refactorizadorService)
        {
            _analizadorService = analizadorService;
            _generadorClientesService = generadorClientesService;
            _refactorizadorService = refactorizadorService;
        }

        [HttpPost("analizar")]
        public async Task<IActionResult> AnalizarDependencias([FromBody] AnalizarRefactorizacionRequestDto request)
        {
            try
            {
                var resultado = await _analizadorService.AnalizarDependenciasAsync(
                    request.NombreProyecto,
                    request.ModuloARefactorizar);

                return Ok(resultado);
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("generar-clientes")]
        public async Task<IActionResult> GenerarClientesHttp([FromBody] GenerarClientesHttpRequestDto request)
        {
            try
            {
                var resultado = await _generadorClientesService.GenerarClienteHttpAsync(
                    request.ServicioDestino,
                    request.UrlMicroservicio,
                    request.TipoCliente);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("aplicar")]
        public async Task<IActionResult> AplicarRefactorizacion([FromBody] AplicarRefactorizacionRequestDto request)
        {
            try
            {
                var resultado = await _refactorizadorService.RefactorizarModuloAsync(request);

                if (!resultado.Exitoso)
                    return BadRequest(resultado);

                return Ok(resultado);
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("preview")]
        public async Task<IActionResult> GenerarPreview([FromBody] AplicarRefactorizacionRequestDto request)
        {
            try
            {
                request.GenerarSoloPreview = true;
                
                var resultado = await _refactorizadorService.RefactorizarModuloAsync(request);

                return Ok(new
                {
                    diff = resultado.CodigoDiff,
                    cambios = resultado.CambiosRealizados,
                    archivosAfectados = resultado.ArchivosModificados
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}