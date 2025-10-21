using System.Threading.Tasks;

namespace ContainerizationService.Services
{
    public interface IDockerComposeService
    {
        Task<string> GenerateDockerfileAsync(string rutaMicroservicio, string nombreModulo, int port);
        Task<string> GenerateDockerComposeAsync(string rutaMicroservicio, string nombreModulo, int hostPort, int containerPort);
        Task<string> ComposeUpAsync(string composeFilePath);
        Task ComposeDownAsync(string composeFilePath);
        Task<string> ComposeLogsAsync(string composeFilePath, string serviceName, int tailLines = 100);
        Task<string> ComposePsAsync(string composeFilePath);
    }
}