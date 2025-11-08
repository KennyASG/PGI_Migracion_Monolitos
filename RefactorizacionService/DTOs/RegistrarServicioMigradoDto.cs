namespace RefactorizacionService.DTOs
{
    public class RegistrarServicioMigradoDto
    {
        public string NombreProyecto { get; set; }
        public string NombreModulo { get; set; }
        public string UrlMicroservicio { get; set; }
        public int Puerto { get; set; }
    }
}