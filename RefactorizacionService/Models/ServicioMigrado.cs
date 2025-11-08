namespace RefactorizacionService.Models
{
    public class ServicioMigrado
    {
        public int Id { get; set; }
        public string NombreProyecto { get; set; }
        public string NombreModulo { get; set; }
        public string UrlMicroservicio { get; set; } // http://localhost:6000
        public int Puerto { get; set; }
        public DateTime FechaMigracion { get; set; }
        public bool EstaActivo { get; set; }
    }
}