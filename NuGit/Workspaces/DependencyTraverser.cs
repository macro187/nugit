using System;
using System.Collections.Generic;
using System.Diagnostics;
using MacroGit;
using NuGit.Infrastructure;
using MacroSystem;
using System.Linq;

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
                Traverse(repository, dep => names.Add(dep.Url.RepositoryName));
                return names;
            }
        }
        

        /// <summary>
        /// Traverse a repository's dependencies
        /// </summary>
        ///
        public static void Traverse(Repository repository)
        {
            Traverse(repository, name => {});
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
        public static void Traverse(Repository repository, Action<Dependency> onVisited)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            IList<Dependency> lockDependencies;

            lockDependencies = repository.GetDotNuGitLock();
            if (lockDependencies.Count > 0)
            {
                TraverseLock(repository, lockDependencies, onVisited);
                return;
            }

            lockDependencies = new List<Dependency>();

            Traverse(
                repository.Workspace,
                repository.GetDotNuGit().Dependencies,
                repository,
                new Dictionary<GitRepositoryName, GitCommitName>() { { repository.Name, new GitCommitName("HEAD") } },
                new HashSet<GitRepositoryName>() { repository.Name },
                d => { onVisited(d); lockDependencies.Add(d); }
                );

            lockDependencies = lockDependencies
                .Select(d =>
                    new Dependency(
                        d.Url,
                        new GitRepository(repository.Workspace.FindRepository(d.Url.RepositoryName).RootPath)
                            .GetCommitId()))
                .ToList();

            repository.SetDotNuGitLock(lockDependencies);
        }


        static void TraverseLock(Repository repository, IList<Dependency> lockDependencies, Action<Dependency> onVisited)
        {
            foreach (var d in lockDependencies)
            {
                CheckOut(repository.Workspace, d.Url.RepositoryName, d.CommitName);
                onVisited(d);
            }
        }


        /// <summary>
        /// Traverse specified dependencies
        /// </summary>
        ///
        public static void Traverse(Workspace workspace, IEnumerable<Dependency> dependencies)
        {
            Traverse(workspace, dependencies, name => {});
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
            Action<Dependency> onVisited)
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
            Action<Dependency> onVisited
            )
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (dependencies == null) throw new ArgumentNullException("dependencies");
            if (checkedOut == null) throw new ArgumentNullException("checkedOut");
            if (visited == null) throw new ArgumentNullException("visited");
            if (onVisited == null) throw new ArgumentNullException("onVisited");

            //
            // Clone dependencies that aren't present
            //
            foreach (var d in dependencies)
            {
                if (workspace.FindRepository(d.Url.RepositoryName) != null) continue;
                using (TraceExtensions.Step("Cloning " + d.Url.RepositoryName))
                {
                    GitRepository.Clone(workspace.RootPath, d.Url);
                }
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

                CheckOut(workspace, name, commit);
                checkedOut.Add(name, commit);
            }

            //
            // onVisited
            //
            foreach (var d in dependencies)
            {
                var name = d.Url.RepositoryName;
                if (visited.Contains(name)) continue;

                onVisited(d);
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
                    repo.GetDotNuGit().Dependencies,
                    repo,
                    checkedOut,
                    visited,
                    onVisited);
            }
        }


        static void CheckOut(Workspace workspace, GitRepositoryName name, GitCommitName commit)
        {
            using (TraceExtensions.Step("Checking out " + name + " to " + commit))
            {
                Debug.Print("Checking out {0} to {1}", name, commit);
                new GitRepository(workspace.FindRepository(name).RootPath).Checkout(commit);
            }
        }

    }

}
