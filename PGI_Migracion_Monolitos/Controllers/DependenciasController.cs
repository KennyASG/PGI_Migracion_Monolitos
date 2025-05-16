using Microsoft.AspNetCore.Mvc;
using PGI_Migracion_Monolitos.Interfaces.Repository;

namespace PGI_Migracion_Monolitos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DependenciasController : ControllerBase
{
    private readonly IDependenciaRepository _repo;

    public DependenciasController(IDependenciaRepository repo)
    {
        _repo = repo;
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

        var dependencias = await _repo.ObtenerPorProyectoAsync(proyecto);

        var nodos = dependencias
            .SelectMany(d => new[] { d.ClaseOrigen, d.ClaseDependencia })
            .Distinct()
            .Select(nombre => new { id = nombre, label = nombre })
            .ToList();

        var enlaces = dependencias
            .Select(d => new { from = d.ClaseOrigen, to = d.ClaseDependencia })
            .ToList();

        return Ok(new { nodes = nodos, edges = enlaces });
    }

}
