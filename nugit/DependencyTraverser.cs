using System;
using System.Collections.Generic;
using System.Diagnostics;
using MacroSystem;
using MacroGuards;
using MacroDiagnostics;
using MacroGit;
using System.Linq;
using MacroExceptions;

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
/// <param name="isLoose">
/// If using frozen dependency information from a lockfile, whether to accept dependencies that are already checked out
/// to the frozen version OR NEWER
/// </param>
///
public static IList<GitRepositoryName>
GetAllDependencies(NuGitRepository repository, bool isLoose)
{
    using (LogicalOperation.Start("Calculating dependencies"))
    {
        var names = new List<GitRepositoryName>();
        Traverse(repository, (d,r) => names.Add(r.Name), true, isLoose);
        return names;
    }
}


/// <summary>
/// Traverse a repository's dependencies
/// </summary>
///
/// <param name="useLock">
/// Whether to use frozen dependency information in the lockfile, if present
/// </param>
///
/// <param name="isLoose">
/// If using frozen dependency information from a lockfile, whether to accept dependencies that are already checked out
/// to the frozen version OR NEWER
/// </param>
///
public static void
Traverse(NuGitRepository repository, bool useLock, bool isLoose)
{
    Traverse(repository, (d,r) => {}, useLock, isLoose);
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
/// <param name="isLoose">
/// If using frozen dependency information from a lockfile, whether to accept dependencies that are already checked out
/// to the frozen version OR NEWER
/// </param>
///
public static void
Traverse(NuGitRepository repository, Action<Dependency,NuGitRepository> onVisited, bool useLock, bool isLoose)
{
    if (repository == null) throw new ArgumentNullException("repository");

    IList<LockDependency> lockDependencies;

    if (useLock && repository.HasDotNuGitLock())
    {
        TraverseLock(repository, repository.ReadDotNuGitLock(), onVisited, isLoose);
        return;
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

    repository.WriteDotNuGitLock(lockDependencies);
}


public static void
TraverseLock(NuGitRepository repository, bool isLoose)
{
    if (repository == null) throw new ArgumentNullException("repository");
    var lockDependencies = repository.ReadDotNuGitLock();
    TraverseLock(repository, lockDependencies, (_,__) => {}, isLoose);
}


static void
TraverseLock(
    NuGitRepository repository,
    IList<LockDependency> lockDependencies,
    Action<Dependency,NuGitRepository> onVisited,
    bool isLoose
)
{
    var workspace = repository.Workspace;

    foreach (var d in lockDependencies)
    using (LogicalOperation.Start($"Restoring {d.Url.RepositoryName} to {d.CommitName} ({d.CommitId})"))
    {
        var name = d.Url.RepositoryName;

        var r = workspace.FindRepository(name);
        if (r == null)
        {
            Clone(workspace.RootPath, d.Url);
            r = workspace.GetRepository(name);
        }

        var head = r.GetCommitId(new GitCommitName("HEAD"));
        var isCheckedOutToExact = head == d.CommitId;
        var isCheckedOutToDescendent = r.IsAncestor(d.CommitId, head);
        var hasUncommittedChanges = r.HasUncommittedChanges();
        var isCommitNameAtCommitId = r.GetCommitId(d.CommitName) == d.CommitId;

        if (isLoose && isCheckedOutToExact && hasUncommittedChanges)
        {
            Trace.TraceInformation($"Already checked out with uncommitted changes");
        }
        else if (isLoose && isCheckedOutToExact)
        {
            Trace.TraceInformation($"Already checked out");
        }
        else if (isLoose && isCheckedOutToDescendent && hasUncommittedChanges)
        {
            Trace.TraceInformation($"Already checked out to descendent with uncommitted changes");
        }
        else if (isLoose && isCheckedOutToDescendent)
        {
            Trace.TraceInformation($"Already checked out to descendent");
        }
        else if (r.HasUncommittedChanges())
        {
            Trace.TraceError("Uncommitted changes");
            throw new UserException($"Uncommitted changes in {name}");
        }
        else if (isCheckedOutToExact)
        {
            Trace.TraceInformation($"Already checked out");
        }
        else if (isCommitNameAtCommitId)
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
    using (LogicalOperation.Start($"Cloning {url}"))
    {
        GitRepository.Clone(parentPath, url);
    }
}


static void
CheckOut(NuGitRepository repository, GitCommitName commit)
{
    using (LogicalOperation.Start($"Checking out {commit}"))
    {
        repository.Checkout(commit);
    }
}


}
}
