namespace RefactorizacionService.DTOs
{
    public class AplicarRefactorizacionResponseDto
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public int RefactorizacionHistorialId { get; set; }
        public List<CambioRealizadoDto> CambiosRealizados { get; set; }
        public string? CodigoDiff { get; set; }
        public List<ArchivoModificadoDto> ArchivosModificados { get; set; }
        public string? RutaCodigoRefactorizado { get; set; }
    }

    public class CambioRealizadoDto
    {
        public string NombreArchivo { get; set; }
        public string NombreClase { get; set; }
        public string NombreMetodo { get; set; }
        public string CodigoAntes { get; set; }
        public string CodigoDespues { get; set; }
        public int LineaInicio { get; set; }
        public int LineaFin { get; set; }
    }

    public class ArchivoModificadoDto
    {
        public string RutaArchivo { get; set; }
        public string NombreArchivo { get; set; }
        public int CantidadCambios { get; set; }
    }
}