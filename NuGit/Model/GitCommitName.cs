using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NuGit
{

    /// <summary>
    /// Name of a Git commit
    /// </summary>
    ///
    public class GitCommitName
    {

        /// <summary>
        /// Initialise a new Git commit name
        /// </summary>
        ///
        /// <param name="gitCommitString">
        /// A Git commit string
        /// </param>
        ///
        /// <exception cref="ArgumentNullException">
        /// <paramref name="gitCommitString"/> was <c>null</c>
        /// </exception>
        ///
        /// <exception cref="FormatException">
        /// <paramref name="gitCommitString"/> was not a valid Git commit reference
        /// </exception>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1720:IdentifiersShouldNotContainTypeNames",
            MessageId = "string",
            Justification = "Don't care")]
        public GitCommitName(string gitCommitString)
        {
             if (gitCommitString == null)
                throw new ArgumentNullException("gitCommitString");
             if (string.IsNullOrEmpty(gitCommitString))
                throw new FormatException("Empty string");
             if (gitCommitString.Contains(' '))
                throw new FormatException("Contains whitespace");
             if (!Regex.IsMatch(gitCommitString, @"^[A-Za-z0-9_.-/]+$"))
                throw new FormatException("Contains invalid characters");

             _value = gitCommitString;
        }
        
        
        string _value;


        /// <summary>
        /// Get the Git commit name as a string
        /// </summary>
        ///
        public override string ToString()
        {
            return _value;
        }


        public static implicit operator string(GitCommitName gitCommit)
        {
            if (gitCommit == null) return null;
            return gitCommit.ToString();
        }

    }

}
