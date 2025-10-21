using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ContainerizationService.Services
{
    public class DockerService : IDockerService
    {
        private readonly IConfiguration _configuration;
        private readonly int _buildTimeout;
        private readonly int _startTimeout;

        public DockerService(IConfiguration configuration)
        {
            _configuration = configuration;
            _buildTimeout = configuration.GetValue<int>("Docker:BuildTimeout", 300);
            _startTimeout = configuration.GetValue<int>("Docker:StartTimeout", 60);
        }

        public async Task<bool> IsDockerAvailableAsync()
        {
            try
            {
                var result = await ExecuteDockerCommandAsync("--version", timeoutSeconds: 5);
                return result.Success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> BuildImageAsync(string rutaMicroservicio, string imageName)
        {
            var command = $"build -t {imageName}:latest \"{rutaMicroservicio}\"";
            var result = await ExecuteDockerCommandAsync(command, timeoutSeconds: _buildTimeout);

            if (!result.Success)
            {
                throw new Exception($"Error al construir la imagen: {result.Error}");
            }

            return result.Output;
        }

        public async Task<string> RunContainerAsync(string imageName, string containerName, int hostPort, int containerPort)
        {
            var command = $"run -d --name {containerName} -p {hostPort}:{containerPort} " +
                         $"-e ASPNETCORE_ENVIRONMENT=Development " +
                         $"-e ASPNETCORE_URLS=http://+:{containerPort} " +
                         $"{imageName}:latest";

            var result = await ExecuteDockerCommandAsync(command, timeoutSeconds: _startTimeout);

            if (!result.Success)
            {
                throw new Exception($"Error al ejecutar el contenedor: {result.Error}");
            }

            return result.Output.Trim();
        }

        public async Task StopContainerAsync(string containerId)
        {
            var command = $"stop {containerId}";
            var result = await ExecuteDockerCommandAsync(command, timeoutSeconds: 30);

            if (!result.Success)
            {
                throw new Exception($"Error al detener el contenedor: {result.Error}");
            }
        }

        public async Task RemoveContainerAsync(string containerId)
        {
            var command = $"rm -f {containerId}";
            var result = await ExecuteDockerCommandAsync(command, timeoutSeconds: 30);

            if (!result.Success)
            {
                throw new Exception($"Error al eliminar el contenedor: {result.Error}");
            }
        }

        public async Task<string> GetContainerLogsAsync(string containerId, int tailLines = 100)
        {
            var command = $"logs --tail {tailLines} {containerId}";
            var result = await ExecuteDockerCommandAsync(command, timeoutSeconds: 10);

            return result.Success ? result.Output : result.Error;
        }

        public async Task<ContainerStatus> GetContainerStatusAsync(string containerId)
        {
            try
            {
                var command = $"inspect --format=\"{{{{.State.Status}}}}\" {containerId}";
                var result = await ExecuteDockerCommandAsync(command, timeoutSeconds: 5);

                if (!result.Success)
                {
                    return ContainerStatus.NotFound;
                }

                var status = result.Output.Trim().ToLower();

                return status switch
                {
                    "running" => ContainerStatus.Running,
                    "exited" => ContainerStatus.Stopped,
                    "created" => ContainerStatus.Stopped,
                    _ => ContainerStatus.Error
                };
            }
            catch
            {
                return ContainerStatus.Error;
            }
        }

        public async Task<string> InspectContainerAsync(string containerId)
        {
            var command = $"inspect {containerId}";
            var result = await ExecuteDockerCommandAsync(command, timeoutSeconds: 10);

            return result.Success ? result.Output : result.Error;
        }

        private async Task<DockerCommandResult> ExecuteDockerCommandAsync(string arguments, int timeoutSeconds = 30)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            using var process = new Process { StartInfo = processStartInfo };

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    error.AppendLine(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));

            if (!completed)
            {
                try
                {
                    process.Kill();
                }
                catch { }

                return new DockerCommandResult
                {
                    Success = false,
                    Error = $"Command timed out after {timeoutSeconds} seconds"
                };
            }

            return new DockerCommandResult
            {
                Success = process.ExitCode == 0,
                Output = output.ToString(),
                Error = error.ToString()
            };
        }

        private class DockerCommandResult
        {
            public bool Success { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }
    }
}