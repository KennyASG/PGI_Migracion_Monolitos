using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PGI_Migracion_Monolitos.DTOs
{
    public class UploadRequest
    {
        [FromForm]
        public IFormFile File { get; set; }

        [FromForm]
        public string ProjectName { get; set; }
    }
}