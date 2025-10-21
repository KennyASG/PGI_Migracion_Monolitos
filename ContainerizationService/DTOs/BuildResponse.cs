namespace ContainerizationService.DTOs
{
    public class BuildResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ImageName { get; set; }
        public int PuertoAsignado { get; set; }
        public string DockerfilePath { get; set; }
        public string DockerComposePath { get; set; }
        public string BuildOutput { get; set; }
    }
}