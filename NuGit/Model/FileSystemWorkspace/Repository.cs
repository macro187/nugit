using System;
using System.IO;

namespace NuGit
{

    /// <summary>
    /// A repository directory
    /// </summary>
    ///
    public class Repository
        : IRepository
    {

        internal Repository(Workspace workspace, RepositoryName name)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (name == null) throw new ArgumentNullException("name");
            _workspace = workspace;
            Name = name;
            _rootPath = Path.Combine(workspace.RootPath, name);
        }


        readonly Workspace _workspace;


        readonly string _rootPath;


        /// <inheritdoc/>
        public IWorkspace Workspace
        {
            get { return _workspace; }
        }


        /// <inheritdoc/>
        public RepositoryName Name
        {
            get;
            private set;
        }


        /// <inheritdoc/>
        public void Checkout(GitCommitName commit)
        {
            // TODO If uncommitted changes, error
            if (ProcessExtensions.Invoke("git", "-C", _rootPath, "checkout", commit) != 0)
                throw new NuGitUserErrorException("git checkout failed");
        }


        /// <inheritdoc/>
        public DotNuGit GetDotNuGit()
        {
            string path = Path.Combine(_rootPath, ".nugit");
            if (!File.Exists(path)) return new DotNuGit();
            try
            {
                return DotNuGitParser.Parse(File.ReadLines(path));
            }
            catch (DotNuGitParseException e)
            {
                e.Path = path;
                throw;
            }
        }

    }

}
