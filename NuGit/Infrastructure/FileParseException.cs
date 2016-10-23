using System;
using System.Globalization;
using NuGit.Infrastructure;

namespace NuGit.Infrastructure
{

    /// <summary>
    /// An error occurred parsing a file
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
    public class FileParseException
        : UserErrorException
    {

        public FileParseException(string description, int lineNumber, string line)
            : this(description, lineNumber, line, null)
        {
        }


        public FileParseException(string description, int lineNumber, string line, Exception innerException)
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
        /// User-facing description of the parse error
        /// </summary>
        ///
        public string Description
        {
            get;
            private set;
        }


        /// <summary>
        /// Path to the file being parsed
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
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Error in {2} on line {3}{0}" +
                    "  {1}{0}" +
                    "  {4}",
                    Environment.NewLine,
                    Description,
                    string.IsNullOrWhiteSpace(Path) ? ".nugit" : Path,
                    LineNumber,
                    Line);
            }
        }

    }

}
