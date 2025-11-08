namespace RefactorizacionService.DTOs
{
    public class GenerarClientesHttpRequestDto
    {
        public string NombreProyecto { get; set; }
        public string ServicioDestino { get; set; } // "Users", "Inventory"
        public string UrlMicroservicio { get; set; }
        public string TipoCliente { get; set; } // "Native", "Refit"
    }
}