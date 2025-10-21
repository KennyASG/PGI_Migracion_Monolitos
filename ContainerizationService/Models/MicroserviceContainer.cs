using System;
using System.ComponentModel.DataAnnotations;

namespace ContainerizationService.Models
{
    public enum ContainerState
    {
        Created,
        Building,
        Built,
        Running,
        Stopped,
        Error,
        Removed
    }

    public class MicroserviceContainer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string NombreModulo { get; set; }

        [Required]
        [MaxLength(200)]
        public string NombreProyecto { get; set; }

        [Required]
        [MaxLength(500)]
        public string RutaMicroservicio { get; set; }

        [MaxLength(200)]
        public string? ImagenDocker { get; set; }

        [MaxLength(100)]
        public string? ContenedorId { get; set; }

        [Required]
        public ContainerState Estado { get; set; }

        public int PuertoAsignado { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; }

        [Required]
        public DateTime FechaUltimaActualizacion { get; set; }

        public string? Logs { get; set; }

        public string? ErrorMessage { get; set; }

        [MaxLength(500)]
        public string? DockerComposeFilePath { get; set; }

        [MaxLength(500)]
        public string? DockerfilePath { get; set; }

        // Navegación
        public PortAssignment? PortAssignment { get; set; }
    }
}