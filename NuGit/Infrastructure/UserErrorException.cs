using System;

namespace NuGit.Infrastructure
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
    public class UserErrorException
        : Exception
    {

        public UserErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        public UserErrorException(string message)
            : base(message)
        {
        }


        public UserErrorException()
            : base()
        {
        }

    }

}
