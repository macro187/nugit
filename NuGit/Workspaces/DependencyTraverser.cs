using System;
using System.Collections.Generic;
using System.Diagnostics;
using MacroSystem;
using MacroGuards;
using MacroGit;
using NuGit.Infrastructure;

namespace NuGit.Workspaces
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
    public static class DependencyTraverser
    {

        /// <summary>
        /// Get a list of a repository's full dependency graph in breadth-first order
        /// </summary>
        ///
        public static IList<GitRepositoryName> GetAllDependencies(Repository repository)
        {
            using (TraceExtensions.Step("Calculating dependencies"))
            {
                var names = new List<GitRepositoryName>();
                Traverse(repository, (d,r) => names.Add(r.Name));
                return names;
            }
        }
        

        /// <summary>
        /// Traverse a repository's dependencies
        /// </summary>
        ///
        public static void Traverse(Repository repository)
        {
            Traverse(repository, (d,r) => {});
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
        public static void Traverse(Repository repository, Action<Dependency,Repository> onVisited)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            IList<LockDependency> lockDependencies;

            lockDependencies = repository.ReadNuGitLock();
            if (lockDependencies.Count > 0)
            {
                TraverseLock(repository, lockDependencies, onVisited);
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

            repository.WriteNuGitLock(lockDependencies);
        }


        static void TraverseLock(
            Repository repository,
            IList<LockDependency> lockDependencies,
            Action<Dependency,Repository> onVisited
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
        public static void Traverse(Workspace workspace, IEnumerable<Dependency> dependencies)
        {
            Traverse(workspace, dependencies, (name,repository) => {});
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
        static void Traverse(
            Workspace workspace,
            IEnumerable<Dependency> dependencies,
            Action<Dependency,Repository> onVisited)
        {
            Traverse(
                workspace,
                dependencies,
                null,
                new Dictionary<GitRepositoryName,GitCommitName>(),
                new HashSet<GitRepositoryName>(),
                onVisited);
        }


        static void Traverse(
            Workspace workspace,
            IEnumerable<Dependency> dependencies,
            Repository requiredBy,
            IDictionary<GitRepositoryName,GitCommitName> checkedOut,
            ISet<GitRepositoryName> visited,
            Action<Dependency,Repository> onVisited
            )
        {
            Guard.NotNull(workspace, nameof(workspace));
            Guard.NotNull(dependencies, nameof(dependencies));
            Guard.NotNull(checkedOut, nameof(checkedOut));
            Guard.NotNull(visited, nameof(visited));
            Guard.NotNull(onVisited, nameof(onVisited));

            //
            // Clone dependencies that aren't present
            //
            foreach (var d in dependencies)
            {
                if (workspace.FindRepository(d.Url.RepositoryName) != null) continue;
                Clone(workspace.RootPath, d.Url);
            }

            //
            // Check out dependencies to specified commits
            //
            foreach (var d in dependencies)
            {
                var name = d.Url.RepositoryName;
                var commit = d.CommitName;

                //
                // Avoid checking out repositories more than once, but warn on subsequent attempts if the commit
                // doesn't match
                //
                GitCommitName checkedOutCommit;
                if (checkedOut.TryGetValue(name, out checkedOutCommit))
                {
                    if (commit != checkedOutCommit)
                    {
                        Trace.TraceWarning(
                            StringExtensions.FormatInvariant(
                                "{0} depends on {1}#{2} but #{3} has already been checked out",
                                requiredBy.Name,
                                name,
                                commit,
                                checkedOutCommit));
                    }
                    continue;
                }

                CheckOut(workspace.GetRepository(name), commit);
                checkedOut.Add(name, commit);
            }

            //
            // onVisited
            //
            foreach (var d in dependencies)
            {
                var name = d.Url.RepositoryName;
                if (visited.Contains(name)) continue;
                onVisited(d, workspace.GetRepository(name));
            }

            //
            // Recurse
            //
            foreach (var d in dependencies)
            {
                var name = d.Url.RepositoryName;
                var repo = workspace.FindRepository(name);
                if (visited.Contains(name)) continue;

                visited.Add(name);

                Traverse(
                    workspace,
                    repo.ReadDotNuGit().Dependencies,
                    repo,
                    checkedOut,
                    visited,
                    onVisited);
            }
        }


        static void Clone(string parentPath, GitUrl url)
        {
            using (TraceExtensions.Step("Cloning " + url.RepositoryName))
            {
                GitRepository.Clone(parentPath, url);
            }
        }


        static void CheckOut(Repository repository, GitCommitName commit)
        {
            using (TraceExtensions.Step("Checking out " + repository.Name + " to " + commit))
            {
                repository.Checkout(commit);
            }
        }

    }

}
