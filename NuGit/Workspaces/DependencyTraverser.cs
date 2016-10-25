using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGit.Infrastructure;
using NuGit.Git;

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
        /// Traverse a repository's dependencies
        /// </summary>
        ///
        public static void Traverse(IRepository repository)
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
        public static void Traverse(IRepository repository, Action<GitRepositoryName> onVisited)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            Traverse(
                repository.Workspace,
                repository.GetDotNuGit().Dependencies,
                repository,
                new Dictionary<GitRepositoryName, GitCommitName>() { { repository.Name, new GitCommitName("HEAD") } },
                new HashSet<GitRepositoryName>() { repository.Name },
                onVisited
                );
        }


        /// <summary>
        /// Traverse specified dependencies
        /// </summary>
        ///
        public static void Traverse(IWorkspace workspace, IEnumerable<GitDependencyInfo> dependencies)
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
        public static void Traverse(
            IWorkspace workspace,
            IEnumerable<GitDependencyInfo> dependencies,
            Action<GitRepositoryName> onVisited)
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
            IWorkspace workspace,
            IEnumerable<GitDependencyInfo> dependencies,
            IRepository requiredBy,
            IDictionary<GitRepositoryName,GitCommitName> checkedOut,
            ISet<GitRepositoryName> visited,
            Action<GitRepositoryName> onVisited
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
                TraceExtensions.TraceHeading("Fetching " + d.Url.RepositoryName);
                workspace.CloneRepository(d.Url);
            }

            //
            // Check out dependencies to specified commits
            //
            foreach (var d in dependencies)
            {
                var name = d.Url.RepositoryName;
                var commit = d.Version;

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

                TraceExtensions.TraceHeading("Switching " + name + " to " + commit);
                workspace.FindRepository(name).Checkout(commit);
                checkedOut.Add(name, commit);
            }

            //
            // onVisited
            //
            foreach (var d in dependencies)
            {
                var name = d.Url.RepositoryName;
                if (visited.Contains(name)) continue;

                onVisited(name);
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

    }

}
