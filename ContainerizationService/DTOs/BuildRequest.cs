using System.ComponentModel.DataAnnotations;

namespace ContainerizationService.DTOs
{
    public class BuildRequest
    {
        [Required]
        public string Proyecto { get; set; }

        [Required]
        public string Modulo { get; set; }

        [Required]
        public string RutaMicroservicio { get; set; }

        public int? PuertoPreferido { get; set; }
    }
}