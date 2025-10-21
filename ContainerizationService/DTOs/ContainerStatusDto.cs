using System;
using ContainerizationService.Models;

namespace ContainerizationService.DTOs
{
    public class ContainerStatusDto
    {
        public string NombreModulo { get; set; }
        public string NombreProyecto { get; set; }
        public ContainerState Estado { get; set; }
        public string ImagenDocker { get; set; }
        public string ContenedorId { get; set; }
        public int PuertoAsignado { get; set; }
        public string Url { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaActualizacion { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsRunning { get; set; }
        public string DockerStatus { get; set; }
    }
}