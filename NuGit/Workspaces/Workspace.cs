using System;
using System.IO;
using NuGit.Infrastructure;
using NuGit.Git;

namespace NuGit.Workspaces
{

    /// <summary>
    /// A root directory that contains repository subdirectories
    /// </summary>
    ///
    public class Workspace
    {

        /// <summary>
        /// Initialise a new workspace
        /// </summary>
        ///
        /// <param name="rootPath">
        /// Path to the workspace's root directory
        /// </param>
        ///
        public Workspace(string rootPath)
        {
            if (rootPath == null) throw new ArgumentNullException("rootPath");
            if (!Directory.Exists(rootPath)) throw new ArgumentException("Not a directory", "rootPath");

            RootPath = Path.GetFullPath(rootPath);
        }


        /// <summary>
        /// Full path to the workspace's root directory
        /// </summary>
        ///
        public string RootPath
        {
            get;
            private set;
        }


        /// <summary>
        /// Get a repository in the workspace
        /// </summary>
        ///
        /// <param name="name">
        /// Name of the repository
        /// </param>
        ///
        /// <returns>
        /// The repository named <paramref name="name"/>
        /// </returns>
        ///
        /// <exception cref="ArgumentException">
        /// No repository named <paramref name="name"/> exists in the workspace
        /// </exception>
        ///
        public Repository GetRepository(GitRepositoryName name)
        {
            var repository = FindRepository(name);
            if (repository == null)
                throw new ArgumentException(
                    StringExtensions.FormatInvariant(
                        "No repository named '{0}' in workspace",
                        name),
                    "name");
            return repository;
        }


        /// <summary>
        /// Look for a repository in the workspace
        /// </summary>
        ///
        /// <param name="name">
        /// Name of the sought-after repository
        /// </param>
        ///
        /// <returns>
        /// The repository in the workspace named <paramref name="name"/>
        /// - OR -
        /// <c>null</c> if no such repository exists
        /// </returns>
        ///
        public Repository FindRepository(GitRepositoryName name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Blank", "name");

            if (!Directory.Exists(Path.Combine(RootPath, name))) return null;

            // TODO Cache
            return new Repository(this, name);
        }

    }

}
