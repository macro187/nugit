using System;
using System.IO;
using NuGit.Infrastructure;
using NuGit.Git;
using NuGit.Workspaces;

namespace NuGit.FileSystemWorkspaces
{

    /// <inheritdoc/>
    public class FileSystemWorkspace
        : IWorkspace
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
        public static FileSystemWorkspace LocateFrom(string containedPath)
        {
            if (containedPath == null) throw new ArgumentNullException("containedPath");

            string path = Path.GetFullPath(containedPath);
            while (true)
            {
                string parent = Path.GetDirectoryName(path);
                if (parent == path) break;
                if (Directory.Exists(Path.Combine(path, ".git"))) return new FileSystemWorkspace(parent);
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
        public FileSystemWorkspace(string rootPath)
        {
            if (rootPath == null) throw new ArgumentNullException("rootPath");
            if (!Directory.Exists(rootPath)) throw new ArgumentException("Not a directory", "rootPath");

            RootPath = Path.GetFullPath(rootPath);
        }


        /// <summary>
        /// Full path to the workspace root directory
        /// </summary>
        ///
        public string RootPath
        {
            get;
            private set;
        }


        /// <inheritdoc/>
        IRepository IWorkspace.FindRepository(GitRepositoryName name)
        {
            return FindRepository(name);
        }


        public FileSystemRepository FindRepository(GitRepositoryName name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Blank", "name");

            if (!Directory.Exists(Path.Combine(RootPath, name))) return null;

            // TODO Cache
            return new FileSystemRepository(this, name);
        }


        /// <inheritdoc/>
        IRepository IWorkspace.CloneRepository(GitUrl gitUrl)
        {
            return CloneRepository(gitUrl);
        }


        public IRepository CloneRepository(GitUrl gitUrl)
        {
            if (gitUrl == null) throw new ArgumentNullException("gitUrl");

            if (ProcessExtensions.Invoke("git", "-C", RootPath, "clone", gitUrl) != 0)
                throw new UserErrorException("git clone failed");

            return FindRepository(gitUrl.RepositoryName);
        }

    }

}
