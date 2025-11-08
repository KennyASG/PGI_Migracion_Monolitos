namespace RefactorizacionService.Models
{
    public class DependenciaRefactorizada
    {
        public int Id { get; set; }
        public int RefactorizacionHistorialId { get; set; }
        public string NombreClase { get; set; }
        public string NombreMetodo { get; set; }
        public string ServicioOriginal { get; set; } // "UserService"
        public string CodigoOriginal { get; set; }
        public string CodigoRefactorizado { get; set; }
        public string TipoRefactorizacion { get; set; } // "HttpClient", "Refit", "EventDriven"
        public int LineaInicio { get; set; }
        public int LineaFin { get; set; }
        
        public RefactorizacionHistorial Historial { get; set; }
    }
}