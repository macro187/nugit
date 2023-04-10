using System;
using System.IO;
using IOPath = System.IO.Path;
using System.Collections.Generic;
using System.Linq;
using MacroGuards;
using MacroExceptions;
using MacroGit;
using MacroSln;
using MacroIO;

namespace
nugit
{


/// <summary>
/// A nugit repository
/// </summary>
///
public class
NuGitRepository
    : GitRepository
{


internal
NuGitRepository(NuGitWorkspace workspace, GitRepositoryName name)
    : base(
        IOPath.Combine(
            Guard.NotNull(workspace, nameof(workspace)).RootPath,
            Guard.NotNull(name, nameof(name))))
{
    Workspace = workspace;
}


/// <summary>
/// Workspace the repository is in
/// </summary>
///
public NuGitWorkspace
Workspace
{
    get;
}


/// <summary>
/// Determine whether a .nugit file is present
/// </summary>
///
public bool
HasDotNuGit()
{
    return File.Exists(GetDotNuGitPath());
}


/// <summary>
/// Determine whether a .nugit.lock file is present
/// </summary>
///
public bool
HasDotNuGitLock()
{
    return File.Exists(GetDotNuGitLockPath());
}


/// <summary>
/// Read .nugit information
/// </summary>
///
public DotNuGit
ReadDotNuGit()
{
    if (!HasDotNuGit())
    {
        return new DotNuGit();
    }

    var path = GetDotNuGitPath();

    try
    {
        return DotNuGitParser.Parse(File.ReadLines(path));
    }
    catch (TextFileParseException e)
    {
        e.Path = path;
        throw;
    }
}


/// <summary>
/// Read dependency information from .nugit.lock
/// </summary>
///
/// <returns>
/// The dependencies listed in .nugit.lock
/// </returns>
///
/// <exception cref="InvalidOperationException">
/// .nugit.lock not present
/// </exception>
///
public IList<LockDependency>
ReadDotNuGitLock()
{
    if (!HasDotNuGitLock())
    {
        throw new InvalidOperationException(".nugit.lock not present");
    }

    var result = new List<LockDependency>();
    int lineNumber = 0;
    foreach (var rawline in File.ReadLines(GetDotNuGitLockPath()))
    {
        lineNumber++;
        var line = rawline.Trim();

        if (string.IsNullOrEmpty(line)) continue;
        if (line.StartsWith("#", StringComparison.Ordinal)) continue;

        var a = line.Split(' ');
        if (a.Length != 3)
            throw new TextFileParseException(
                "Expected URL, commit name, and commit ID",
                lineNumber,
                rawline);
        
        GitUrl url;
        try
        {
            url = new GitUrl(a[0]);
        }
        catch (FormatException fe)
        {
            throw new TextFileParseException(
                "Expected valid Git URL",
                lineNumber,
                rawline,
                fe);
        }

        GitRev commitName;
        try
        {
            commitName = new GitRev(a[1]);
        }
        catch (FormatException fe)
        {
            throw new TextFileParseException(
                "Expected valid Git commit name",
                lineNumber,
                rawline,
                fe);
        }
        
        GitRev commitId;
        try
        {
            commitId = new GitRev(a[2]);
        }
        catch (FormatException fe)
        {
            throw new TextFileParseException(
                "Expected valid Git commit identifier",
                lineNumber,
                rawline,
                fe);
        }

        result.Add(new LockDependency(url, commitName, commitId));
    }

    return result;
}


/// <summary>
/// Write dependency information to .nugit.lock
/// </summary>
///
public void
WriteDotNuGitLock(ICollection<LockDependency> dependencies)
{
    Guard.NotNull(dependencies, nameof(dependencies));

    string path = GetDotNuGitLockPath();

    if (dependencies.Count == 0)
    {
        File.Delete(path);
        return;
    }

    FileExtensions.RewriteAllLines(
        path,
        dependencies.Select(d =>
            string.Concat(
                d.Url,
                " ",
                d.CommitName,
                " ",
                d.CommitId)));
}


/// <summary>
/// Look for the primary Visual Studio solution in the repository
/// </summary>
///
public VisualStudioSolution
FindVisualStudioSolution()
{
    return VisualStudioSolution.Find(GetDotNuGitDir());
}


/// <summary>
/// Determine full path to .nugit (regardless of whether it actually exists)
/// </summary>
///
string
GetDotNuGitPath()
{
    return IOPath.Combine(GetDotNuGitDir(), ".nugit");
}


/// <summary>
/// Determine full path to .nugit.lock (regardless of whether it actually exists)
/// </summary>
///
string
GetDotNuGitLockPath()
{
    return IOPath.Combine(GetDotNuGitDir(), ".nugit.lock");
}


/// <summary>
/// Determine full path to directory that may contain .nugit and .nugit.lock
/// </summary>
///
string
GetDotNuGitDir()
{
    string dotNugitDir = IOPath.Combine(Path, ".nugit");
    if (Directory.Exists(dotNugitDir)) return dotNugitDir;
    return Path;
}


}
}
