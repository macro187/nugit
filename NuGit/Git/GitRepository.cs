using System;
using System.IO;
using NuGit.Infrastructure;

namespace NuGit.Git
{

    public static class GitRepository
    {

        /// <summary>
        /// Clone a new Git repository into a workspace
        /// </summary>
        ///
        public static void CloneRepository(string workspacePath, GitUrl gitUrl)
        {
            if (workspacePath == null)
                throw new ArgumentNullException("workspacePath");
            if (!Directory.Exists(workspacePath))
                throw new ArgumentException("Workspace path doesn't exist", "workspacePath");
            if (gitUrl == null) throw new ArgumentNullException("gitUrl");

            if (ProcessExtensions.Invoke("git", "-C", workspacePath, "clone", gitUrl) != 0)
                throw new UserErrorException("git clone failed");
        }


        /// <summary>
        /// Switch a Git repository to a particular Git commit
        /// </summary>
        ///
        public static void Checkout(string repositoryPath, GitCommitName commit)
        {
            if (repositoryPath == null)
                throw new ArgumentNullException("repositoryPath");
            if (!Directory.Exists(repositoryPath))
                throw new ArgumentException("Repository path doesn't exist", "repositoryPath");

            // TODO If uncommitted changes, error
            if (ProcessExtensions.Invoke("git", "-C", repositoryPath, "checkout", commit) != 0)
                throw new UserErrorException("git checkout failed");
        }

    }
}
