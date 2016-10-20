using System;

namespace NuGit.Git
{

    /// <summary>
    /// Reference to a specific commit in a specific Git repository
    /// </summary>
    ///
    public class GitDependencyInfo
    {

        /// <summary>
        /// Initialise a new dependency info
        /// </summary>
        ///
        /// <exception cref="ArgumentNullException">
        /// <paramref name="url"/> is <c>null</c>
        /// - OR -
        /// <paramref name="version"/> is <c>null</c>
        /// </exception>
        ///
        public GitDependencyInfo(GitUrl url, GitCommitName version)
        {
            if (url == null)
                throw new ArgumentNullException("url");
            if (version == null)
                throw new ArgumentNullException("version");

            Url = url;
            Version = version;
        }


        /// <summary>
        /// URL of required Git repository
        /// </summary>
        ///
        public GitUrl Url
        {
            get;
            private set;
        }


        /// <summary>
        /// Required commit (branch, tag, hash, etc.)
        /// </summary>
        ///
        public GitCommitName Version
        {
            get;
            private set;
        }

    }

}
