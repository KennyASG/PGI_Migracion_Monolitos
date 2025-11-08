namespace RefactorizacionService.DTOs
{
    public class AnalizarRefactorizacionResponseDto
    {
        public string NombreProyecto { get; set; }
        public string ModuloAnalizado { get; set; }
        public List<DependenciaDetectadaDto> DependenciasDetectadas { get; set; }
        public int TotalDependencias { get; set; }
        public string NivelComplejidad { get; set; } // "Baja", "Media", "Alta"
        public List<string> ServiciosMigradosDisponibles { get; set; }
        public List<string> Recomendaciones { get; set; }
    }

    public class DependenciaDetectadaDto
    {
        public string NombreClase { get; set; }
        public string NombreMetodo { get; set; }
        public string ServicioDependiente { get; set; }
        public string TipoInvocacion { get; set; } // "Directo", "Inyectado"
        public bool ServicioYaMigrado { get; set; }
        public string? UrlMicroservicio { get; set; }
        public List<string> LineasCodigo { get; set; }
    }
}