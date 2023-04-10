using MacroGuards;
using MacroGit;


namespace
nugit
{


/// <summary>
/// A required Git repository plus commit
/// </summary>
///
public class
Dependency
{


public
Dependency(GitUrl url, GitRev commitName)
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
public GitRev
CommitName
{
    get;
}


}
}
