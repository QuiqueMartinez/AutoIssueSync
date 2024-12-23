# AutoIssueSync
---
*AutoIssueSync* is a command-line tool for automating the synchronization of project issues with GitHub. This tool analyzes `.cs` files in a C# project, identifies attributes tagged with `GitHubIssue`, and ensures that the corresponding issues in a GitHub repository are created, updated, or closed based on the current state of the codebase.

**IMPORTANT!!** The objective of this utility is to manage all issues' lifecycles through code attributes, pointing to concrete classes or methods. It may have undesired effects on issues created manually in Github.


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
   - `TOKEN_GITHUB`: A personal access token for GitHub with `repo` scope.

2. **Run the tool**:

Add attributes to classes or methods directly in the code. A git action triggers the utility to update the issues.


![Attributes](Images/Usage01.png)


Access to the issues info directly in Github or in any issue tracker like VS Code extensions or GitKraken


![Attributes](Images/Usage02.png)
![Attributes](Images/Usage03.png)

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
