using Microsoft.EntityFrameworkCore;
using PGI_Migracion_Monolitos.Models;

namespace PGI_Migracion_Monolitos.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<DependenciaModel> Dependencias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DependenciaModel>(entity =>
            {
                entity.ToTable("Dependencias");
                entity.Property(d => d.ClaseOrigen).IsRequired().HasMaxLength(150);
                entity.Property(d => d.ClaseDependencia).IsRequired().HasMaxLength(150);
            });
        }
    }
}