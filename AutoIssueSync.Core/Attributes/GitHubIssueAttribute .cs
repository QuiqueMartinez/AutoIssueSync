namespace AutoIssueSync.Core
{
    // Add tags as needed
    public enum IssueType
    {
        TASK,
        BUG,
        SMELL,
        ISSUE,
        FEATURE
    }

    // Add tags as needed
    public enum IssueStatus
    {
        LOW_PRIORITY,
        CRITICAL,
        STAGE,
        IN_PROGRESS,
        REVIEW,
        DONE
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class GitHubIssueAttribute : Attribute
    {
        public IssueType issueType { get; }
        public IssueStatus gitHubColumn { get; }
        public string title { get; }
        public string description { get; }

        public GitHubIssueAttribute(IssueType issueType, IssueStatus gitHubColumn, string title, string description)
        {
            this.issueType = issueType;
            this.gitHubColumn = gitHubColumn;
            this.title = title;
            this.description = description;
        }
    }
}