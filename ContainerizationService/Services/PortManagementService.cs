using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContainerizationService.Data;
using ContainerizationService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ContainerizationService.Services
{
    public class PortManagementService : IPortManagementService
    {
        private readonly ContainerizationDbContext _context;
        private readonly int _portRangeStart;
        private readonly int _portRangeEnd;

        public PortManagementService(ContainerizationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _portRangeStart = configuration.GetValue<int>("Docker:PortRangeStart", 6000);
            _portRangeEnd = configuration.GetValue<int>("Docker:PortRangeEnd", 6100);
        }

        public async Task<int> AssignPortAsync(string nombreModulo)
        {
            // Buscar si ya tiene un puerto asignado
            var container = await _context.MicroserviceContainers
                .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

            if (container != null && container.PuertoAsignado > 0)
            {
                return container.PuertoAsignado;
            }

            // Buscar el primer puerto disponible
            var portAssignment = await _context.PortAssignments
                .Where(p => !p.EnUso && p.Puerto >= _portRangeStart && p.Puerto <= _portRangeEnd)
                .OrderBy(p => p.Puerto)
                .FirstOrDefaultAsync();

            if (portAssignment == null)
            {
                throw new InvalidOperationException("No hay puertos disponibles en el rango configurado.");
            }

            // Marcar como en uso
            portAssignment.EnUso = true;
            portAssignment.FechaAsignacion = DateTime.UtcNow;

            if (container != null)
            {
                portAssignment.MicroserviceContainerId = container.Id;
            }

            await _context.SaveChangesAsync();

            return portAssignment.Puerto;
        }

        public async Task ReleasePortAsync(int port)
        {
            var portAssignment = await _context.PortAssignments
                .FirstOrDefaultAsync(p => p.Puerto == port);

            if (portAssignment != null && portAssignment.EnUso)
            {
                portAssignment.EnUso = false;
                portAssignment.FechaLiberacion = DateTime.UtcNow;
                portAssignment.MicroserviceContainerId = null;

                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsPortAvailableAsync(int port)
        {
            var portAssignment = await _context.PortAssignments
                .FirstOrDefaultAsync(p => p.Puerto == port);

            return portAssignment != null && !portAssignment.EnUso;
        }

        public async Task<List<int>> GetAvailablePortsAsync()
        {
            return await _context.PortAssignments
                .Where(p => !p.EnUso && p.Puerto >= _portRangeStart && p.Puerto <= _portRangeEnd)
                .Select(p => p.Puerto)
                .OrderBy(p => p)
                .ToListAsync();
        }

        public async Task<int?> GetPortByModuleAsync(string nombreModulo)
        {
            var container = await _context.MicroserviceContainers
                .FirstOrDefaultAsync(c => c.NombreModulo == nombreModulo);

            return container?.PuertoAsignado;
        }
    }
}