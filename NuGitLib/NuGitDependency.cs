﻿using MacroGuards;
using MacroGit;


namespace
NuGitLib
{


/// <summary>
/// A required Git repository plus commit
/// </summary>
///
public class
NuGitDependency
{


public
NuGitDependency(GitUrl url, GitCommitName commitName)
{
    Guard.NotNull(url, nameof(url));
    Guard.NotNull(commitName, nameof(commitName));

    Url = url;
    CommitName = commitName;
}


/// <summary>
/// Required repository
/// </summary>
///
public GitUrl
Url
{
    get;
}


/// <summary>
/// Required commit
/// </summary>
///
public GitCommitName
CommitName
{
    get;
}


}
}
