using System;
using System.Collections.Generic;
using System.Diagnostics;
using MacroSystem;
using MacroGuards;
using MacroDiagnostics;
using MacroGit;
using System.Linq;


namespace
nugit
{


/// <summary>
/// Dependency traversal algorithm
/// </summary>
///
/// <remarks>
/// <para>
/// Traverses the graph of required repositories breadth-first.
/// </para>
/// <para>
/// Clones (when necessary) and checks out specified version of repositories as they are encountered.
/// </para>
/// <para>
/// For a given repository, the version specified in the first-encountered (or "shallowest") dependency is used.
/// No action is taken for subsequently-encountered dependencies on the same version, however warnings are produced
/// for subsequently-encountered dependencies on different versions.
/// </para>
/// </remarks>
///
public static class
DependencyTraverser
{


/// <summary>
/// Get a list of a repository's full dependency graph in breadth-first order
/// </summary>
///
public static IList<GitRepositoryName>
GetAllDependencies(NuGitRepository repository)
{
    using (LogicalOperation.Start("Calculating dependencies"))
    {
        var names = new List<GitRepositoryName>();
        Traverse(repository, (d,r) => names.Add(r.Name), true);
        return names;
    }
}


/// <summary>
/// Traverse a repository's dependencies, using frozen dependency information in the lockfile if present
/// </summary>
///
public static void
Traverse(NuGitRepository repository)
{
    Traverse(repository, true);
}


/// <summary>
/// Traverse a repository's dependencies
/// </summary>
///
/// <param name="useLock">
/// Whether to use frozen dependency information in the lockfile, if present
/// </param>
///
public static void
Traverse(NuGitRepository repository, bool useLock)
{
    Traverse(repository, (d,r) => {}, useLock);
}


/// <summary>
/// Traverse a repository's dependencies, performing an action as each is visited
/// </summary>
///
/// <remarks>
/// Each dependency is visited exactly once.
/// </remarks>
///
/// <param name="onVisited">
/// An action to invoke for each repository encountered during the descent through the dependency graph, at
/// which time the correct version of the repository itself will be available but its transitive dependencies
/// will not
/// </param>
///
/// <param name="useLock">
/// Whether to use frozen dependency information in the lockfile, if present
/// </param>
///
public static void
Traverse(NuGitRepository repository, Action<Dependency,NuGitRepository> onVisited, bool useLock)
{
    if (repository == null) throw new ArgumentNullException("repository");

    IList<LockDependency> lockDependencies;

    if (useLock)
    {
        lockDependencies = repository.ReadNuGitLock();
        if (lockDependencies != null)
        {
            TraverseLock(repository, lockDependencies, onVisited);
            return;
        }
    }

    lockDependencies = new List<LockDependency>();

    Traverse(
        repository.Workspace,
        repository.ReadDotNuGit().Dependencies,
        repository,
        new Dictionary<GitRepositoryName, GitCommitName>() { { repository.Name, new GitCommitName("HEAD") } },
        new HashSet<GitRepositoryName>() { repository.Name },
        (d,r) => {
            onVisited(d,r);
            lockDependencies.Add(new LockDependency(d.Url, d.CommitName, r.GetCommitId()));
            }
        );

    repository.WriteNuGitLock(lockDependencies);
}


static void
TraverseLock(
    NuGitRepository repository,
    IList<LockDependency> lockDependencies,
    Action<Dependency,NuGitRepository> onVisited
)
{
    var workspace = repository.Workspace;

    foreach (var d in lockDependencies)
    {
        var name = d.Url.RepositoryName;
        var r = workspace.FindRepository(name);
        if (r == null)
        {
            Clone(workspace.RootPath, d.Url);
            r = workspace.GetRepository(name);
        }
        if (r.GetCommitId(d.CommitName) == d.CommitId)
        {
            CheckOut(r, d.CommitName);
        }
        else
        {
            CheckOut(r, d.CommitId);
        }
        onVisited(d, r);
    }
}


/// <summary>
/// Traverse specified dependencies
/// </summary>
///
public static void
Traverse(NuGitWorkspace workspace, IEnumerable<Dependency> dependencies)
{
    Traverse(workspace, dependencies, (d,r) => {});
}


/// <summary>
/// Traverse specified dependencies, performing an action as each is visited
/// </summary>
///
/// <remarks>
/// Each dependency is visited exactly once.
/// </remarks>
///
/// <param name="onVisited">
/// An action to invoke for each repository encountered during the descent through the dependency graph, at
/// which time the correct version of the repository itself will be available but its transitive dependencies
/// will not
/// </param>
///
static void
Traverse(
    NuGitWorkspace workspace,
    IEnumerable<Dependency> dependencies,
    Action<Dependency,NuGitRepository> onVisited)
{
    Traverse(
        workspace,
        dependencies,
        null,
        new Dictionary<GitRepositoryName,GitCommitName>(),
        new HashSet<GitRepositoryName>(),
        onVisited);
}


static void
Traverse(
    NuGitWorkspace workspace,
    IEnumerable<Dependency> dependencies,
    NuGitRepository requiredBy,
    IDictionary<GitRepositoryName,GitCommitName> checkedOut,
    ISet<GitRepositoryName> visited,
    Action<Dependency,NuGitRepository> onVisited
    )
{
    Guard.NotNull(workspace, nameof(workspace));
    Guard.NotNull(dependencies, nameof(dependencies));
    Guard.NotNull(checkedOut, nameof(checkedOut));
    Guard.NotNull(visited, nameof(visited));
    Guard.NotNull(onVisited, nameof(onVisited));

    var unvisited = dependencies.Where(d => !visited.Contains(d.Url.RepositoryName)).ToList().AsReadOnly();

    //
    // Clone any dependency repos that aren't present
    //
    foreach (var d in unvisited)
    {
        if (workspace.FindRepository(d.Url.RepositoryName) != null) continue;
        Clone(workspace.RootPath, d.Url);
    }

    //
    // Visit each dependency
    //
    foreach (var d in dependencies)
    {
        var name = d.Url.RepositoryName;
        var repo = workspace.GetRepository(name);
        var commit = d.CommitName;
        checkedOut.TryGetValue(name, out var checkedOutCommit);

        //
        // First visit wins
        //
        if (checkedOutCommit == null)
        {
            CheckOut(repo, commit);
            checkedOut.Add(name, commit);
            visited.Add(name);
            onVisited(d, repo);
            continue;
        }

        //
        // Subsequent visits specifying different commits get a warning
        //
        if (commit != checkedOutCommit)
        {
            Trace.TraceWarning(
                StringExtensions.FormatInvariant(
                    "{0} depends on {1}#{2} but #{3} has already been checked out",
                    requiredBy.Name,
                    name,
                    commit,
                    checkedOutCommit));
            continue;
        }

        //
        // Subsequent visits specifying the same commit do nothing
        //
    }

    //
    // Recurse
    //
    foreach (var d in unvisited)
    {
        var name = d.Url.RepositoryName;
        var repo = workspace.FindRepository(name);

        Traverse(
            workspace,
            repo.ReadDotNuGit().Dependencies,
            repo,
            checkedOut,
            visited,
            onVisited);
    }
}


static void
Clone(string parentPath, GitUrl url)
{
    using (LogicalOperation.Start("Cloning " + url.RepositoryName))
    {
        GitRepository.Clone(parentPath, url);
    }
}


static void
CheckOut(NuGitRepository repository, GitCommitName commit)
{
    using (LogicalOperation.Start("Checking out " + repository.Name + " to " + commit))
    {
        repository.Checkout(commit);
    }
}


}
}
