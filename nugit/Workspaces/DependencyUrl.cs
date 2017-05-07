using System;
using MacroGuards;
using MacroGit;

namespace nugit.Workspaces
{

    /// <summary>
    /// A nugit dependency URL
    /// </summary>
    ///
    /// <remarks>
    /// Dependency URLs consist of regular Git repository URLs plus optional commit specifiers as URL fragments
    /// </remarks>
    ///
    /// <example>
    /// No commit specified, implies <c>master</c>:
    /// http://example.com/path/to/repo.git
    /// </example>
    ///
    /// <example>
    /// Branch:
    /// http://example.com/path/to/repo.git#branch-name
    /// </example>
    ///
    /// <example>
    /// Tag:
    /// http://example.com/path/to/repo.git#tag-name
    /// </example>
    ///
    /// <example>
    /// SHA-1:
    /// http://example.com/path/to/repo.git#0d11b76bd7ff16a24c6390fb5f75017ba59eee42
    /// </example>
    ///
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Usage",
        "CA2237:MarkISerializableTypesWithSerializable",
        Justification = "Don't care")]
    public class DependencyUrl
        : Uri
    {

        const string DefaultCommitName = "master";


        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "Covered by Uri.ToString()")]
        public static implicit operator string(DependencyUrl url)
        {
            if (url == null) return null;
            return url.ToString();
        }


        public DependencyUrl(Dependency dependency)
            : this(
                string.Concat(
                    Guard.NotNull(dependency, nameof(dependency)).Url,
                    "#",
                    Guard.NotNull(dependency, nameof(dependency)).CommitName))
        {
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength",
            Justification = "Test for empty string communicates intention the clearest")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#",
            Justification = "This is a constructor for a type of Uri")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string",
            Justification = "Staying consistent with System.Uri constructor")]
        public DependencyUrl(string urlString)
            : base(
                Guard.NotNull(urlString, nameof(urlString)),
                UriKind.Absolute)
        {
            GitUrl url;
            try
            {
                url = new GitUrl(GetLeftPart(UriPartial.Path));
            }
            catch (FormatException fe)
            {
                throw new FormatException("Not a valid Git repository URL", fe);
            }

            GitCommitName commitName = new GitCommitName(DefaultCommitName);
            if (Fragment.Length > 1)
            {
                try
                {
                    commitName = new GitCommitName(Fragment.Substring(1));
                }
                catch (FormatException fe)
                {
                    throw new FormatException("URL fragment is not a valid Git commit name", fe);
                }
            }

            if (Query != "")
                throw new FormatException("Query components are not permitted in dependency URLs");

            Dependency = new Dependency(url, commitName);
        }


        /// <summary>
        /// The dependency expressed by the URL
        /// </summary>
        ///
        public Dependency Dependency
        {
            get;
        }

    }

}
