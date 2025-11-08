using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RefactorizacionService.DTOs;
using System.Text;

namespace RefactorizacionService.Services
{
    public class RefactorizadorCodigoService : IRefactorizadorCodigoService
    {
        private readonly IConfiguration _configuration;
        private readonly IRefactorizacionHistorialService _historialService;
        private readonly ILogger<RefactorizadorCodigoService> _logger;

        public RefactorizadorCodigoService(
            IConfiguration configuration,
            IRefactorizacionHistorialService historialService,
            ILogger<RefactorizadorCodigoService> logger)
        {
            _configuration = configuration;
            _historialService = historialService;
            _logger = logger;
        }

        public async Task<AplicarRefactorizacionResponseDto> RefactorizarModuloAsync(
            AplicarRefactorizacionRequestDto request)
        {
            // Buscar en la carpeta local del RefactorizacionService
            var localMonolithosFolder = Path.Combine(Directory.GetCurrentDirectory(), "Monolithos");
            var rutaProyecto = Path.Combine(localMonolithosFolder, request.NombreProyecto, request.NombreProyecto);

            if (!Directory.Exists(rutaProyecto))
                throw new DirectoryNotFoundException($"Proyecto {request.NombreProyecto} no encontrado en {localMonolithosFolder}. Por favor descomprímalo manualmente.");

            var rutaModulo = Path.Combine(rutaProyecto, "Modules", request.ModuloARefactorizar);

            if (!Directory.Exists(rutaModulo))
                throw new DirectoryNotFoundException($"Módulo {request.ModuloARefactorizar} no encontrado en {rutaProyecto}");

            var historial = await _historialService.CrearHistorialAsync(
                request.NombreProyecto,
                request.ModuloARefactorizar);

            var cambiosRealizados = new List<CambioRealizadoDto>();
            var archivosModificados = new List<ArchivoModificadoDto>();
            var todosLosDiffs = new StringBuilder();

            try
            {
                var archivosCs = Directory.GetFiles(rutaModulo, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\obj\\") && !f.Contains("\\bin\\"))
                    .ToList();

                foreach (var archivo in archivosCs)
                {
                    var codigoOriginal = await File.ReadAllTextAsync(archivo);
                    var codigoRefactorizado = codigoOriginal;
                    var cambiosEnArchivo = 0;

                    foreach (var dependencia in request.Dependencias)
                    {
                        codigoRefactorizado = await ReemplazarInvocacionDirectaPorHttpAsync(
                            codigoRefactorizado,
                            dependencia.ServicioOriginal,
                            dependencia.UrlMicroservicio);
                    }

                    if (codigoOriginal != codigoRefactorizado)
                    {
                        var diff = await GenerarDiffAsync(codigoOriginal, codigoRefactorizado);
                        todosLosDiffs.AppendLine($"=== {Path.GetFileName(archivo)} ===");
                        todosLosDiffs.AppendLine(diff);
                        todosLosDiffs.AppendLine();

                        cambiosRealizados.Add(new CambioRealizadoDto
                        {
                            NombreArchivo = Path.GetFileName(archivo),
                            NombreClase = ExtraerNombreClase(codigoOriginal),
                            NombreMetodo = "Multiple",
                            CodigoAntes = codigoOriginal.Substring(0, Math.Min(500, codigoOriginal.Length)),
                            CodigoDespues = codigoRefactorizado.Substring(0, Math.Min(500, codigoRefactorizado.Length)),
                            LineaInicio = 1,
                            LineaFin = codigoRefactorizado.Split('\n').Length
                        });

                        cambiosEnArchivo++;

                        if (!request.GenerarSoloPreview)
                        {
                            await GuardarCodigoRefactorizadoAsync(archivo, codigoRefactorizado);
                        }

                        archivosModificados.Add(new ArchivoModificadoDto
                        {
                            RutaArchivo = archivo,
                            NombreArchivo = Path.GetFileName(archivo),
                            CantidadCambios = cambiosEnArchivo
                        });
                    }
                }

                var estado = cambiosRealizados.Any() ? "Completado" : "Sin cambios";
                await _historialService.ActualizarHistorialAsync(
                    historial.Id,
                    estado,
                    todosLosDiffs.ToString());

                return new AplicarRefactorizacionResponseDto
                {
                    Exitoso = true,
                    Mensaje = request.GenerarSoloPreview
                        ? "Preview generado exitosamente"
                        : $"Refactorización completada. {cambiosRealizados.Count} cambios realizados.",
                    RefactorizacionHistorialId = historial.Id,
                    CambiosRealizados = cambiosRealizados,
                    CodigoDiff = todosLosDiffs.ToString(),
                    ArchivosModificados = archivosModificados,
                    RutaCodigoRefactorizado = request.GenerarSoloPreview ? null : rutaModulo
                };
            }
            catch (Exception ex)
            {
                await _historialService.ActualizarHistorialAsync(
                    historial.Id,
                    "Error",
                    null,
                    ex.Message);

                return new AplicarRefactorizacionResponseDto
                {
                    Exitoso = false,
                    Mensaje = $"Error durante la refactorización: {ex.Message}",
                    RefactorizacionHistorialId = historial.Id,
                    CambiosRealizados = new List<CambioRealizadoDto>(),
                    ArchivosModificados = new List<ArchivoModificadoDto>()
                };
            }
        }

        public async Task<string> ReemplazarInvocacionDirectaPorHttpAsync(
            string codigoOriginal,
            string servicioOriginal,
            string urlMicroservicio)
        {
            var codigoModificado = codigoOriginal;

            // 1. Agregar using para System.Net.Http.Json si no existe
            if (!codigoModificado.Contains("using System.Net.Http.Json;"))
            {
                var primerUsing = codigoModificado.IndexOf("using ");
                if (primerUsing >= 0)
                {
                    codigoModificado = codigoModificado.Insert(primerUsing, "using System.Net.Http.Json;\n");
                }
            }

            // 2. Identificar el nombre del campo del servicio
            var patronCampo = $"private readonly {servicioOriginal} ";
            var indiceCampo = codigoModificado.IndexOf(patronCampo);
            
            if (indiceCampo == -1)
                return codigoOriginal; // No se encontró el servicio

            // Extraer nombre del campo (ej: _userService)
            var inicioCampo = indiceCampo + patronCampo.Length;
            var finCampo = codigoModificado.IndexOf(";", inicioCampo);
            var nombreCampo = codigoModificado.Substring(inicioCampo, finCampo - inicioCampo).Trim();

            // 3. Agregar campo IHttpClientFactory después de los campos existentes
            if (!codigoModificado.Contains("private readonly IHttpClientFactory"))
            {
                var ultimoCampo = codigoModificado.LastIndexOf("private readonly");
                var finUltimoCampo = codigoModificado.IndexOf(";", ultimoCampo) + 1;
                
                codigoModificado = codigoModificado.Insert(finUltimoCampo, 
                    "\n    private readonly IHttpClientFactory _httpClientFactory;");
            }

            // 4. Agregar parámetro IHttpClientFactory al constructor
            var patronConstructor = "public " + ObtenerNombreClase(codigoOriginal) + "(";
            var indiceConstructor = codigoModificado.IndexOf(patronConstructor);
            
            if (indiceConstructor >= 0 && !codigoModificado.Contains("IHttpClientFactory httpClientFactory"))
            {
                var finParametros = codigoModificado.IndexOf(")", indiceConstructor);
                codigoModificado = codigoModificado.Insert(finParametros, 
                    ",\n        IHttpClientFactory httpClientFactory");
                
                // Agregar asignación en el cuerpo del constructor
                var inicioCuerpo = codigoModificado.IndexOf("{", indiceConstructor) + 1;
                codigoModificado = codigoModificado.Insert(inicioCuerpo, 
                    "\n        _httpClientFactory = httpClientFactory;");
            }

            // 5. Reemplazar invocaciones específicas de UserService
            codigoModificado = ReemplazarGetUserById(codigoModificado, nombreCampo, urlMicroservicio);
            codigoModificado = ReemplazarUserExists(codigoModificado, nombreCampo, urlMicroservicio);

            return await Task.FromResult(codigoModificado);
        }

        private string ObtenerNombreClase(string codigo)
        {
            var patron = "public class ";
            var inicio = codigo.IndexOf(patron);
            if (inicio < 0) return "";
            
            inicio += patron.Length;
            var fin = codigo.IndexOfAny(new[] { '\n', '\r', ' ', '{' }, inicio);
            return codigo.Substring(inicio, fin - inicio).Trim();
        }

        private string ReemplazarGetUserById(string codigo, string nombreCampo, string urlMicroservicio)
        {
            // Buscar patrón: var user = _userService.GetUserById(userId);
            var patron = $"var user = {nombreCampo}.GetUserById(";
            var indice = codigo.IndexOf(patron);
            
            if (indice < 0) return codigo;

            // Extraer el parámetro
            var inicioParam = indice + patron.Length;
            var finParam = codigo.IndexOf(")", inicioParam);
            var parametro = codigo.Substring(inicioParam, finParam - inicioParam).Trim();

            // Encontrar el método que contiene esta invocación
            var metodoInicio = codigo.LastIndexOf("public ", indice);
            var metodoFirma = codigo.Substring(metodoInicio, codigo.IndexOf("\n", metodoInicio) - metodoInicio);

            // Hacer el método async si no lo es
            if (!metodoFirma.Contains("async"))
            {
                codigo = codigo.Replace(metodoFirma, metodoFirma.Replace("public ", "public async "));
                
                // Cambiar tipo de retorno
                if (metodoFirma.Contains("Order CreateOrder"))
                    codigo = codigo.Replace("Order CreateOrder", "Task<Order> CreateOrder");
                else if (metodoFirma.Contains("List<Order> GetOrdersByUserId"))
                    codigo = codigo.Replace("List<Order> GetOrdersByUserId", "Task<List<Order>> GetOrdersByUserId");
            }

            // Reemplazar la invocación directa por HTTP
            var invocacionOriginal = $"var user = {nombreCampo}.GetUserById({parametro});";
            var invocacionHttp = $@"
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync($""{urlMicroservicio}/api/users/{{{parametro}}}"");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($""User with ID {{{parametro}}} does not exist"");
        var user = await response.Content.ReadFromJsonAsync<User>();";

            codigo = codigo.Replace(invocacionOriginal, invocacionHttp);

            // Manejar el if (user == null) que ya no es necesario con HTTP
            var checkNull = @"if (user == null)
        {
            _logger.LogError($""Cannot create order: User {userId} not found"");
            throw new InvalidOperationException($""User with ID {userId} does not exist"");
        }";
            codigo = codigo.Replace(checkNull, "        // User validation done via HTTP call");

            return codigo;
        }

        private string ReemplazarUserExists(string codigo, string nombreCampo, string urlMicroservicio)
        {
            // Buscar patrón: if (!_userService.UserExists(userId))
            var patron = $"if (!{nombreCampo}.UserExists(";
            var indice = codigo.IndexOf(patron);
            
            if (indice < 0) return codigo;

            // Extraer el parámetro
            var inicioParam = indice + patron.Length;
            var finParam = codigo.IndexOf(")", inicioParam);
            var parametro = codigo.Substring(inicioParam, finParam - inicioParam).Trim();

            // Encontrar el método que contiene esta invocación y hacerlo async si no lo es
            var metodoInicio = codigo.LastIndexOf("public ", indice);
            var metodoFirma = codigo.Substring(metodoInicio, codigo.IndexOf("\n", metodoInicio) - metodoInicio);

            if (!metodoFirma.Contains("async"))
            {
                codigo = codigo.Replace(metodoFirma, metodoFirma.Replace("public ", "public async "));
                
                if (metodoFirma.Contains("List<Order> GetOrdersByUserId"))
                    codigo = codigo.Replace("List<Order> GetOrdersByUserId", "Task<List<Order>> GetOrdersByUserId");
            }

            // Reemplazar la invocación directa por HTTP
            var invocacionOriginal = $"if (!{nombreCampo}.UserExists({parametro}))";
            var invocacionHttp = $@"var userExistsClient = _httpClientFactory.CreateClient();
        var userExistsResponse = await userExistsClient.GetAsync($""{urlMicroservicio}/api/users/exists/{{{parametro}}}"");
        var userExists = userExistsResponse.IsSuccessStatusCode;
        
        if (!userExists)";

            codigo = codigo.Replace(invocacionOriginal, invocacionHttp);

            return codigo;
        }

        public async Task<string> GenerarDiffAsync(string codigoOriginal, string codigoRefactorizado)
        {
            var lineasOriginales = codigoOriginal.Split('\n');
            var lineasRefactorizadas = codigoRefactorizado.Split('\n');

            var diff = new StringBuilder();
            diff.AppendLine("--- Original");
            diff.AppendLine("+++ Refactorizado");
            diff.AppendLine();

            for (int i = 0; i < Math.Min(lineasOriginales.Length, lineasRefactorizadas.Length); i++)
            {
                var lineaOriginal = lineasOriginales[i].TrimEnd();
                var lineaRefactorizada = lineasRefactorizadas[i].TrimEnd();

                if (lineaOriginal != lineaRefactorizada)
                {
                    diff.AppendLine($"-{lineaOriginal}");
                    diff.AppendLine($"+{lineaRefactorizada}");
                }
            }

            return await Task.FromResult(diff.ToString());
        }

        public async Task GuardarCodigoRefactorizadoAsync(string rutaDestino, string codigoRefactorizado)
        {
            await File.WriteAllTextAsync(rutaDestino, codigoRefactorizado);
        }

        private string ExtraerNombreClase(string codigo)
        {
            var arbol = CSharpSyntaxTree.ParseText(codigo);
            var raiz = arbol.GetRoot();
            var clase = raiz.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            return clase?.Identifier.Text ?? "Unknown";
        }
    }
}