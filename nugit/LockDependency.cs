using MacroGuards;
using MacroGit;


namespace
nugit
{


/// <summary>
/// A required Git repository, commit name, and exact commit ID
/// </summary>
///
public class
LockDependency
    : Dependency
{


public
LockDependency(GitUrl url, GitRev commitName, GitRev commitId)
    : base(url, commitName)
{
    Guard.NotNull(commitId, nameof(commitId));

    CommitId = commitId;
}


/// <summary>
/// Exact required commit unique ID
/// </summary>
///
public GitRev
CommitId
{
    get;
}


}
}
