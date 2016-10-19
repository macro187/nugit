using System;
using System.IO;

namespace NuGit
{

    /// <inheritdoc/>
    public class Workspace
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
        /// Full path to the workspace root directory
        /// </summary>
        ///
        public string RootPath
        {
            get;
            private set;
        }


        /// <inheritdoc/>
        public IRepository FindRepository(RepositoryName name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Blank", "name");

            if (!Directory.Exists(Path.Combine(RootPath, name))) return null;

            // TODO Cache
            return new Repository(this, name);
        }


        /// <inheritdoc/>
        public IRepository CloneRepository(GitUrl gitUrl)
        {
            if (gitUrl == null) throw new ArgumentNullException("gitUrl");

            if (ProcessExtensions.Invoke("git", "-C", RootPath, "clone", gitUrl) != 0)
                throw new NuGitUserErrorException("git clone failed");

            return FindRepository(gitUrl.RepositoryName);
        }

    }

}
