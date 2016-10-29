using System;
using System.IO;
using NuGit.Infrastructure;
using NuGit.Git;

namespace NuGit.Workspaces
{

    /// <summary>
    /// A repository subdirectory
    /// </summary>
    ///
    public class Repository
    {

        internal Repository(Workspace workspace, GitRepositoryName name)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (name == null) throw new ArgumentNullException("name");
            RootPath = Path.Combine(workspace.RootPath, name);
            Name = name;
            Workspace = workspace;
        }


        /// <summary>
        /// Full path to the repository's root directory
        /// </summary>
        ///
        public string RootPath
        {
            get;
            private set;
        }


        /// <summary>
        /// Workspace the repository is in
        /// </summary>
        ///
        public Workspace Workspace
        {
            get;
            private set;
        }


        /// <summary>
        /// Name of the repository subdirectory
        /// </summary>
        ///
        public GitRepositoryName Name
        {
            get;
            private set;
        }


        /// <summary>
        /// Switch the repository to a particular Git commit
        /// </summary>
        ///
        /// <remarks>
        /// Can affect the result of <see cref="GetDotNuGit()"/>.
        /// </remarks>
        ///
        public void Checkout(GitCommitName commit)
        {
            // TODO If uncommitted changes, error
            if (ProcessExtensions.Invoke("git", "-C", RootPath, "checkout", commit) != 0)
                throw new UserErrorException("git checkout failed");
        }


        /// <summary>
        /// Get the repository's .nugit information
        /// </summary>
        ///
        /// <remarks>
        /// Result can be affected by <see cref="Checkout()"/>.
        /// </remarks>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Emphasise that information is reread on each call")]
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
