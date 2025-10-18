using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using TransformadorService.DTOs;
using System.IO.Compression;

namespace TransformadorService.Services
{
    public class AnalizadorCodigoService : IAnalizadorCodigoService
    {
        private readonly IConfiguration _configuration;

        public AnalizadorCodigoService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<ClaseAnalizadaDto>> AnalizarModuloAsync(string nombreProyecto, string nombreModulo)
        {
            var uploadsPath = _configuration["Paths:UploadsFolder"] ?? "../Uploads";
            var zipPath = Path.Combine(uploadsPath, nombreProyecto, $"{nombreProyecto}.zip");
            Console.WriteLine($"Ruta Uploads configurada: {_configuration["Paths:UploadsFolder"]}");


            if (!File.Exists(zipPath))
                throw new FileNotFoundException($"No se encontró el archivo {zipPath}");

            // Carpeta temporal
            var tempPath = Path.Combine(Path.GetTempPath(), $"Analisis_{nombreProyecto}_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);

            ZipFile.ExtractToDirectory(zipPath, tempPath);

            //var moduloPath = Directory.GetDirectories(Path.Combine(tempPath, "Modules"))
              //                        .FirstOrDefault(d => d.EndsWith(nombreModulo, StringComparison.OrdinalIgnoreCase));
              
              // Buscar primero la carpeta "Modules" dentro del proyecto descomprimido
              var modulesRoot = Directory.GetDirectories(tempPath, "Modules", SearchOption.AllDirectories)
                  .FirstOrDefault();

              if (modulesRoot == null)
                  throw new DirectoryNotFoundException($"No se encontró la carpeta 'Modules' dentro del proyecto descomprimido.");

              // Buscar el módulo dentro de la carpeta Modules encontrada
              var moduloPath = Directory.GetDirectories(modulesRoot)
                  .FirstOrDefault(d => d.EndsWith(nombreModulo, StringComparison.OrdinalIgnoreCase));

              if (moduloPath == null)
                  throw new DirectoryNotFoundException($"No se encontró el módulo '{nombreModulo}' dentro de la carpeta 'Modules'.");

            if (moduloPath == null)
                throw new DirectoryNotFoundException($"No se encontró el módulo {nombreModulo} dentro del proyecto.");

            var clasesAnalizadas = new List<ClaseAnalizadaDto>();

            // Analizar todos los .cs del módulo
            foreach (var file in Directory.GetFiles(moduloPath, "*.cs", SearchOption.AllDirectories))
            {
                var codigo = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(codigo);
                var root = tree.GetCompilationUnitRoot();

                var clases = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var clase in clases)
                {
                    var claseDto = new ClaseAnalizadaDto
                    {
                        Nombre = clase.Identifier.Text,
                        Namespace = root.DescendantNodes()
                                        .OfType<NamespaceDeclarationSyntax>()
                                        .FirstOrDefault()?.Name.ToString(),
                        Metodos = new List<MetodoAnalizadoDto>()
                    };

                    var metodos = clase.DescendantNodes().OfType<MethodDeclarationSyntax>();

                    foreach (var metodo in metodos)
                    {
                        var atributos = metodo.AttributeLists
                                              .SelectMany(a => a.Attributes)
                                              .Select(a => a.Name.ToString())
                                              .ToList();

                        var httpVerb = atributos.FirstOrDefault(a =>
                            a.StartsWith("Http", StringComparison.OrdinalIgnoreCase));

                        var ruta = metodo.AttributeLists
                                         .SelectMany(a => a.Attributes)
                                         .Select(a => a.ArgumentList?.Arguments.FirstOrDefault()?.ToString()?.Trim('"'))
                                         .FirstOrDefault();

                        claseDto.Metodos.Add(new MetodoAnalizadoDto
                        {
                            Nombre = metodo.Identifier.Text,
                            HttpVerb = httpVerb,
                            Ruta = ruta
                        });
                    }

                    clasesAnalizadas.Add(claseDto);
                }
            }

            // Limpiar la carpeta temporal
            Directory.Delete(tempPath, true);

            return clasesAnalizadas;
        }
    }
}
