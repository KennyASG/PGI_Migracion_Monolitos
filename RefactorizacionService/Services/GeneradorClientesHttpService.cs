using RefactorizacionService.DTOs;
using System.Text;

namespace RefactorizacionService.Services
{
    public class GeneradorClientesHttpService : IGeneradorClientesHttpService
    {
        private readonly IConfiguration _configuration;

        public GeneradorClientesHttpService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<GenerarClientesHttpResponseDto> GenerarClienteHttpAsync(
            string servicioDestino, 
            string urlMicroservicio, 
            string tipoCliente)
        {
            var metodos = new List<string> 
            { 
                "GetAll", 
                "GetById", 
                "Create", 
                "Update", 
                "Delete" 
            };

            var nombreInterfaz = $"I{servicioDestino}Client";
            var nombreImplementacion = $"{servicioDestino}Client";

            var codigoInterfaz = await GenerarInterfazClienteAsync(servicioDestino, metodos);
            
            var codigoImplementacion = tipoCliente.ToLower() == "refit"
                ? await GenerarImplementacionClienteRefitAsync(servicioDestino, urlMicroservicio)
                : await GenerarImplementacionClienteNativoAsync(servicioDestino, urlMicroservicio, metodos);

            return new GenerarClientesHttpResponseDto
            {
                NombreInterfaz = nombreInterfaz,
                CodigoInterfaz = codigoInterfaz,
                NombreClaseImplementacion = nombreImplementacion,
                CodigoImplementacion = codigoImplementacion,
                RutaArchivos = $"Clients/{servicioDestino}",
                MetodosGenerados = metodos
            };
        }

        public async Task<string> GenerarInterfazClienteAsync(
            string nombreServicio, 
            List<string> metodos)
        {
            var sb = new StringBuilder();
            var entidad = nombreServicio.TrimEnd('s'); // Users -> User

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace MonolithPro.Clients.{nombreServicio}");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface I{nombreServicio}Client");
            sb.AppendLine("    {");

            foreach (var metodo in metodos)
            {
                switch (metodo)
                {
                    case "GetAll":
                        sb.AppendLine($"        Task<List<{entidad}Dto>> GetAllAsync();");
                        break;
                    case "GetById":
                        sb.AppendLine($"        Task<{entidad}Dto> GetByIdAsync(int id);");
                        break;
                    case "Create":
                        sb.AppendLine($"        Task<{entidad}Dto> CreateAsync({entidad}Dto entity);");
                        break;
                    case "Update":
                        sb.AppendLine($"        Task<{entidad}Dto> UpdateAsync(int id, {entidad}Dto entity);");
                        break;
                    case "Delete":
                        sb.AppendLine($"        Task<bool> DeleteAsync(int id);");
                        break;
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerarImplementacionClienteNativoAsync(
            string nombreServicio, 
            string urlBase, 
            List<string> metodos)
        {
            var sb = new StringBuilder();
            var entidad = nombreServicio.TrimEnd('s'); // Users -> User

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Net.Http.Json;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace MonolithPro.Clients.{nombreServicio}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {nombreServicio}Client : I{nombreServicio}Client");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly HttpClient _httpClient;");
            sb.AppendLine("        private readonly string _baseUrl;");
            sb.AppendLine();
            sb.AppendLine($"        public {nombreServicio}Client(HttpClient httpClient)");
            sb.AppendLine("        {");
            sb.AppendLine("            _httpClient = httpClient;");
            sb.AppendLine($"            _baseUrl = \"{urlBase}\";");
            sb.AppendLine("        }");
            sb.AppendLine();

            foreach (var metodo in metodos)
            {
                switch (metodo)
                {
                    case "GetAll":
                        sb.AppendLine($"        public async Task<List<{entidad}Dto>> GetAllAsync()");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            var response = await _httpClient.GetAsync($\"{{_baseUrl}}/api/{nombreServicio.ToLower()}\");");
                        sb.AppendLine("            response.EnsureSuccessStatusCode();");
                        sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<List<{entidad}Dto>>();");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                        break;

                    case "GetById":
                        sb.AppendLine($"        public async Task<{entidad}Dto> GetByIdAsync(int id)");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            var response = await _httpClient.GetAsync($\"{{_baseUrl}}/api/{nombreServicio.ToLower()}/{{id}}\");");
                        sb.AppendLine("            response.EnsureSuccessStatusCode();");
                        sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<{entidad}Dto>();");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                        break;

                    case "Create":
                        sb.AppendLine($"        public async Task<{entidad}Dto> CreateAsync({entidad}Dto entity)");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            var response = await _httpClient.PostAsJsonAsync($\"{{_baseUrl}}/api/{nombreServicio.ToLower()}\", entity);");
                        sb.AppendLine("            response.EnsureSuccessStatusCode();");
                        sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<{entidad}Dto>();");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                        break;

                    case "Update":
                        sb.AppendLine($"        public async Task<{entidad}Dto> UpdateAsync(int id, {entidad}Dto entity)");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            var response = await _httpClient.PutAsJsonAsync($\"{{_baseUrl}}/api/{nombreServicio.ToLower()}/{{id}}\", entity);");
                        sb.AppendLine("            response.EnsureSuccessStatusCode();");
                        sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<{entidad}Dto>();");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                        break;

                    case "Delete":
                        sb.AppendLine($"        public async Task<bool> DeleteAsync(int id)");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            var response = await _httpClient.DeleteAsync($\"{{_baseUrl}}/api/{nombreServicio.ToLower()}/{{id}}\");");
                        sb.AppendLine("            return response.IsSuccessStatusCode;");
                        sb.AppendLine("        }");
                        sb.AppendLine();
                        break;
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerarImplementacionClienteRefitAsync(
            string nombreServicio, 
            string urlBase)
        {
            var sb = new StringBuilder();
            var entidad = nombreServicio.TrimEnd('s');

            sb.AppendLine("using Refit;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace MonolithPro.Clients.{nombreServicio}");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface I{nombreServicio}Client");
            sb.AppendLine("    {");
            sb.AppendLine($"        [Get(\"/api/{nombreServicio.ToLower()}\")]");
            sb.AppendLine($"        Task<List<{entidad}Dto>> GetAllAsync();");
            sb.AppendLine();
            sb.AppendLine($"        [Get(\"/api/{nombreServicio.ToLower()}/{{id}}\")]");
            sb.AppendLine($"        Task<{entidad}Dto> GetByIdAsync(int id);");
            sb.AppendLine();
            sb.AppendLine($"        [Post(\"/api/{nombreServicio.ToLower()}\")]");
            sb.AppendLine($"        Task<{entidad}Dto> CreateAsync([Body] {entidad}Dto entity);");
            sb.AppendLine();
            sb.AppendLine($"        [Put(\"/api/{nombreServicio.ToLower()}/{{id}}\")]");
            sb.AppendLine($"        Task<{entidad}Dto> UpdateAsync(int id, [Body] {entidad}Dto entity);");
            sb.AppendLine();
            sb.AppendLine($"        [Delete(\"/api/{nombreServicio.ToLower()}/{{id}}\")]");
            sb.AppendLine($"        Task<bool> DeleteAsync(int id);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }
    }
}