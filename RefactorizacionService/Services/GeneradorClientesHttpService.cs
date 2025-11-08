using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var metodos = await ExtraerMetodosDelControllerAsync(servicioDestino);

            if (!metodos.Any())
            {
                throw new InvalidOperationException($"No se encontraron métodos en el controller de {servicioDestino}");
            }

            var nombreInterfaz = $"I{servicioDestino}Client";
            var nombreImplementacion = $"{servicioDestino}Client";

            var codigoInterfaz = await GenerarInterfazClienteAsync(servicioDestino, metodos);
            
            var codigoImplementacion = tipoCliente.ToLower() == "refit"
                ? await GenerarImplementacionClienteRefitAsync(servicioDestino, urlMicroservicio, metodos)
                : await GenerarImplementacionClienteNativoAsync(servicioDestino, urlMicroservicio, metodos);

            return new GenerarClientesHttpResponseDto
            {
                NombreInterfaz = nombreInterfaz,
                CodigoInterfaz = codigoInterfaz,
                NombreClaseImplementacion = nombreImplementacion,
                CodigoImplementacion = codigoImplementacion,
                RutaArchivos = $"Clients/{servicioDestino}",
                MetodosGenerados = metodos.Select(m => m.NombreMetodo).ToList()
            };
        }

        private async Task<List<MetodoControllerDto>> ExtraerMetodosDelControllerAsync(string servicioDestino)
        {
            var metodos = new List<MetodoControllerDto>();

            var microserviciosFolder = _configuration["Paths:MicroserviciosGenerados"];
            var rutaController = Path.Combine(microserviciosFolder, servicioDestino, "Controllers", $"{servicioDestino}Controller.cs");

            if (!File.Exists(rutaController))
            {
                throw new FileNotFoundException($"Controller no encontrado en: {rutaController}");
            }

            var codigoController = await File.ReadAllTextAsync(rutaController);
            var arbol = CSharpSyntaxTree.ParseText(codigoController);
            var raiz = await arbol.GetRootAsync();

            var claseController = raiz.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text.Contains("Controller"));

            if (claseController == null)
                return metodos;

            var metodosController = claseController.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var metodo in metodosController)
            {
                var atributos = metodo.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .ToList();

                var httpMethodAttr = atributos.FirstOrDefault(a =>
                    a.Name.ToString().StartsWith("Http"));

                if (httpMethodAttr == null)
                    continue;

                var httpMethod = ExtraerHttpMethod(httpMethodAttr.Name.ToString());
                var route = ExtraerRoute(httpMethodAttr);
                var nombreMetodo = metodo.Identifier.Text;
                var tipoRetorno = ExtraerTipoRetorno(metodo.ReturnType.ToString());
                var parametros = ExtraerParametros(metodo.ParameterList);

                metodos.Add(new MetodoControllerDto
                {
                    NombreMetodo = nombreMetodo,
                    HttpMethod = httpMethod,
                    Route = route,
                    TipoRetorno = tipoRetorno,
                    Parametros = parametros
                });
            }

            return metodos;
        }

        private string ExtraerHttpMethod(string atributoNombre)
        {
            if (atributoNombre.Contains("Get")) return "GET";
            if (atributoNombre.Contains("Post")) return "POST";
            if (atributoNombre.Contains("Put")) return "PUT";
            if (atributoNombre.Contains("Delete")) return "DELETE";
            if (atributoNombre.Contains("Patch")) return "PATCH";
            return "GET";
        }

        private string ExtraerRoute(AttributeSyntax atributo)
        {
            if (atributo.ArgumentList == null || !atributo.ArgumentList.Arguments.Any())
                return "";

            var primerArgumento = atributo.ArgumentList.Arguments.First();
            return primerArgumento.Expression.ToString().Trim('"');
        }

        private string ExtraerTipoRetorno(string tipoRetornoCompleto)
        {
            if (tipoRetornoCompleto.Contains("Task<") || tipoRetornoCompleto.Contains("ActionResult<"))
            {
                var inicio = tipoRetornoCompleto.IndexOf('<') + 1;
                var fin = tipoRetornoCompleto.LastIndexOf('>');
                if (inicio > 0 && fin > inicio)
                {
                    return tipoRetornoCompleto.Substring(inicio, fin - inicio).Trim();
                }
            }
            return tipoRetornoCompleto;
        }

        private List<ParametroMetodoDto> ExtraerParametros(ParameterListSyntax parameterList)
        {
            var parametros = new List<ParametroMetodoDto>();

            foreach (var param in parameterList.Parameters)
            {
                var esFromBody = param.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "FromBody");

                var esFromRoute = param.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "FromRoute");

                parametros.Add(new ParametroMetodoDto
                {
                    Nombre = param.Identifier.Text,
                    Tipo = param.Type?.ToString() ?? "object",
                    EsFromBody = esFromBody,
                    EsFromRoute = esFromRoute || (!esFromBody && param.Type?.ToString() != "int" && param.Type?.ToString() != "string")
                });
            }

            return parametros;
        }

        public async Task<string> GenerarInterfazClienteAsync(
            string nombreServicio, 
            List<MetodoControllerDto> metodos)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace MonolithPro.Clients.{nombreServicio}");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface I{nombreServicio}Client");
            sb.AppendLine("    {");

            foreach (var metodo in metodos)
            {
                var parametrosStr = string.Join(", ", metodo.Parametros.Select(p => $"{p.Tipo} {p.Nombre}"));
                sb.AppendLine($"        Task<{metodo.TipoRetorno}> {metodo.NombreMetodo}Async({parametrosStr});");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public async Task<string> GenerarImplementacionClienteNativoAsync(
            string nombreServicio, 
            string urlBase, 
            List<MetodoControllerDto> metodos)
        {
            var sb = new StringBuilder();

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
                var parametrosStr = string.Join(", ", metodo.Parametros.Select(p => $"{p.Tipo} {p.Nombre}"));
                
                sb.AppendLine($"        public async Task<{metodo.TipoRetorno}> {metodo.NombreMetodo}Async({parametrosStr})");
                sb.AppendLine("        {");

                var routeConParametros = ConstruirRutaConParametros(metodo);
                
                switch (metodo.HttpMethod.ToUpper())
                {
                    case "GET":
                        sb.AppendLine($"            var response = await _httpClient.GetAsync($\"{{_baseUrl}}{routeConParametros}\");");
                        sb.AppendLine("            response.EnsureSuccessStatusCode();");
                        sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<{metodo.TipoRetorno}>();");
                        break;

                    case "POST":
                        var bodyParam = metodo.Parametros.FirstOrDefault(p => p.EsFromBody);
                        if (bodyParam != null)
                        {
                            sb.AppendLine($"            var response = await _httpClient.PostAsJsonAsync($\"{{_baseUrl}}{routeConParametros}\", {bodyParam.Nombre});");
                        }
                        else
                        {
                            sb.AppendLine($"            var response = await _httpClient.PostAsync($\"{{_baseUrl}}{routeConParametros}\", null);");
                        }
                        sb.AppendLine("            response.EnsureSuccessStatusCode();");
                        sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<{metodo.TipoRetorno}>();");
                        break;

                    case "PUT":
                        var bodyParamPut = metodo.Parametros.FirstOrDefault(p => p.EsFromBody);
                        if (bodyParamPut != null)
                        {
                            sb.AppendLine($"            var response = await _httpClient.PutAsJsonAsync($\"{{_baseUrl}}{routeConParametros}\", {bodyParamPut.Nombre});");
                        }
                        else
                        {
                            sb.AppendLine($"            var response = await _httpClient.PutAsync($\"{{_baseUrl}}{routeConParametros}\", null);");
                        }
                        sb.AppendLine("            response.EnsureSuccessStatusCode();");
                        sb.AppendLine($"            return await response.Content.ReadFromJsonAsync<{metodo.TipoRetorno}>();");
                        break;

                    case "DELETE":
                        sb.AppendLine($"            var response = await _httpClient.DeleteAsync($\"{{_baseUrl}}{routeConParametros}\");");
                        sb.AppendLine("            return response.IsSuccessStatusCode;");
                        break;
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        private string ConstruirRutaConParametros(MetodoControllerDto metodo)
        {
            var ruta = metodo.Route;
            
            foreach (var param in metodo.Parametros.Where(p => !p.EsFromBody))
            {
                if (ruta.Contains($"{{{param.Nombre}}}"))
                {
                    ruta = ruta.Replace($"{{{param.Nombre}}}", $"{{{param.Nombre}}}");
                }
            }

            return ruta;
        }

        public async Task<string> GenerarImplementacionClienteRefitAsync(
            string nombreServicio, 
            string urlBase,
            List<MetodoControllerDto> metodos)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using Refit;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace MonolithPro.Clients.{nombreServicio}");
            sb.AppendLine("{");
            sb.AppendLine($"    public interface I{nombreServicio}Client");
            sb.AppendLine("    {");

            foreach (var metodo in metodos)
            {
                var httpMethodAttr = metodo.HttpMethod switch
                {
                    "GET" => "Get",
                    "POST" => "Post",
                    "PUT" => "Put",
                    "DELETE" => "Delete",
                    _ => "Get"
                };

                sb.AppendLine($"        [{httpMethodAttr}(\"{metodo.Route}\")]");
                
                var parametrosStr = string.Join(", ", metodo.Parametros.Select(p =>
                {
                    if (p.EsFromBody)
                        return $"[Body] {p.Tipo} {p.Nombre}";
                    return $"{p.Tipo} {p.Nombre}";
                }));

                sb.AppendLine($"        Task<{metodo.TipoRetorno}> {metodo.NombreMetodo}Async({parametrosStr});");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return await Task.FromResult(sb.ToString());
        }

        public Task<string> GenerarInterfazClienteAsync(string nombreServicio, List<string> metodos)
        {
            throw new NotImplementedException("Usar sobrecarga con List<MetodoControllerDto>");
        }

        public Task<string> GenerarImplementacionClienteNativoAsync(string nombreServicio, string urlBase, List<string> metodos)
        {
            throw new NotImplementedException("Usar sobrecarga con List<MetodoControllerDto>");
        }

        public Task<string> GenerarImplementacionClienteRefitAsync(string nombreServicio, string urlBase)
        {
            throw new NotImplementedException("Usar sobrecarga con List<MetodoControllerDto>");
        }
    }

    public class MetodoControllerDto
    {
        public string NombreMetodo { get; set; }
        public string HttpMethod { get; set; }
        public string Route { get; set; }
        public string TipoRetorno { get; set; }
        public List<ParametroMetodoDto> Parametros { get; set; } = new();
    }

    public class ParametroMetodoDto
    {
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public bool EsFromBody { get; set; }
        public bool EsFromRoute { get; set; }
    }
}