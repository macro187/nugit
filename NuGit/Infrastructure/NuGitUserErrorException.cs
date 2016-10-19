using System;

namespace NuGit
{

    /// <summary>
    /// A user-facing error
    /// </summary>
    ///
    /// <remarks>
    /// <see cref="Exception.Message"/> is the user-facing error message
    /// </remarks>
    ///
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Usage",
        "CA2237:MarkISerializableTypesWithSerializable",
        Justification = "Internal use only, so don't care")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Design",
        "CA1032:ImplementStandardExceptionConstructors",
        Justification = "Internal use only, so don't care")]
    public class NuGitUserErrorException
        : Exception
    {

        public NuGitUserErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        public NuGitUserErrorException(string message)
            : base(message)
        {
        }


        public NuGitUserErrorException()
            : base()
        {
        }

    }

}
