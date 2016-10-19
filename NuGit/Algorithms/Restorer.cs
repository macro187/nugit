using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NuGit
{

    /// <summary>
    /// Dependency resolution algorithm
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Recursively clones and checks out required versions of required repositories.
    /// </para>
    /// <para>
    /// Processes dependencies recursively breadth-first.  First-encountered (or "shallowest") version of a given
    /// dependency wins.  Subsequent encounters for a different version of the same dependency produce a warning.
    /// </para>
    /// </remarks>
    ///
    public static class Restorer
    {
        
        /// <summary>
        /// Restore a repository's dependencies into its workspace
        /// </summary>
        ///
        public static void Restore(IRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            Restore(
                repository.Workspace,
                repository.GetDotNuGit().Dependencies,
                repository,
                new Dictionary<RepositoryName, GitCommitName>() { { repository.Name, new GitCommitName("HEAD") } },
                new HashSet<RepositoryName>() { repository.Name }
                );
        }


        /// <summary>
        /// Restore specified dependencies into a specified workspace
        /// </summary>
        ///
        public static void Restore(IWorkspace workspace, IEnumerable<DependencyInfo> dependencies)
        {
            Restore(
                workspace,
                dependencies,
                null,
                new Dictionary<RepositoryName,GitCommitName>(),
                new HashSet<RepositoryName>());
        }


        static void Restore(
            IWorkspace workspace,
            IEnumerable<DependencyInfo> dependencies,
            IRepository requiredBy,
            IDictionary<RepositoryName,GitCommitName> checkedOut,
            ISet<RepositoryName> restored
            )
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (dependencies == null) throw new ArgumentNullException("dependencies");
            if (checkedOut == null) throw new ArgumentNullException("checkedOut");
            if (restored == null) throw new ArgumentNullException("restored");

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
            // Recurse
            //
            foreach (var d in dependencies)
            {
                var name = d.Url.RepositoryName;
                var repo = workspace.FindRepository(name);
                if (restored.Contains(name)) continue;

                restored.Add(name);

                Restore(
                    workspace,
                    repo.GetDotNuGit().Dependencies,
                    repo,
                    checkedOut,
                    restored);
            }
        }

    }

}
