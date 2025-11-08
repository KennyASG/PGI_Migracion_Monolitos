using Microsoft.EntityFrameworkCore;
using RefactorizacionService.Models;

namespace RefactorizacionService.Data
{
    public class RefactorizacionDbContext : DbContext
    {
        public RefactorizacionDbContext(DbContextOptions<RefactorizacionDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefactorizacionHistorial> RefactorizacionHistorial { get; set; }
        public DbSet<ServicioMigrado> ServiciosMigrados { get; set; }
        public DbSet<DependenciaRefactorizada> DependenciasRefactorizadas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RefactorizacionHistorial>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NombreProyecto).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ModuloRefactorizado).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Estado).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FechaRefactorizacion).IsRequired();
            });

            modelBuilder.Entity<ServicioMigrado>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NombreProyecto).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NombreModulo).IsRequired().HasMaxLength(200);
                entity.Property(e => e.UrlMicroservicio).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Puerto).IsRequired();
                entity.Property(e => e.FechaMigracion).IsRequired();
                entity.HasIndex(e => new { e.NombreProyecto, e.NombreModulo }).IsUnique();
            });

            modelBuilder.Entity<DependenciaRefactorizada>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NombreClase).IsRequired().HasMaxLength(200);
                entity.Property(e => e.NombreMetodo).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ServicioOriginal).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TipoRefactorizacion).IsRequired().HasMaxLength(50);
                
                entity.HasOne(e => e.Historial)
                    .WithMany(h => h.DependenciasRefactorizadas)
                    .HasForeignKey(e => e.RefactorizacionHistorialId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}