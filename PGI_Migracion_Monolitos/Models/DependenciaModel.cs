using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PGI_Migracion_Monolitos.Models
{
    public class DependenciaModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ClaseOrigen { get; set; } = string.Empty;

        [Required]
        public string ClaseDependencia { get; set; } = string.Empty;

        public string? NamespaceOrigen { get; set; }

        public string? NamespaceDependencia { get; set; }

        public DateTime FechaAnalisis { get; set; } = DateTime.UtcNow;

        public string ProyectoAnalizado { get; set; } = string.Empty;

    }
}