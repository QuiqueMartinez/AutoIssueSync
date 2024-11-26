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
                ClassOrMethodName = elementName, // Can be a class or method name
                Description = description,
                IssueType = issueType.ToString(),
                IssueStatus = issueStatus.ToString(),
                FilePath = filePath
            };
        }

        static async Task SyncIssuesWithGitHub(List<Issue> updatedIssues)
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

            // 1. Mark issues as closed if not present in updated issues
            foreach (var existingIssue in existingIssues)
            {
                bool isNotInUpdatedIssues = !updatedIssues
                    .Any(updatedIssue => updatedIssue.ClassOrMethodName == existingIssue.Title);

                if (isNotInUpdatedIssues)
                {
                    // Close the issue on GitHub
                    var issueUpdate = new IssueUpdate
                    {
                        State = ItemState.Closed
                    };

                    await githubClient.Issue.Update(owner, repo, existingIssue.Number, issueUpdate);

                    Console.WriteLine($"Issue closed: {existingIssue.HtmlUrl}");
                }
            }

            // 2. Create or update issues that match
            foreach (var updatedIssue in updatedIssues)
            {
                var existingIssue = existingIssues.FirstOrDefault(ei =>
                    ei.Body?.Contains(updatedIssue.ClassOrMethodName) == true);

                if (existingIssue != null)
                {
                    string existingBody = existingIssue.Body?.Trim() ?? string.Empty;
                    string updatedBody = $"**Description**: {updatedIssue.Description}\n" +
                                         $"**Issue Type**: {updatedIssue.IssueType}\n" +
                                         $"**GitHub Column**: {updatedIssue.IssueStatus}\n" +
                                         //$"**Affected Class/Method**: {updatedIssue.ClassOrMethodName}\n" +
                                         $"**File**: {updatedIssue.FilePath}".Trim();

                    bool labelsMatch = existingIssue.Labels.Any(label => label.Name == updatedIssue.IssueType);
                    if (existingBody == updatedBody && labelsMatch)
                    {
                        Console.WriteLine($"Issue skipped (no changes): {existingIssue.HtmlUrl}");
                        continue;
                    }

                    var issueUpdate = new IssueUpdate
                    {
                        Body = updatedBody
                    };

                    try
                    {
                        await githubClient.Issue.Update(owner, repo, existingIssue.Number, issueUpdate);
                        Console.WriteLine($"Issue updated: {existingIssue.HtmlUrl}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating issue '{updatedIssue.ClassOrMethodName}': {ex.Message}");
                    }
                }
                else
                {
                    var issueToCreate = new NewIssue(updatedIssue.ClassOrMethodName)
                    {
                        Body = $"**Description**: {updatedIssue.Description}\n" +
                               $"**Issue Type**: {updatedIssue.IssueType}\n" +
                               $"**GitHub Column**: {updatedIssue.IssueStatus}\n" +
                               //$"**Affected Class/Method**: {updatedIssue.ClassOrMethodName}\n" +
                               $"**File**: {updatedIssue.FilePath}"
                    };
                    issueToCreate.Labels.Add(updatedIssue.IssueType);

                    try
                    {
                        var createdIssue = await githubClient.Issue.Create(owner, repo, issueToCreate);
                        Console.WriteLine($"Issue created: {createdIssue.HtmlUrl}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating issue '{updatedIssue.ClassOrMethodName}': {ex.Message}");
                    }
                }
            }
        }

        public class Issue
        {
            public string ClassOrMethodName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string IssueType { get; set; } = string.Empty;
            public string IssueStatus { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
        }
    }
}
