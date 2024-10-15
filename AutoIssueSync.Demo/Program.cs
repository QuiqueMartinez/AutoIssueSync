using System.Reflection;
using AutoIssueSync.Core;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Printing AutoIssueSync custom attributes");

        var testClassType = typeof(TestClass);
        AnalyzeAttributes(testClassType);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void AnalyzeAttributes(Type type)
    {
        Console.WriteLine($"Analyzing class attributes: {type.Name}");

        // Obtener los atributos aplicados a la clase
        var classAttributes = type.GetCustomAttributes<GitHubIssueAttribute>();
        foreach (var attr in classAttributes)
        {
            PrintAttributeInfo(attr, type.Name, "Class");
        }

        // Obtener los métodos de la clase y sus atributos
        foreach (var method in type.GetMethods())
        {
            var methodAttributes = method.GetCustomAttributes<GitHubIssueAttribute>();
            foreach (var attr in methodAttributes)
            {
                PrintAttributeInfo(attr, method.Name, "Method");
            }
        }
    }

    // Función que imprime la información de un atributo
    static void PrintAttributeInfo(GitHubIssueAttribute attribute, string elementName, string elementType)
    {
        Console.WriteLine($"{elementType}: {elementName}");
        Console.WriteLine($"  - Issue type: {attribute.issueType}");
        Console.WriteLine($"  - GitHub column: {attribute.gitHubColumn}");
        Console.WriteLine($"  - Title: {attribute.title}");
        Console.WriteLine($"  - Description: {attribute.description}\n");
    }
}

// Test class with custom attributes that will be converted into
// Github issues

[GitHubIssue(IssueType.BUG, GitHubColumn.TODO, "Bug in class.", 
    "Example of how to convert a bug into an issue in the test class.")]
public class TestClass
{
    [GitHubIssue(IssueType.FEATURE, GitHubColumn.IN_PROGRESS, "MyNewFeature:", 
        "Need to implement a new feature.")]
    public void NewFeature()
    {
    }

    [GitHubIssue(IssueType.TASK, GitHubColumn.REVIEW, "Pending task", 
        "Optimize this method.")]
    public void ReviewMethod()
    {
    }
}