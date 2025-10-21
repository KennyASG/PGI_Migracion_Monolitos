using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ContainerizationService.Services
{
    public class DockerComposeService : IDockerComposeService
    {
        public async Task<string> GenerateDockerfileAsync(string rutaMicroservicio, string nombreModulo, int port)
        {
            var dockerfileContent = $@"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE {port}

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""{nombreModulo}.csproj"", ""./""]
RUN dotnet restore ""{nombreModulo}.csproj""
COPY . .
RUN dotnet build ""{nombreModulo}.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""{nombreModulo}.csproj"" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{nombreModulo}.dll""]";

            var dockerfilePath = Path.Combine(rutaMicroservicio, "Dockerfile");
            await File.WriteAllTextAsync(dockerfilePath, dockerfileContent);

            return dockerfilePath;
        }

        public async Task<string> GenerateDockerComposeAsync(string rutaMicroservicio, string nombreModulo, int hostPort, int containerPort)
        {
            var serviceName = nombreModulo.ToLower().Replace(" ", "-");
            var imageName = $"{serviceName}";
            var containerName = $"{serviceName}-container";

            var composeContent = $@"version: '3.8'

services:
  {serviceName}:
    image: {imageName}:latest
    container_name: {containerName}
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - ""{hostPort}:{containerPort}""
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:{containerPort}
    restart: unless-stopped
    networks:
      - default

networks:
  default:
    driver: bridge";

            var composeFilePath = Path.Combine(rutaMicroservicio, "docker-compose.yml");
            await File.WriteAllTextAsync(composeFilePath, composeContent);

            return composeFilePath;
        }

        public async Task<string> ComposeUpAsync(string composeFilePath)
        {
            var workingDirectory = Path.GetDirectoryName(composeFilePath);
            var command = "up -d --build";

            var result = await ExecuteDockerComposeCommandAsync(command, workingDirectory, timeoutSeconds: 300);

            if (!result.Success)
            {
                throw new Exception($"Error al ejecutar docker-compose up: {result.Error}");
            }

            return result.Output;
        }

        public async Task ComposeDownAsync(string composeFilePath)
        {
            var workingDirectory = Path.GetDirectoryName(composeFilePath);
            var command = "down";

            var result = await ExecuteDockerComposeCommandAsync(command, workingDirectory, timeoutSeconds: 60);

            if (!result.Success)
            {
                throw new Exception($"Error al ejecutar docker-compose down: {result.Error}");
            }
        }

        public async Task<string> ComposeLogsAsync(string composeFilePath, string serviceName, int tailLines = 100)
        {
            var workingDirectory = Path.GetDirectoryName(composeFilePath);
            var command = $"logs --tail {tailLines} {serviceName}";

            var result = await ExecuteDockerComposeCommandAsync(command, workingDirectory, timeoutSeconds: 10);

            return result.Success ? result.Output : result.Error;
        }

        public async Task<string> ComposePsAsync(string composeFilePath)
        {
            var workingDirectory = Path.GetDirectoryName(composeFilePath);
            var command = "ps";

            var result = await ExecuteDockerComposeCommandAsync(command, workingDirectory, timeoutSeconds: 10);

            return result.Success ? result.Output : result.Error;
        }

        private async Task<DockerComposeCommandResult> ExecuteDockerComposeCommandAsync(
            string arguments,
            string workingDirectory,
            int timeoutSeconds = 30)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "docker-compose",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
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

                return new DockerComposeCommandResult
                {
                    Success = false,
                    Error = $"Command timed out after {timeoutSeconds} seconds"
                };
            }

            return new DockerComposeCommandResult
            {
                Success = process.ExitCode == 0,
                Output = output.ToString(),
                Error = error.ToString()
            };
        }

        private class DockerComposeCommandResult
        {
            public bool Success { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }
    }
}