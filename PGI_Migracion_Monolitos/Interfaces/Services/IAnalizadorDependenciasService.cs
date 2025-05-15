namespace PGI_Migracion_Monolitos.Interfaces.Services;

public interface IAnalizadorDependenciasService
{
    Task AnalizarDependenciasAsync(string rutaProyecto);
}
