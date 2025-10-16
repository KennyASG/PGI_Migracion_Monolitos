namespace TransformadorService.DTOs
{
    public class ClaseMicroservicioDto
    {
        public string Nombre { get; set; }
        public string Namespace { get; set; }
        public List<MetodoMicroservicioDto> Metodos { get; set; } = new();
    }

    public class MetodoMicroservicioDto
    {
        public string Nombre { get; set; }
        public string HttpMethod { get; set; }
        public string Route { get; set; }
    }

    public class MicroserviceGenerationResult
    {
        public string RutaMicroservicio { get; set; }
        public List<ClaseMicroservicioDto> ClasesExtraidas { get; set; } = new();
        public bool RefactorizacionExitosa { get; set; }
    }

    public class TransformRequest
    {
        public string Proyecto { get; set; }
        public string Modulo { get; set; }
    }
}