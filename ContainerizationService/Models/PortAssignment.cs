using System;
using System.ComponentModel.DataAnnotations;

namespace ContainerizationService.Models
{
    public class PortAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Puerto { get; set; }

        [Required]
        public bool EnUso { get; set; }

        public int? MicroserviceContainerId { get; set; }

        public DateTime? FechaAsignacion { get; set; }

        public DateTime? FechaLiberacion { get; set; }

        // Navegación
        public MicroserviceContainer? MicroserviceContainer { get; set; }
    }
}