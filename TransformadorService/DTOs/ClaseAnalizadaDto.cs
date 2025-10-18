using System.Collections.Generic;

namespace TransformadorService.DTOs
{
    public class ClaseAnalizadaDto
    {
        public string Nombre { get; set; }
        public string Namespace { get; set; }
        public List<MetodoAnalizadoDto> Metodos { get; set; }
    }
}