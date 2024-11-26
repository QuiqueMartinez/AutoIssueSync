# AutoIssueSync
---
*AutoIssueSync* is a command-line tool for automating the synchronization of project issues with GitHub. This tool analyzes `.cs` files in a C# project, identifies attributes tagged with `GitHubIssue`, and ensures that the corresponding issues in a GitHub repository are created, updated, or closed based on the current state of the codebase.

**IMPORTANT!!** The objective of this utility is to manage all issues' lifecycles through code attributes. It may have undesired effects on issues created manually in Github.


## Features

- **Analyze C# files**: Detect methods and classes annotated with `GitHubIssue` attributes.
- **Synchronize issues**:
  - Create new issues for newly detected annotations.
  - Update existing issues if there are any changes.
  - Close issues that no longer exist in the codebase.
- **GitHub integration**:
  - Uses the GitHub API to manage issues directly in the repository.

---

## Usage

1. **Set up environment variables**:
   - `GITHUB_TOKEN`: A personal access token for GitHub with `repo` scope.
   - `GITHUB_REPOSITORY`: The target repository in the format `owner/repo`.

   Example:
   ```bash
   export GITHUB_TOKEN=your_personal_access_token
   export GITHUB_REPOSITORY=owner/repo
   ```

2. **Run the tool**:
   ```bash
   dotnet run -- <project_path>
   ```

   Replace `<project_path>` with the path to your C# project directory.

3. **Check the output**:
   - The tool will log the synchronization process to the console, including created, updated, and closed issues.

---

## `GitHubIssue` Attribute

For AutoIssueSync to detect an issue, the method or class must be annotated with a `GitHubIssue` attribute. Example:

```csharp
[GitHubIssue(IssueType.Bug, IssueStatus.Todo, "Fix null reference exception", "Occurs in edge case scenarios")]
public void ExampleMethod()
{
    // Method implementation
}
```

- **Arguments**:
  - `IssueType`: The type of issue (e.g., `Bug`, `Feature`, etc.).
  - `IssueStatus`: The status of the issue (e.g., `Todo`, `InProgress`, etc.).
  - `Title`: A short title for the issue.
  - `Description`: A detailed description of the issue.

---

## Synchronization Logic

The tool applies the following rules:

1. **Identifying issues**:
   - An issue is considered the same if it corresponds to the same method or class in the codebase.

2. **Actions**:
   - **Create**: If an issue is detected in the codebase but doesn't exist in the GitHub repository.
   - **Update**: If an existing issue differs in any way (e.g., title, description, or labels).
   - **Close**: If an issue exists in GitHub but no longer corresponds to a method or class in the codebase.

---

## Example Output

```plaintext
Analyzing file: /path/to/MyClass.cs
Issue created: https://github.com/owner/repo/issues/123
Issue updated: https://github.com/owner/repo/issues/124
Issue closed: https://github.com/owner/repo/issues/125
```

---

## Contributing

Feel free to suggest features, fork, and open pull requests on GitHub.

---

## License

This project is distributed under the Unlicensed License. You can use, modify, and distribute the code without restrictions. However, the authors disclaim all warranties and assume no responsibility for any issues, damages, or liabilities arising from this software's use, modification, or distribution.

---

## Acknowledgments

- Built with [Octokit](https://github.com/octokit/octokit.net) for GitHub API integration.
- Powered by the Roslyn Compiler Platform for C# code analysis. 

---

Feel free to suggest features or report bugs in the [Issues](https://github.com/your-username/AutoIssueSync/issues) section!
