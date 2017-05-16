using System;
using System.Collections.Generic;
using System.IO;
using MacroExceptions;

namespace nugit
{

    /// <summary>
    /// <c>.nugit</c> parser
    /// </summary>
    ///
    public static class DotNugitParser
    {

        const string PROGRAM_PREFIX = "program: ";


        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1820:TestForEmptyStringsUsingStringLength",
            Justification = "Testing against \"\" expresses intent more clearly")]
        public static DotNugit Parse(IEnumerable<string> lines)
        {
            if (lines == null) throw new ArgumentNullException("lines");

            var dependencies = new List<Dependency>();
            var programs = new List<string>();

            int lineNumber = 0;
            foreach (string line in lines)
            {
                lineNumber++;

                //
                // Empty / whitespace-only
                //
                if (string.IsNullOrWhiteSpace(line)) continue;

                //
                // # <comment>
                //
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;

                //
                // program: <program>
                //
                if (line.StartsWith(PROGRAM_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    var program = line.Substring(PROGRAM_PREFIX.Length).Trim();
                    if (program == "")
                        throw new TextFileParseException(
                            "Expected <program>",
                            lineNumber + 1,
                            line);
                    program = program.Replace('/', '\\');
                    program = program.Replace('\\', Path.DirectorySeparatorChar);
                    programs.Add(program);
                    continue;
                }

                //
                // <dependencyurl>
                //
                DependencyUrl url;
                try
                {
                    url = new DependencyUrl(line.Trim());
                }
                catch (FormatException fe)
                {
                    throw new TextFileParseException(
                        "Invalid dependency URL: " + fe.Message,
                        lineNumber + 1,
                        line,
                        fe);
                }

                dependencies.Add(url.Dependency);
            }

            return new DotNugit(dependencies, programs);
        }

    }

}
