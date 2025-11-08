namespace RefactorizacionService.Models
{
    public class RefactorizacionHistorial
    {
        public int Id { get; set; }
        public string NombreProyecto { get; set; }
        public string ModuloRefactorizado { get; set; }
        public string Estado { get; set; } // "Pendiente", "Completado", "Error"
        public DateTime FechaRefactorizacion { get; set; }
        public string? CodigoDiff { get; set; }
        public string? MensajeError { get; set; }
        public int CantidadDependenciasRefactorizadas { get; set; }
        
        public ICollection<DependenciaRefactorizada> DependenciasRefactorizadas { get; set; }
    }
}