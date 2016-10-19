using System;
using System.Text.RegularExpressions;

namespace NuGit
{

    /// <summary>
    /// A Git repository name
    /// </summary>
    ///
    /// <remarks>
    /// Case-insensitive
    /// </remarks>
    ///
    public class RepositoryName
        : IEquatable<RepositoryName>
    {

        /// <summary>
        /// Initialise a new repository name
        /// </summary>
        ///
        /// <param name="repositoryNameString">
        /// The repository name
        /// </param>
        ///
        /// <exception cref="ArgumentNullException">
        /// <paramref name="repositoryNameString"/> was <c>null</c>
        /// </exception>
        ///
        /// <exception cref="FormatException">
        /// <paramref name="repositoryNameString"/> was not a valid repository name
        /// </exception>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA1720:IdentifiersShouldNotContainTypeNames",
            MessageId = "string",
            Justification = "Don't care")]
        public RepositoryName(string repositoryNameString)
        {
            if (repositoryNameString == null)
                throw new ArgumentNullException("repositoryNameString");
            if (!Regex.IsMatch(repositoryNameString, @"^[A-Za-z0-9_.-]+$"))
                throw new FormatException("Contains invalid characters");

             _value = repositoryNameString;
        }
        
        
        string _value;


        public static implicit operator string(RepositoryName repositoryName)
        {
            if (repositoryName == null) return null;
            return repositoryName.ToString();
        }



        #region IEquatable<RepositoryName>

        public bool Equals(RepositoryName other)
        {
            if (other == null) return false;
            return ToString().Equals(other.ToString(), StringComparison.OrdinalIgnoreCase);
        }


        public static bool operator==(RepositoryName oneName, RepositoryName anotherName)
        {
            if (object.ReferenceEquals(oneName, null) && object.ReferenceEquals(anotherName, null)) return true;
            if (object.ReferenceEquals(oneName, null) || object.ReferenceEquals(anotherName, null)) return false;
            return oneName.Equals(anotherName);
        }


        public static bool operator!=(RepositoryName oneName, RepositoryName anotherName)
        {
            return !(oneName == anotherName);
        }

        #endregion



        #region object

        /// <summary>
        /// Get the repository name as a string
        /// </summary>
        ///
        public override string ToString()
        {
            return _value;
        }


        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return Equals(obj as RepositoryName);
        }


        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ToString().ToUpperInvariant().GetHashCode();
        }

        #endregion
    }

}
