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
}
