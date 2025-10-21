using System.ComponentModel.DataAnnotations;

namespace ContainerizationService.DTOs
{
    public class DeployRequest
    {
        [Required]
        public string NombreModulo { get; set; }

        public bool RebuildImage { get; set; } = false;

        public bool ForceRecreate { get; set; } = false;
    }
}