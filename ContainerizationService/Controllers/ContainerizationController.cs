using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContainerizationService.Data;
using ContainerizationService.DTOs;
using ContainerizationService.Models;
using ContainerizationService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ContainerizationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContainerizationController : ControllerBase
    {
        private readonly ContainerizationDbContext _context;
        private readonly IDockerService _dockerService;
        private readonly IDockerComposeService _dockerComposeService;
        private readonly IPortManagementService _portManagementService;
        private readonly IConfiguration _configuration;

        public ContainerizationController(
            ContainerizationDbContext context,
            IDockerService dockerService,
            IDockerComposeService dockerComposeService,
            IPortManagementService portManagementService,
            IConfiguration configuration)
        {
            _context = context;
            _dockerService = dockerService;
            _dockerComposeService = dockerComposeService;
            _portManagementService = portManagementService;
            _configuration = configuration;
        }

        /// <summary>
        /// Construye la imagen Docker del microservicio
        /// </summary>
        [HttpPost("build")]
        public async Task<IActionResult> BuildMicroservice([FromBody] BuildRequest request)
        {
            try
            {
                // Verificar que Docker esté disponible
                if (!await _dockerService.IsDockerAvailableAsync())
                {
                    return BadRequest(new { success = false, message = "Docker no está disponible en el sistema" });
                }

                // Verificar que la ruta del microservicio exista
                if (!Directory.Exists(request.RutaMicroservicio))
                {
                    return NotFound(new { success = false, message = "La ruta del microservicio no existe" });
                }

                // Verificar si ya existe un contenedor con ese nombre
                var existingContainer = await _context.MicroserviceContainers
                    .FirstOrDefaultAsync(c => c.NombreModulo == request.Modulo);

                if (existingContainer != null && existingContainer.Estado != ContainerState.Error)
                {
                    return BadRequest(new { success = false, message = "Ya existe un contenedor con ese nombre" });
                }

                // Asignar puerto
                int puerto = request.PuertoPreferido ?? await _portManagementService.AssignPortAsync(request.Modulo);

                // Crear registro en DB
                var container = new MicroserviceContainer
                {
                    NombreModulo = request.Modulo,
                    NombreProyecto = request.Proyecto,
                    RutaMicroservicio = request.RutaMicroservicio,
                    Estado = ContainerState.Created,
                    PuertoAsignado = puerto,
                    FechaCreacion = DateTime.UtcNow,
                    FechaUltimaActualizacion = DateTime.UtcNow
                };

                _context.MicroserviceContainers.Add(container);
                await _context.SaveChangesAsync();

                // Generar Dockerfile
                var dockerfilePath = await _dockerComposeService.GenerateDockerfileAsync(
                    request.RutaMicroservicio,
                    request.Modulo,
                    puerto);

                container.DockerfilePath = dockerfilePath;

                // Generar docker-compose.yml
                var composePath = await _dockerComposeService.GenerateDockerComposeAsync(
                    request.RutaMicroservicio,
                    request.Modulo,
                    puerto,
                    puerto);

                container.DockerComposeFilePath = composePath;
                container.Estado = ContainerState.Building;
                await _context.SaveChangesAsync();

                // Construir imagen
                var imageName = request.Modulo.ToLower().Replace(" ", "-");
                string buildOutput;

                try
                {
                    buildOutput = await _dockerService.BuildImageAsync(request.RutaMicroservicio, imageName);

                    container.ImagenDocker = $"{imageName}:latest";
                    container.Estado = ContainerState.Built;
                    container.Logs = buildOutput;
                    container.FechaUltimaActualizacion = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    container.Estado = ContainerState.Error;
                    container.ErrorMessage = ex.Message;
                    container.FechaUltimaActualizacion = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return BadRequest(new BuildResponse
                    {
                        Success = false,
                        Message = $"Error al construir la imagen: {ex.Message}",
                        BuildOutput = ex.Message
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new BuildResponse
                {
                    Success = true,
                    Message = "Imagen construida exitosamente",
                    ImageName = container.ImagenDocker,
                    PuertoAsignado = puerto,
                    DockerfilePath = dockerfilePath,
                    DockerComposePath = composePath,
                    BuildOutput = buildOutput
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Despliega el microservicio usando docker-compose
        /// </summary>
        [HttpPost("deploy/{nombreModulo}")]
        public async Task<IActionResult> DeployMicroservice(string nombreModulo, [FromBody] DeployRequest request)
        {
            try
            {
                var container = await _context.MicroserviceContainers
                    .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

                if (container == null)
                {
                    return NotFound(new { success = false, message = "Microservicio no encontrado" });
                }

                if (container.Estado != ContainerState.Built && container.Estado != ContainerState.Stopped)
                {
                    return BadRequest(new { success = false, message = $"El microservicio debe estar en estado Built o Stopped. Estado actual: {container.Estado}" });
                }

                if (string.IsNullOrEmpty(container.DockerComposeFilePath))
                {
                    return BadRequest(new { success = false, message = "No se encontró el archivo docker-compose.yml" });
                }

                // Si ya está corriendo, detenerlo primero
                if (!string.IsNullOrEmpty(container.ContenedorId))
                {
                    try
                    {
                        await _dockerComposeService.ComposeDownAsync(container.DockerComposeFilePath);
                    }
                    catch { }
                }

                // Desplegar con docker-compose
                string deployOutput;
                try
                {
                    deployOutput = await _dockerComposeService.ComposeUpAsync(container.DockerComposeFilePath);

                    // Obtener información del contenedor
                    var serviceName = nombreModulo.ToLower().Replace(" ", "-");
                    var containerName = $"{serviceName}-container";

                    container.ContenedorId = containerName;
                    container.Estado = ContainerState.Running;
                    container.Logs = deployOutput;
                    container.ErrorMessage = null;
                    container.FechaUltimaActualizacion = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    container.Estado = ContainerState.Error;
                    container.ErrorMessage = ex.Message;
                    container.FechaUltimaActualizacion = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return BadRequest(new DeployResponse
                    {
                        Success = false,
                        Message = $"Error al desplegar: {ex.Message}",
                        DeployOutput = ex.Message
                    });
                }

                await _context.SaveChangesAsync();

                return Ok(new DeployResponse
                {
                    Success = true,
                    Message = "Microservicio desplegado exitosamente",
                    ContenedorId = container.ContenedorId,
                    Url = $"http://localhost:{container.PuertoAsignado}",
                    Puerto = container.PuertoAsignado,
                    ImageName = container.ImagenDocker,
                    DeployOutput = deployOutput
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Detiene un microservicio desplegado
        /// </summary>
        [HttpPost("stop/{nombreModulo}")]
        public async Task<IActionResult> StopMicroservice(string nombreModulo)
        {
            try
            {
                var container = await _context.MicroserviceContainers
                    .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

                if (container == null)
                {
                    return NotFound(new { success = false, message = "Microservicio no encontrado" });
                }

                if (container.Estado != ContainerState.Running)
                {
                    return BadRequest(new { success = false, message = "El microservicio no está en ejecución" });
                }

                if (string.IsNullOrEmpty(container.DockerComposeFilePath))
                {
                    return BadRequest(new { success = false, message = "No se encontró el archivo docker-compose.yml" });
                }

                await _dockerComposeService.ComposeDownAsync(container.DockerComposeFilePath);

                container.Estado = ContainerState.Stopped;
                container.FechaUltimaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Microservicio detenido exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Reinicia un microservicio
        /// </summary>
        [HttpPost("restart/{nombreModulo}")]
        public async Task<IActionResult> RestartMicroservice(string nombreModulo)
        {
            try
            {
                var container = await _context.MicroserviceContainers
                    .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

                if (container == null)
                {
                    return NotFound(new { success = false, message = "Microservicio no encontrado" });
                }

                // Detener si está corriendo
                if (container.Estado == ContainerState.Running && !string.IsNullOrEmpty(container.DockerComposeFilePath))
                {
                    await _dockerComposeService.ComposeDownAsync(container.DockerComposeFilePath);
                }

                // Iniciar nuevamente
                var deployOutput = await _dockerComposeService.ComposeUpAsync(container.DockerComposeFilePath);

                container.Estado = ContainerState.Running;
                container.FechaUltimaActualizacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Microservicio reiniciado exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Elimina completamente un microservicio (contenedor, imagen y registro)
        /// </summary>
        [HttpDelete("{nombreModulo}")]
        public async Task<IActionResult> DeleteMicroservice(string nombreModulo)
        {
            try
            {
                var container = await _context.MicroserviceContainers
                    .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

                if (container == null)
                {
                    return NotFound(new { success = false, message = "Microservicio no encontrado" });
                }

                // Detener y eliminar contenedor
                if (!string.IsNullOrEmpty(container.DockerComposeFilePath))
                {
                    try
                    {
                        await _dockerComposeService.ComposeDownAsync(container.DockerComposeFilePath);
                    }
                    catch { }
                }

                // Liberar puerto
                await _portManagementService.ReleasePortAsync(container.PuertoAsignado);

                // Eliminar registro de la base de datos
                _context.MicroserviceContainers.Remove(container);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Microservicio eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el estado de un microservicio
        /// </summary>
        [HttpGet("{nombreModulo}/status")]
        public async Task<IActionResult> GetStatus(string nombreModulo)
        {
            try
            {
                var container = await _context.MicroserviceContainers
                    .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

                if (container == null)
                {
                    return NotFound(new { success = false, message = "Microservicio no encontrado" });
                }

                string dockerStatus = "Unknown";
                bool isRunning = false;

                if (!string.IsNullOrEmpty(container.ContenedorId) && container.Estado == ContainerState.Running)
                {
                    var status = await _dockerService.GetContainerStatusAsync(container.ContenedorId);
                    dockerStatus = status.ToString();
                    isRunning = status == ContainerStatus.Running;

                    // Actualizar estado si no coincide
                    if (!isRunning && container.Estado == ContainerState.Running)
                    {
                        container.Estado = ContainerState.Stopped;
                        container.FechaUltimaActualizacion = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new ContainerStatusDto
                {
                    NombreModulo = container.NombreModulo,
                    NombreProyecto = container.NombreProyecto,
                    Estado = container.Estado,
                    ImagenDocker = container.ImagenDocker,
                    ContenedorId = container.ContenedorId,
                    PuertoAsignado = container.PuertoAsignado,
                    Url = $"http://localhost:{container.PuertoAsignado}",
                    FechaCreacion = container.FechaCreacion,
                    FechaUltimaActualizacion = container.FechaUltimaActualizacion,
                    ErrorMessage = container.ErrorMessage,
                    IsRunning = isRunning,
                    DockerStatus = dockerStatus
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene los logs de un microservicio
        /// </summary>
        [HttpGet("{nombreModulo}/logs")]
        public async Task<IActionResult> GetLogs(string nombreModulo, [FromQuery] int lines = 100)
        {
            try
            {
                var container = await _context.MicroserviceContainers
                    .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

                if (container == null)
                {
                    return NotFound(new { success = false, message = "Microservicio no encontrado" });
                }

                string logs = "";

                if (!string.IsNullOrEmpty(container.ContenedorId))
                {
                    try
                    {
                        logs = await _dockerService.GetContainerLogsAsync(container.ContenedorId, lines);
                    }
                    catch (Exception ex)
                    {
                        logs = $"Error obteniendo logs: {ex.Message}";
                    }
                }
                else if (!string.IsNullOrEmpty(container.Logs))
                {
                    logs = container.Logs;
                }

                return Ok(new LogsResponse
                {
                    Success = true,
                    NombreModulo = nombreModulo,
                    ContenedorId = container.ContenedorId,
                    Logs = logs,
                    LineasMostradas = lines
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista todos los microservicios desplegados
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> ListMicroservices()
        {
            try
            {
                var containers = await _context.MicroserviceContainers
                    .OrderByDescending(c => c.FechaCreacion)
                    .Select(c => new DeploymentInfoDto
                    {
                        Id = c.Id,
                        NombreModulo = c.NombreModulo,
                        NombreProyecto = c.NombreProyecto,
                        ImagenDocker = c.ImagenDocker,
                        ContenedorId = c.ContenedorId,
                        Estado = c.Estado,
                        PuertoAsignado = c.PuertoAsignado,
                        Url = $"http://localhost:{c.PuertoAsignado}",
                        RutaMicroservicio = c.RutaMicroservicio,
                        DockerfilePath = c.DockerfilePath,
                        DockerComposeFilePath = c.DockerComposeFilePath,
                        FechaCreacion = c.FechaCreacion,
                        FechaUltimaActualizacion = c.FechaUltimaActualizacion
                    })
                    .ToListAsync();

                return Ok(new { success = true, count = containers.Count, microservices = containers });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene los puertos disponibles
        /// </summary>
        [HttpGet("ports/available")]
        public async Task<IActionResult> GetAvailablePorts()
        {
            try
            {
                var ports = await _portManagementService.GetAvailablePortsAsync();
                return Ok(new { success = true, count = ports.Count, ports });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}