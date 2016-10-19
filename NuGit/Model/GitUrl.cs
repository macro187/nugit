using System;
using System.IO;
using System.Linq;

namespace NuGit
{

    /// <summary>
    /// A Git repository URL
    /// </summary>
    ///
    /// <remarks>
    /// See <c>https://git-scm.com/docs/git-fetch</c> for details
    /// </remarks>
    ///
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Usage",
        "CA2237:MarkISerializableTypesWithSerializable",
        Justification = "Don't care")]
    public class GitUrl
        : Uri
    {

        /// <summary>
        /// Valid Git URL schemes
        /// </summary>
        ///
        static readonly string[] GitSchemes = new [] { "FILE", "SSH", "GIT", "HTTP", "HTTPS" };


        static string CheckGitUrlString(string gitUrlString)
        {
            if (gitUrlString == null) throw new ArgumentNullException("gitUrlString");
            return gitUrlString;
        }


        /// <summary>
        /// Initialise a new Git URL
        /// </summary>
        ///
        /// <param name="gitUrlString">
        /// A Git URL string
        /// </param>
        ///
        /// <exception cref="ArgumentNullException">
        /// <paramref name="gitUrlString"/> was <c>null</c>
        /// </exception>
        ///
        /// <exception cref="FormatException">
        /// <paramref name="gitUrlString"/> was not a valid Git URL
        /// </exception>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1820:TestForEmptyStringsUsingStringLength",
            Justification = "Testing against empty string conveys intent more clearly")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1054:UriParametersShouldNotBeStrings",
            MessageId = "0#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1720:IdentifiersShouldNotContainTypeNames",
            MessageId = "string")]
        public GitUrl(string gitUrlString)
            : base(CheckGitUrlString(gitUrlString), UriKind.Absolute)
        {
            if (!GitSchemes.Contains(Scheme.ToUpperInvariant()))
                throw new FormatException("Invalid Git URL scheme");
            if (Query != "")
                throw new FormatException("Query components are not permitted in Git URLs");

            RepositoryName = new RepositoryName(Path.GetFileNameWithoutExtension(AbsolutePath));

            Commit = null;
            if (Fragment.Length > 1)
            {
                Commit = new GitCommitName(Fragment.Substring(1).Trim());
            }
        }


        /// <summary>
        /// The final path component, minus any filename extension
        /// </summary>
        ///
        public RepositoryName RepositoryName
        {
            get;
            private set;
        }


        /// <summary>
        /// Commit reference specified as the URL fragment (i.e. value following the hash # character)
        /// </summary>
        ///
        public GitCommitName Commit
        {
            get;
            private set;
        }


        /// <summary>
        /// Get the URL as a string, minus commit fragment if present
        /// </summary>
        ///
        public override string ToString()
        {
            return GetLeftPart(UriPartial.Path);
        }


        public static implicit operator string(GitUrl gitUrl)
        {
            if (gitUrl == null) return null;
            return gitUrl.ToString();
        }

    }

}
