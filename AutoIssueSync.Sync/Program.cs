using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;

namespace AutoIssueSync.Sync
{
    class Program
    {
        // Clase para almacenar la información de cada issue detectado
        public class IssueData
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string IssueType { get; set; }
            public string MethodName { get; set; }
            public string FilePath { get; set; }
        }

        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Uso: AutoIssueSync.Sync <ruta_issues_json>");
                return;
            }

            // 1. Leer el archivo `issues.json` generado por `AutoIssueSync.Analyzer`
            var issuesFilePath = args[0];
            if (!File.Exists(issuesFilePath))
            {
                Console.WriteLine($"Error: No se encontró el archivo '{issuesFilePath}'");
                return;
            }

            var issues = JsonConvert.DeserializeObject<List<IssueData>>(File.ReadAllText(issuesFilePath));
            if (issues == null || !issues.Any())
            {
                Console.WriteLine("No se encontraron issues para procesar.");
                return;
            }

            // 2. Configurar la autenticación en GitHub con `Octokit`
            var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrWhiteSpace(githubToken))
            {
                Console.WriteLine("Error: El token de GitHub no está configurado.");
                return;
            }

            var githubClient = new GitHubClient(new ProductHeaderValue("AutoIssueSync"))
            {
                Credentials = new Credentials(githubToken)
            };

            // 3. Obtener el nombre del repositorio y el usuario dinámicamente desde variables de entorno de GitHub
            string[] repoInfo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")?.Split('/') ?? new string[0];
            if (repoInfo.Length != 2)
            {
                Console.WriteLine("Error: No se pudo determinar el repositorio desde GITHUB_REPOSITORY.");
                return;
            }

            string owner = repoInfo[0];
            string repo = repoInfo[1];

            Console.WriteLine($"Procesando issues para el repositorio: {owner}/{repo}");

            // 4. Crear los issues en GitHub
            foreach (var issue in issues)
            {
                var newIssue = new NewIssue(issue.Title)
                {
                    Body = $"**Descripción**: {issue.Description}\n**Tipo de Issue**: {issue.IssueType}\n**Método Afectado**: {issue.MethodName}\n**Archivo**: {issue.FilePath}"
                };
                newIssue.Labels.Add(issue.IssueType);  // Añadir el tipo de issue como etiqueta

                try
                {
                    var createdIssue = await githubClient.Issue.Create(owner, repo, newIssue);
                    Console.WriteLine($"Issue creado: {createdIssue.HtmlUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al crear el issue '{issue.Title}': {ex.Message}");
                }
            }
        }
    }
}
