using System;
using System.IO;
using NuGit.Infrastructure;
using NuGit.Git;
using NuGit.Workspaces;

namespace NuGit.FileSystemWorkspaces
{

    /// <summary>
    /// A repository directory
    /// </summary>
    ///
    public class FileSystemRepository
        : IRepository
    {

        internal FileSystemRepository(FileSystemWorkspace workspace, GitRepositoryName name)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (name == null) throw new ArgumentNullException("name");
            _workspace = workspace;
            Name = name;
            RootPath = Path.Combine(workspace.RootPath, name);
        }


        readonly FileSystemWorkspace _workspace;


        public string RootPath
        {
            get;
            private set;
        }


        /// <inheritdoc/>
        public IWorkspace Workspace
        {
            get { return _workspace; }
        }


        /// <inheritdoc/>
        public GitRepositoryName Name
        {
            get;
            private set;
        }


        /// <inheritdoc/>
        public void Checkout(GitCommitName commit)
        {
            // TODO If uncommitted changes, error
            if (ProcessExtensions.Invoke("git", "-C", RootPath, "checkout", commit) != 0)
                throw new UserErrorException("git checkout failed");
        }


        /// <inheritdoc/>
        public DotNuGit GetDotNuGit()
        {
            string path = Path.Combine(RootPath, ".nugit");
            if (!File.Exists(path)) return new DotNuGit();
            try
            {
                return DotNuGitParser.Parse(File.ReadLines(path));
            }
            catch (FileParseException e)
            {
                e.Path = path;
                throw;
            }
        }

    }

}
