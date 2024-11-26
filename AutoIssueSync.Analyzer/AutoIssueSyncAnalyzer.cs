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
            var githubToken = Environment.GetEnvironmentVariable("TOKEN_GITHUB");
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

            // 1. Close issues not in updatedIssues or explicitly marked as closed
            foreach (var existingIssue in existingIssues)
            {
                var matchingUpdatedIssue = updatedIssues
                    .FirstOrDefault(ui => ui.ClassOrMethodName == existingIssue.Title && ui.FilePath == ExtractFilePathFromIssue(existingIssue.Body));

                if (matchingUpdatedIssue == null || matchingUpdatedIssue.IssueStatus == "Closed")
                {
                    if (existingIssue.State.Value != ItemState.Closed)
                    {
                        var issueUpdate = new IssueUpdate
                        {
                            State = ItemState.Closed
                        };

                        await githubClient.Issue.Update(owner, repo, existingIssue.Number, issueUpdate);
                        Console.WriteLine($"Issue closed: {existingIssue.HtmlUrl}");
                    }
                }
            }

            // 2. Create or update issues in GitHub
            foreach (var updatedIssue in updatedIssues)
            {
                var existingIssue = existingIssues.FirstOrDefault(ei =>
                    ei.Title == updatedIssue.ClassOrMethodName &&
                    ExtractFilePathFromIssue(ei.Body) == updatedIssue.FilePath);

                string updatedBody = $"**Description**: {updatedIssue.Description}\n" +
                                     $"**Issue Type**: {updatedIssue.IssueType}\n" +
                                     $"**GitHub Column**: {updatedIssue.IssueStatus}\n" +
                                     $"**File**: {updatedIssue.FilePath}";

                if (existingIssue != null)
                {
                    // Check if any fields have changed
                    bool needsUpdate = existingIssue.Body?.Trim() != updatedBody.Trim() ||
                                       !existingIssue.Labels.Any(label => label.Name == updatedIssue.IssueType);

                    if (needsUpdate)
                    {
                        var issueUpdate = new IssueUpdate
                        {
                            Body = updatedBody
                        };
                        issueUpdate.Labels.Clear();
                        issueUpdate.Labels.Add(updatedIssue.IssueType);

                        await githubClient.Issue.Update(owner, repo, existingIssue.Number, issueUpdate);
                        Console.WriteLine($"Issue updated: {existingIssue.HtmlUrl}");
                    }
                    else
                    {
                        Console.WriteLine($"Issue skipped (no changes): {existingIssue.HtmlUrl}");
                    }
                }
                else
                {
                    // Create a new issue
                    var issueToCreate = new NewIssue(updatedIssue.ClassOrMethodName)
                    {
                        Body = updatedBody
                    };
                    issueToCreate.Labels.Add(updatedIssue.IssueType);

                    var createdIssue = await githubClient.Issue.Create(owner, repo, issueToCreate);
                    Console.WriteLine($"Issue created: {createdIssue.HtmlUrl}");
                }
            }
        }

        static string ExtractFilePathFromIssue(string issueBody)
        {
            // Extract the file path from the issue body based on the format used in `updatedBody`
            const string fileKey = "**File**:";
            var lines = issueBody.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith(fileKey))
                {
                    return line.Substring(fileKey.Length).Trim();
                }
            }
            return string.Empty;
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
