using System;
using System.Linq;
using System.Reflection;
using AutoIssueSync.Core;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Probando los atributos personalizados de AutoIssueSync...");

        // Crear una instancia de la clase de prueba y analizar sus atributos
        var testClassType = typeof(TestClass);
        AnalyzeAttributes(testClassType);

        Console.WriteLine("\nPresiona cualquier tecla para salir...");
        Console.ReadKey();
    }

    // Función que usa reflexión para analizar los atributos de una clase o método
    static void AnalyzeAttributes(Type type)
    {
        Console.WriteLine($"Analizando atributos de la clase: {type.Name}");

        // Obtener los atributos aplicados a la clase
        var classAttributes = type.GetCustomAttributes<GitHubIssueAttribute>();
        foreach (var attr in classAttributes)
        {
            PrintAttributeInfo(attr, type.Name, "Clase");
        }

        // Obtener los métodos de la clase y sus atributos
        foreach (var method in type.GetMethods())
        {
            var methodAttributes = method.GetCustomAttributes<GitHubIssueAttribute>();
            foreach (var attr in methodAttributes)
            {
                PrintAttributeInfo(attr, method.Name, "Método");
            }
        }
    }

    // Función que imprime la información de un atributo
    static void PrintAttributeInfo(GitHubIssueAttribute attribute, string elementName, string elementType)
    {
        Console.WriteLine($"{elementType}: {elementName}");
        Console.WriteLine($"  - Tipo de Issue: {attribute.issueType}");
        Console.WriteLine($"  - Columna de GitHub: {attribute.gitHubColumn}");
        Console.WriteLine($"  - Título: {attribute.title}");
        Console.WriteLine($"  - Descripción: {attribute.description}\n");
    }
}

// Clase de prueba con atributos personalizados
[GitHubIssue(IssueType.BUG, GitHubColumn.TODO, "Bug en el constructor", "Este es un ejemplo de un bug en la clase de prueba.")]
public class TestClass
{
    [GitHubIssue(IssueType.FEATURE, GitHubColumn.IN_PROGRESS, "Nueva característica", "Implementar nueva característica en este método.")]
    public void NewFeature()
    {
    }

    [GitHubIssue(IssueType.TASK, GitHubColumn.REVIEW, "Revisar implementación", "Revisar la implementación y optimizar el método.")]
    public void ReviewMethod()
    {
    }
}