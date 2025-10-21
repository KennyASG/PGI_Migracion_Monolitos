using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContainerizationService.Services
{
    public interface IPortManagementService
    {
        Task<int> AssignPortAsync(string nombreModulo);
        Task ReleasePortAsync(int port);
        Task<bool> IsPortAvailableAsync(int port);
        Task<List<int>> GetAvailablePortsAsync();
        Task<int?> GetPortByModuleAsync(string nombreModulo);
    }
}