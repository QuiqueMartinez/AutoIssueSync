using System.Reflection;
using Octokit;  // Librería para interactuar con la API de GitHub
using AutoIssueSync.Core;

namespace AutoIssueSync.Sync
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Iniciando análisis de atributos y sincronización con GitHub...");

            // 1. Obtener el ensamblado del proyecto principal
            var assembly = Assembly.Load("AutoIssueSync.Core");  // Carga del ensamblado principal
            var types = assembly.GetTypes();

            // 2. Crear un cliente de GitHub
            var githubToken = Environment.GetEnvironmentVariable("TOKEN_GITHUB");
            if (string.IsNullOrEmpty(githubToken))
            {
                Console.WriteLine("Error: TOKEN_GITHUB no está configurado.");
                return;
            }

            var githubClient = new GitHubClient(new ProductHeaderValue("AutoIssueSync"))
            {
                Credentials = new Credentials(githubToken)
            };

            // 3. Analizar las clases y métodos con atributos
            foreach (var type in types)
            {
                var classAttributes = type.GetCustomAttributes<GitHubIssueAttribute>();

                Console.WriteLine("Found " + classAttributes.Count() +" class attributes.");

                foreach (var attribute in classAttributes)
                {
                    await CreateOrUpdateIssue(githubClient, type.Name, attribute);
                }

                foreach (var method in type.GetMethods())
                {



                    var methodAttributes = method.GetCustomAttributes<GitHubIssueAttribute>();
                    Console.WriteLine("Found " + methodAttributes.Count() + " methodAttributes attributes.");
                    foreach (var attribute in methodAttributes)
                    {
                        await CreateOrUpdateIssue(githubClient, $"{type.Name}.{method.Name}", attribute);
                    }
                }
            }

            Console.WriteLine("Sincronización completa.");
        }

        // Función para crear o actualizar issues en GitHub basados en los atributos encontrados
        static async System.Threading.Tasks.Task CreateOrUpdateIssue(GitHubClient client, string source, GitHubIssueAttribute attribute)
        {
            var owner = "mi-org";  // Reemplazar con tu usuario u organización
            var repo = "AutoIssueSync"; // Reemplazar con tu repositorio

            // Crear el issue en GitHub con los detalles del atributo
            var newIssue = new NewIssue($"{attribute.title} ({source})")
            {
                Body = $"Descripción: {attribute.description}\nTipo de Issue: {attribute.issueType}\nColumna Sugerida: {attribute.gitHubColumn}"
            };

            newIssue.Labels.Add(attribute.issueType.ToString()); // Agregar etiquetas de manera estándar
            await client.Issue.Create(owner, repo, newIssue);
            Console.WriteLine($"Issue '{newIssue.Title}' creado exitosamente en {repo}.");
        }
    }
}
