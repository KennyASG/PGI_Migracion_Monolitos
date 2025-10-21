using System;
using ContainerizationService.Models;

namespace ContainerizationService.DTOs
{
    public class DeploymentInfoDto
    {
        public int Id { get; set; }
        public string NombreModulo { get; set; }
        public string NombreProyecto { get; set; }
        public string ImagenDocker { get; set; }
        public string ContenedorId { get; set; }
        public ContainerState Estado { get; set; }
        public int PuertoAsignado { get; set; }
        public string Url { get; set; }
        public string RutaMicroservicio { get; set; }
        public string DockerfilePath { get; set; }
        public string DockerComposeFilePath { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaUltimaActualizacion { get; set; }
    }
}