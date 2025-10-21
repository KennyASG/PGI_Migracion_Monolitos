namespace ContainerizationService.DTOs
{
    public class LogsResponse
    {
        public bool Success { get; set; }
        public string NombreModulo { get; set; }
        public string ContenedorId { get; set; }
        public string Logs { get; set; }
        public int LineasMostradas { get; set; }
    }
}