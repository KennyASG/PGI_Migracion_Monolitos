namespace ContainerizationService.DTOs
{
    public class DeployResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ContenedorId { get; set; }
        public string Url { get; set; }
        public int Puerto { get; set; }
        public string ImageName { get; set; }
        public string DeployOutput { get; set; }
    }
}