using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ContainerizationService.Data
{
    public class ContainerizationDbContextFactory : IDesignTimeDbContextFactory<ContainerizationDbContext>
    {
        public ContainerizationDbContext CreateDbContext(string[] args)
        {
            // Cargar configuración desde appsettings.json o appsettings.Development.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            // Obtener cadena de conexión desde la sección "ConnectionStrings:DefaultConnection"
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configurar opciones de DbContext
            var optionsBuilder = new DbContextOptionsBuilder<ContainerizationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ContainerizationDbContext(optionsBuilder.Options);
        }
    }
}
