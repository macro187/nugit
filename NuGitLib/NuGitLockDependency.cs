﻿using MacroGuards;
using MacroGit;


namespace
NuGitLib
{


/// <summary>
/// A required Git repository, commit name, and exact commit ID
/// </summary>
///
public class
NuGitLockDependency
    : NuGitDependency
{


public
NuGitLockDependency(GitUrl url, GitCommitName commitName, GitCommitName commitId)
    : base(url, commitName)
{
    Guard.NotNull(commitId, nameof(commitId));

    CommitId = commitId;
}


/// <summary>
/// Exact required commit unique ID
/// </summary>
///
public GitCommitName
CommitId
{
    get;
}


}
}
