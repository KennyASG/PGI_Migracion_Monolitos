using Microsoft.AspNetCore.Mvc;
using PGI_Migracion_Monolitos.DTOs;
using PGI_Migracion_Monolitos.Interfaces.Services;

namespace PGI_Migracion_Monolitos.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchivoController : ControllerBase
    {
        private readonly IAnalizadorDependenciasService _analizador;

        public ArchivoController(IAnalizadorDependenciasService analizador)
        {
            _analizador = analizador;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] UploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No se recibió archivo.");

            if (string.IsNullOrWhiteSpace(request.ProjectName))
                return BadRequest("Debe proporcionar un nombre de proyecto.");

            var uploadsFolder = Path.Combine("Uploads", request.ProjectName);
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, request.File.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            await _analizador.AnalizarDependenciasAsync(filePath);

            return Ok(new
            {
                message = "Archivo recibido y análisis completado.",
                ruta = Path.GetFullPath(filePath),
                nombreProyecto = request.ProjectName
            });
        }

    }
}