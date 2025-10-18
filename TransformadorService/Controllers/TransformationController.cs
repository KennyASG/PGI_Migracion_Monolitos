using Microsoft.AspNetCore.Mvc;
using TransformadorService.Services;

namespace TransformadorService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransformacionFuncionalController : ControllerBase
    {
        private readonly IAnalizadorFuncionalService _analizadorFuncional;
        private readonly IGeneradorFuncionalService _generadorFuncional;

        public TransformacionFuncionalController(
            IAnalizadorFuncionalService analizadorFuncional, 
            IGeneradorFuncionalService generadorFuncional)
        {
            _analizadorFuncional = analizadorFuncional;
            _generadorFuncional = generadorFuncional;
        }

        /// <summary>
        /// Genera un microservicio con migración funcional completa (con lógica de negocio)
        /// </summary>
        [HttpPost("{proyecto}/{modulo}")]
        public async Task<IActionResult> GenerarMicroservicioFuncional(string proyecto, string modulo)
        {
            try
            {
                // 1. Analizar el módulo completo (código, dependencias, modelos)
                var clasesCompletas = await _analizadorFuncional.AnalizarModuloCompletoAsync(proyecto, modulo);

                // 2. Generar microservicio con lógica funcional
                var rutaGenerada = await _generadorFuncional.GenerarMicroservicioFuncionalAsync(
                    proyecto, 
                    modulo, 
                    clasesCompletas
                );

                return Ok(new
                {
                    success = true,
                    message = $"Microservicio '{modulo}' generado con migración funcional completa",
                    rutaGenerada = rutaGenerada,
                    clasesAnalizadas = clasesCompletas.Count,
                    detalles = new
                    {
                        controllers = clasesCompletas.Count(c => c.Nombre.EndsWith("Controller")),
                        services = clasesCompletas.Count(c => c.Nombre.EndsWith("Service")),
                        modelos = clasesCompletas.FirstOrDefault(c => c.Nombre == "_Modelos")?.Usings.Count(u => u.StartsWith("Modelo:")) ?? 0
                    }
                });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (DirectoryNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message, stack = ex.StackTrace });
            }
        }

        /// <summary>
        /// Analiza un módulo y devuelve información detallada sin generar el microservicio
        /// </summary>
        [HttpGet("analizar/{proyecto}/{modulo}")]
        public async Task<IActionResult> AnalizarModuloDetallado(string proyecto, string modulo)
        {
            try
            {
                var clasesCompletas = await _analizadorFuncional.AnalizarModuloCompletoAsync(proyecto, modulo);

                return Ok(new
                {
                    success = true,
                    proyecto = proyecto,
                    modulo = modulo,
                    resumen = new
                    {
                        totalClases = clasesCompletas.Count,
                        controllers = clasesCompletas.Count(c => c.Nombre.EndsWith("Controller")),
                        services = clasesCompletas.Count(c => c.Nombre.EndsWith("Service")),
                        modelos = clasesCompletas.FirstOrDefault(c => c.Nombre == "_Modelos")?.Usings.Count(u => u.StartsWith("Modelo:")) ?? 0
                    },
                    clases = clasesCompletas.Select(c => new
                    {
                        nombre = c.Nombre,
                        tipo = c.Nombre.EndsWith("Controller") ? "Controller" : 
                               c.Nombre.EndsWith("Service") ? "Service" : "Modelo",
                        metodos = c.Metodos.Select(m => new
                        {
                            nombre = m.Nombre,
                            httpVerb = m.HttpVerb,
                            ruta = m.Ruta,
                            parametros = m.Parametros.Count,
                            dependenciasUsadas = m.DependenciasUsadas.Count,
                            tieneCodigoCompleto = !string.IsNullOrEmpty(m.CodigoCompleto)
                        }).ToList(),
                        dependenciasConstructor = c.DependenciasConstructor,
                        usings = c.Usings.Count
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}