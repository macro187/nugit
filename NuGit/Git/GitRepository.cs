using System;
using System.IO;
using NuGit.Infrastructure;
using MacroDiagnostics;

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

            if (ProcessExtensions.Execute(true, true, "git", "-C", workspacePath, "clone", gitUrl) != 0)
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
            if (ProcessExtensions.Execute(true, true, "git", "-C", repositoryPath, "checkout", commit) != 0)
                throw new UserErrorException("git checkout failed");
        }


        /// <summary>
        /// Get the unique hash identifier of the currently-checked-out revision of a Git repository
        /// </summary>
        ///
        public static string GetRevision(string repositoryPath)
        {
            if (repositoryPath == null)
                throw new ArgumentNullException("repositoryPath");
            if (!Directory.Exists(repositoryPath))
                throw new ArgumentException("Repository path doesn't exist", "repositoryPath");

            var result = ProcessExtensions.ExecuteCaptured(
                false, false,
                "git", "-C", repositoryPath, "rev-parse", "HEAD");
            
            if (result.ExitCode != 0)
                throw new UserErrorException("git checkout failed");

            return result.StandardOutput.Trim();
        }

    }
}
