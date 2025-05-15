using Microsoft.AspNetCore.Mvc;
using PGI_Migracion_Monolitos.Interfaces.Services;

namespace PGI_Migracion_Monolitos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalisisController : ControllerBase
{
    private readonly IAnalizadorDependenciasService _analizador;

    public AnalisisController(IAnalizadorDependenciasService analizador)
    {
        _analizador = analizador;
    }

    [HttpPost("roslyn")]
    public async Task<IActionResult> EjecutarAnalisis([FromBody] string rutaProyecto)
    {
        if (string.IsNullOrWhiteSpace(rutaProyecto))
            return BadRequest("La ruta del proyecto no puede estar vacía.");

        await _analizador.AnalizarDependenciasAsync(rutaProyecto);

        return Ok("Análisis completado y dependencias guardadas.");
    }
}