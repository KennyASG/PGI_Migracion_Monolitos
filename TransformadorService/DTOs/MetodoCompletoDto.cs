namespace TransformadorService.DTOs
{
    public class MetodoCompletoDto
    {
        public string Nombre { get; set; }
        public string HttpVerb { get; set; }
        public string Ruta { get; set; }
        public string CodigoCompleto { get; set; }
        public string TipoRetorno { get; set; }
        public List<ParametroDto> Parametros { get; set; } = new();
        public List<string> DependenciasUsadas { get; set; } = new();
    }

    public class ParametroDto
    {
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public bool EsFromBody { get; set; }
        public bool EsFromRoute { get; set; }
    }

    public class ClaseCompletaDto
    {
        public string Nombre { get; set; }
        public string Namespace { get; set; }
        public List<MetodoCompletoDto> Metodos { get; set; } = new();
        public List<string> Usings { get; set; } = new();
        public List<PropiedadDto> Propiedades { get; set; } = new();
        public List<string> DependenciasConstructor { get; set; } = new();
        public string CodigoClaseCompleta { get; set; }
    }

    public class PropiedadDto
    {
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public bool TieneGetter { get; set; }
        public bool TieneSetter { get; set; }
    }

    public class ModeloDto
    {
        public string Nombre { get; set; }
        public string Namespace { get; set; }
        public string CodigoCompleto { get; set; }
        public List<PropiedadDto> Propiedades { get; set; } = new();
    }
}