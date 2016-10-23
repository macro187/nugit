using System;
using System.Globalization;
using NuGit.Infrastructure;

namespace NuGit.VisualStudio
{

    /// <summary>
    /// An error occurred parsing a Visual Studio file
    /// </summary>
    ///
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Design",
        "CA1032:ImplementStandardExceptionConstructors",
        Justification = "Only used internally so don't care")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Microsoft.Usage",
        "CA2237:MarkISerializableTypesWithSerializable",
        Justification = "Only used internally so don't care")]
    public class VisualStudioParseException
        : UserErrorException
    {

        public VisualStudioParseException(string description, int lineNumber, string line)
            : this(description, lineNumber, line, null)
        {
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Naming",
            "CA2204:Literals should be spelled correctly",
            MessageId = "nugit",
            Justification = ".nugit is spelled correctly")]
        public VisualStudioParseException(string description, int lineNumber, string line, Exception innerException)
            : base("", innerException)
        {
            if (description == null) throw new ArgumentNullException("description");
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Blank", "description");
            if (lineNumber < 1) throw new ArgumentOutOfRangeException("lineNumber");
            if (line == null) throw new ArgumentNullException("line");
            if (string.IsNullOrWhiteSpace(line)) throw new ArgumentException("Blank", "line");

            Description = description;
            Path = "";
            LineNumber = lineNumber;
            Line = line;
        }


        /// <summary>
        /// User-facing description of the parse error, if one occurred, otherwise an empty string
        /// </summary>
        ///
        public string Description
        {
            get;
            private set;
        }


        /// <summary>
        /// Path to the Visual Studio file
        /// </summary>
        ///
        public string Path
        {
            get;
            set;
        }


        /// <summary>
        /// 1-based line number the parse error was on
        /// </summary>
        ///
        public int LineNumber
        {
            get;
            private set;
        }


        /// <summary>
        /// Contents of the line the parse error was on
        /// </summary>
        ///
        public string Line
        {
            get;
            private set;
        }


        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return StringExtensions.FormatInvariant(
                    "Error in {2} on line {3}{0}" +
                    "  {1}{0}" +
                    "  {4}",
                    Environment.NewLine,
                    Description,
                    Path,
                    LineNumber,
                    Line);
            }
        }

    }

}
