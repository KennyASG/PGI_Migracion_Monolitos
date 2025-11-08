namespace RefactorizacionService.DTOs
{
    public class ListarServiciosMigradosResponseDto
    {
        public List<ServicioMigradoInfoDto> Servicios { get; set; }
        public int TotalServicios { get; set; }
    }

    public class ServicioMigradoInfoDto
    {
        public int Id { get; set; }
        public string NombreProyecto { get; set; }
        public string NombreModulo { get; set; }
        public string UrlMicroservicio { get; set; }
        public int Puerto { get; set; }
        public DateTime FechaMigracion { get; set; }
        public bool EstaActivo { get; set; }
    }
}