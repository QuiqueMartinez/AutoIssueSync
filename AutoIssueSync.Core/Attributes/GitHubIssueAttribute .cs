﻿using System;

namespace AutoIssueSync.Core
{
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
