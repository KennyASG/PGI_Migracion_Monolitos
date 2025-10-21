using Microsoft.EntityFrameworkCore;
using ContainerizationService.Models;

namespace ContainerizationService.Data
{
    public class ContainerizationDbContext : DbContext
    {
        public ContainerizationDbContext(DbContextOptions<ContainerizationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MicroserviceContainer> MicroserviceContainers { get; set; }
        public DbSet<PortAssignment> PortAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de MicroserviceContainer
            modelBuilder.Entity<MicroserviceContainer>(entity =>
            {
                entity.ToTable("MicroserviceContainers");

                entity.HasIndex(e => e.NombreModulo)
                    .IsUnique()
                    .HasDatabaseName("IX_MicroserviceContainers_NombreModulo");

                entity.HasIndex(e => e.ContenedorId)
                    .HasDatabaseName("IX_MicroserviceContainers_ContenedorId");

                entity.HasIndex(e => e.Estado)
                    .HasDatabaseName("IX_MicroserviceContainers_Estado");

                entity.Property(e => e.Estado)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.FechaUltimaActualizacion)
                    .HasDefaultValueSql("GETDATE()");

                // Relación con PortAssignment
                entity.HasOne(e => e.PortAssignment)
                    .WithOne(p => p.MicroserviceContainer)
                    .HasForeignKey<PortAssignment>(p => p.MicroserviceContainerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de PortAssignment
            modelBuilder.Entity<PortAssignment>(entity =>
            {
                entity.ToTable("PortAssignments");

                entity.HasIndex(e => e.Puerto)
                    .IsUnique()
                    .HasDatabaseName("IX_PortAssignments_Puerto");

                entity.HasIndex(e => e.EnUso)
                    .HasDatabaseName("IX_PortAssignments_EnUso");
            });

            // Seed de puertos disponibles (6000-6100)
            var ports = new List<PortAssignment>();
            for (int i = 6000; i <= 6100; i++)
            {
                ports.Add(new PortAssignment
                {
                    Id = i - 5999, // Id empieza en 1
                    Puerto = i,
                    EnUso = false
                });
            }

            modelBuilder.Entity<PortAssignment>().HasData(ports);
        }
    }
}