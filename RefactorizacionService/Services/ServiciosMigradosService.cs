using Microsoft.EntityFrameworkCore;
using RefactorizacionService.Data;
using RefactorizacionService.DTOs;
using RefactorizacionService.Models;

namespace RefactorizacionService.Services
{
    public class ServiciosMigradosService : IServiciosMigradosService
    {
        private readonly RefactorizacionDbContext _context;

        public ServiciosMigradosService(RefactorizacionDbContext context)
        {
            _context = context;
        }

        public async Task<ServicioMigrado> RegistrarServicioMigradoAsync(RegistrarServicioMigradoDto dto)
        {
            var existente = await _context.ServiciosMigrados
                .FirstOrDefaultAsync(s => s.NombreProyecto == dto.NombreProyecto 
                    && s.NombreModulo == dto.NombreModulo);

            if (existente != null)
            {
                existente.UrlMicroservicio = dto.UrlMicroservicio;
                existente.Puerto = dto.Puerto;
                existente.FechaMigracion = DateTime.UtcNow;
                existente.EstaActivo = true;
                
                await _context.SaveChangesAsync();
                return existente;
            }

            var nuevoServicio = new ServicioMigrado
            {
                NombreProyecto = dto.NombreProyecto,
                NombreModulo = dto.NombreModulo,
                UrlMicroservicio = dto.UrlMicroservicio,
                Puerto = dto.Puerto,
                FechaMigracion = DateTime.UtcNow,
                EstaActivo = true
            };

            _context.ServiciosMigrados.Add(nuevoServicio);
            await _context.SaveChangesAsync();

            return nuevoServicio;
        }

        public async Task<List<ServicioMigrado>> ObtenerServiciosMigradosAsync(string nombreProyecto)
        {
            return await _context.ServiciosMigrados
                .Where(s => s.NombreProyecto == nombreProyecto && s.EstaActivo)
                .OrderBy(s => s.NombreModulo)
                .ToListAsync();
        }

        public async Task<ServicioMigrado?> ObtenerServicioPorModuloAsync(string nombreProyecto, string nombreModulo)
        {
            return await _context.ServiciosMigrados
                .FirstOrDefaultAsync(s => s.NombreProyecto == nombreProyecto 
                    && s.NombreModulo == nombreModulo 
                    && s.EstaActivo);
        }

        public async Task<bool> DesactivarServicioAsync(int servicioId)
        {
            var servicio = await _context.ServiciosMigrados.FindAsync(servicioId);
            
            if (servicio == null)
                return false;

            servicio.EstaActivo = false;
            await _context.SaveChangesAsync();
            
            return true;
        }

        public async Task<ListarServiciosMigradosResponseDto> ListarTodosLosServiciosAsync()
        {
            var servicios = await _context.ServiciosMigrados
                .Where(s => s.EstaActivo)
                .OrderBy(s => s.NombreProyecto)
                .ThenBy(s => s.NombreModulo)
                .ToListAsync();

            return new ListarServiciosMigradosResponseDto
            {
                Servicios = servicios.Select(s => new ServicioMigradoInfoDto
                {
                    Id = s.Id,
                    NombreProyecto = s.NombreProyecto,
                    NombreModulo = s.NombreModulo,
                    UrlMicroservicio = s.UrlMicroservicio,
                    Puerto = s.Puerto,
                    FechaMigracion = s.FechaMigracion,
                    EstaActivo = s.EstaActivo
                }).ToList(),
                TotalServicios = servicios.Count
            };
        }
    }
}