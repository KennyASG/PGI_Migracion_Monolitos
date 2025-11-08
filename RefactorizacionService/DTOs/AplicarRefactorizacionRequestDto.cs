namespace RefactorizacionService.DTOs
{
    public class AplicarRefactorizacionRequestDto
    {
        public string NombreProyecto { get; set; }
        public string ModuloARefactorizar { get; set; }
        public List<DependenciaARefactorizarDto> Dependencias { get; set; }
        public bool GenerarSoloPreview { get; set; } // true = solo diff, false = aplicar cambios
    }

    public class DependenciaARefactorizarDto
    {
        public string ServicioOriginal { get; set; }
        public string UrlMicroservicio { get; set; }
        public string TipoRefactorizacion { get; set; } // "HttpClient"
    }
}