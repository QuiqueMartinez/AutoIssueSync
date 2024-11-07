using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Octokit;
using AutoIssueSync.Core;

namespace AutoIssueSync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: AutoIssueSync <project_path>");
                return;
            }

            // Get the project path
            var projectPath = args[0];
            var issues = AnalyzeProject(projectPath);

            if (issues == null || !issues.Any())
            {
                Console.WriteLine("No issues found to process.");
                return;
            }

            // Proceed with synchronization to GitHub
            await SyncIssuesWithGitHub(issues);
        }

        static List<Issue> AnalyzeProject(string projectPath)
        {
            var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
            var issues = new List<Issue>();

            foreach (var file in files)
            {
                Console.WriteLine($"Analyzing file: {file}");
                var code = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var root = syntaxTree.GetRoot();

                // Analyze class attributes
                var classesWithAttributes = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(cls => cls.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr => attr.Name.ToString().Contains("GitHubIssue")))
                    .ToList();

                foreach (var cls in classesWithAttributes)
                {
                    var attributes = cls.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Where(attr => attr.Name.ToString().Contains("GitHubIssue"));

                    foreach (var attribute in attributes)
                    {
                        var issue = ParseGitHubIssueAttribute(attribute, cls.Identifier.ToString(), file);
                        issues.Add(issue);
                    }
                }

                // Analyze method attributes
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

            return issues;
        }

        static Issue ParseGitHubIssueAttribute(AttributeSyntax attribute, string elementName, string filePath)
        {
            var argumentList = attribute.ArgumentList?.Arguments.ToList();

            if (argumentList == null || argumentList.Count < 4)
                throw new ArgumentException("GitHubIssueAttribute must have four arguments: issueType, gitHubColumn, title, and description");

            var issueType = Enum.Parse<IssueType>(argumentList[0].ToString().Replace("IssueType.", "").Trim());
            var issueStatus = Enum.Parse<IssueStatus>(argumentList[1].ToString().Replace("IssueStatus.", "").Trim());
            string title = argumentList[2].ToString().Trim('"');
            string description = argumentList[3].ToString().Trim('"');

            return new Issue
            {
                MethodName = elementName, // Can be a class or method name
                Title = title,
                Description = description,
                IssueType = issueType.ToString(),
                IssueStatus = issueStatus.ToString(),
                FilePath = filePath
            };
        }

        static async Task SyncIssuesWithGitHub(List<Issue> newIssues)
        {
            var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrWhiteSpace(githubToken))
            {
                Console.WriteLine("Error: GitHub token is not set.");
                return;
            }

            var githubClient = new GitHubClient(new ProductHeaderValue("AutoIssueSync"))
            {
                Credentials = new Credentials(githubToken)
            };

            string[] repoInfo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")?.Split('/') ?? new string[0];
            if (repoInfo.Length != 2)
            {
                Console.WriteLine("Error: Could not determine repository from GITHUB_REPOSITORY.");
                return;
            }

            string owner = repoInfo[0];
            string repo = repoInfo[1];

            Console.WriteLine($"Processing issues for repository: {owner}/{repo}");

            var existingIssues = await githubClient.Issue.GetAllForRepository(owner, repo);

            // 1. Mark issues as closed if not present in new issues
            foreach (var existingIssue in existingIssues)
            {
                if (existingIssue.Labels.Any(label => label.Name.Equals("closed", StringComparison.OrdinalIgnoreCase)) ||
                    !newIssues.Any(ni => ni.Title == existingIssue.Title && ni.IssueType == existingIssue.Labels.FirstOrDefault()?.Name))
                {
                    var issueUpdate = new IssueUpdate
                    {
                        State = ItemState.Closed
                    };
                    await githubClient.Issue.Update(owner, repo, existingIssue.Number, issueUpdate);
                    Console.WriteLine($"Issue closed: {existingIssue.HtmlUrl}");
                }
            }

            // 2. Create or update issues that match
            foreach (var newIssue in newIssues)
            {
                var existingIssue = existingIssues.FirstOrDefault(ei => ei.Title == newIssue.Title &&
                                                                       ei.Labels.Any(label => label.Name == newIssue.IssueType));
                if (existingIssue == null)
                {
                    // Create new issue
                    var issueToCreate = new NewIssue(newIssue.Title)
                    {
                        Body = $"**Description**: {newIssue.Description}\n**Issue Type**: {newIssue.IssueType}\n**GitHub Column**: {newIssue.IssueStatus}\n**Affected Method**: {newIssue.MethodName}\n**File**: {newIssue.FilePath}"
                    };
                    issueToCreate.Labels.Add(newIssue.IssueType);

                    try
                    {
                        var createdIssue = await githubClient.Issue.Create(owner, repo, issueToCreate);
                        Console.WriteLine($"Issue created: {createdIssue.HtmlUrl}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating issue '{newIssue.Title}': {ex.Message}");
                    }
                }
            }
        }

        public class Issue
        {
            public string MethodName { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string IssueType { get; set; }
            public string IssueStatus { get; set; }
            public string FilePath { get; set; }
        }
    }
}
