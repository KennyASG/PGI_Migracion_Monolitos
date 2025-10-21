using System.Threading.Tasks;

namespace ContainerizationService.Services
{
    public enum ContainerStatus
    {
        Running,
        Stopped,
        NotFound,
        Error
    }

    public interface IDockerService
    {
        Task<string> BuildImageAsync(string rutaMicroservicio, string imageName);
        Task<string> RunContainerAsync(string imageName, string containerName, int hostPort, int containerPort);
        Task StopContainerAsync(string containerId);
        Task RemoveContainerAsync(string containerId);
        Task<string> GetContainerLogsAsync(string containerId, int tailLines = 100);
        Task<ContainerStatus> GetContainerStatusAsync(string containerId);
        Task<bool> IsDockerAvailableAsync();
        Task<string> InspectContainerAsync(string containerId);
    }
}