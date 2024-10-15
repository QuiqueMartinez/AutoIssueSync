using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;


namespace AutoIssueSync.Analyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Use: AutoIssueSync.Analyzer <project_path>");
                return;
            }

            // Get project's path as an argument
            var projectPath = args[0];
            var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

            var issues = new List<Issue>();

            foreach (var file in files)
            {
                Console.WriteLine($"Analizando archivo: {file}");
                var code = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = syntaxTree.GetRoot();

                var methodsWithAttributes = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr => attr.Name.ToString().Contains("GitHubIssue")))
                    .ToList();

                foreach (var method in methodsWithAttributes)
                {
                    var attributes = method.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(attr => attr.Name.ToString().Contains("GitHubIssue"));

                    foreach (var attribute in attributes)
                    {
                        var issue = ParseGitHubIssueAttribute(attribute, method.Identifier.ToString(), file);
                        issues.Add(issue);
                    }
                }
            }

            // Guardar los resultados en un archivo JSON


            string outputFile = Path.Combine(Directory.GetCurrentDirectory(), "issues.json");

            File.WriteAllText(outputFile, JsonConvert.SerializeObject(issues, Formatting.Indented));
            Console.WriteLine($"Análisis completado. Resultados guardados en {outputFile}");
        }

        // Método para extraer información del atributo `GitHubIssueAttribute`
        static Issue ParseGitHubIssueAttribute(AttributeSyntax attribute, string methodName, string filePath)
        {
            var argumentList = attribute.ArgumentList?.Arguments;

            string title = argumentList?[0].ToString().Trim('"');
            string description = argumentList?[1].ToString().Trim('"');
            string issueType = argumentList?[2].ToString().Replace("IssueType.", "").Trim();

            return new Issue
            {
                MethodName = methodName,
                Title = title,
                Description = description,
                IssueType = issueType,
                FilePath = filePath
            };
        }
    }

    // Clase para almacenar la información de cada issue detectado
    public class Issue
    {
        public string MethodName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IssueType { get; set; }
        public string FilePath { get; set; }
    }
}
