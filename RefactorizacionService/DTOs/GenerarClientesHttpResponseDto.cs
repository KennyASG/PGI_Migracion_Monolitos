namespace RefactorizacionService.DTOs
{
    public class GenerarClientesHttpResponseDto
    {
        public string NombreInterfaz { get; set; }
        public string CodigoInterfaz { get; set; }
        public string NombreClaseImplementacion { get; set; }
        public string CodigoImplementacion { get; set; }
        public string RutaArchivos { get; set; }
        public List<string> MetodosGenerados { get; set; }
    }
}