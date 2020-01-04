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


public static void
Update(NuGitRepository repository)
{
    if (repository == null) throw new ArgumentNullException("repository");

    var lockDependencies = new List<LockDependency>();
    Traverse(
        repository.Workspace,
        repository.ReadDotNuGit().Dependencies,
        repository,
        new Dictionary<GitRepositoryName, GitCommitName>() { { repository.Name, new GitCommitName("HEAD") } },
        new HashSet<GitRepositoryName>() { repository.Name },
        (d,r) => {
            lockDependencies.Add(new LockDependency(d.Url, d.CommitName, r.GetCommitId()));
        });

    repository.WriteDotNuGitLock(lockDependencies);
}


public static void
Restore(NuGitRepository repository, bool exact)
{
    var workspace = repository.Workspace;
    var lockDependencies = repository.ReadDotNuGitLock();

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

        if (!exact && isCheckedOutToExact && hasUncommittedChanges)
        {
            Trace.TraceInformation($"Already checked out with uncommitted changes");
        }
        else if (!exact && isCheckedOutToExact)
        {
            Trace.TraceInformation($"Already checked out");
        }
        else if (!exact && isCheckedOutToDescendent && hasUncommittedChanges)
        {
            Trace.TraceInformation($"Already checked out to descendent with uncommitted changes");
        }
        else if (!exact && isCheckedOutToDescendent)
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
        using (LogicalOperation.Start($"Restoring {d.Url.RepositoryName} to {d.CommitName}"))
        {
            var commitId = repo.GetCommitId(commit);
            var headId = repo.GetCommitId();
            var isCheckedOutToCommit = headId == commitId;
            var hasUncommittedChanges = repo.HasUncommittedChanges();

            if (repo.HasUncommittedChanges())
            {
                Trace.TraceError("Uncommitted changes");
                throw new UserException($"Uncommitted changes in {name}");
            }
            else if (isCheckedOutToCommit)
            {
                Trace.TraceInformation($"Already checked out");
            }
            else
            {
                CheckOut(repo, commit);
            }

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
