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
        /// Locate a workspace given a path contained within it
        /// </summary>
        ///
        /// <param name="containedPath">
        /// A path that may be contained within a workspace
        /// </param>
        ///
        /// <returns>
        /// The workspace the path is in
        /// - OR -
        /// <c>null</c> if the path is not in a workspace
        /// </returns>
        ///
        public static Workspace LocateFrom(string containedPath)
        {
            if (containedPath == null) throw new ArgumentNullException("containedPath");

            string path = Path.GetFullPath(containedPath);
            while (true)
            {
                string parent = Path.GetDirectoryName(path);
                if (parent == path) break;
                if (Directory.Exists(Path.Combine(path, ".git"))) return new Workspace(parent);
                path = parent;
            }
            return null;
        }


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
