using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NuGit.Infrastructure
{

    public static class ProcessExtensions
    {

        /// <summary>
        /// Run a program
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// The full command line is echoed to <see cref="Trace.WriteLine(string)"/>.
        /// </para>
        /// <para>
        /// The program's stdout and stderr output are echoed to <see cref="Trace.WriteLine(string)"/>.
        /// </para>
        /// <para>
        /// Multiple blank output lines are collapsed.
        /// </para>
        /// </remarks>
        ///
        /// <param name="program">
        /// Name of program to run, if it is on the system <c>path</c>
        /// - OR -
        /// Full path to the program to run
        /// </param>
        ///
        /// <param name="arguments">
        /// List of unquoted arguments to pass to the program.  Arguments may contain space characters, but not quote
        /// characters.
        /// </param>
        ///
        /// <returns>
        /// The program exit code
        /// </returns>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1820:TestForEmptyStringsUsingStringLength",
            Justification = "Comparing to empty string communicates intent more clearly")]
        public static int Invoke(string program, params string[] arguments)
        {
            if (program == null) throw new ArgumentNullException("program");
            if (string.IsNullOrWhiteSpace(program)) throw new ArgumentException("Empty or whitespace", "program");

            var programString = 
                program.Contains(" ")
                    ? string.Concat("\"", program, "\"")
                    : program;

            var argumentsString =
                string.Join(
                    " ",
                    arguments.Select(s =>
                        s.Contains(" ")
                            ? string.Concat("\"", s, "\"")
                            : s));

            var commandLine =
                argumentsString == ""
                    ? programString
                    : programString + " " + argumentsString;
            
            using (var proc = new Process())
            {
                bool exited = false;
                bool lastLineWasBlank = false;
                object outputLock = new object();

                proc.StartInfo.FileName = program;
                proc.StartInfo.Arguments = argumentsString;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.OutputDataReceived += (_,e) => {
                    lock (outputLock)
                    {
                        var line = (e.Data ?? "").Trim();
                        var thisLineIsBlank = (line == "");
                        if (thisLineIsBlank && lastLineWasBlank) return;
                        Trace.WriteLine(line);
                        lastLineWasBlank = thisLineIsBlank;
                    }
                };
                proc.ErrorDataReceived += (_,e) => {
                    lock (outputLock)
                    {
                        var line = e.Data ?? "";
                        var thisLineIsBlank = (line == "");
                        if (thisLineIsBlank && lastLineWasBlank) return;
                        Trace.WriteLine(line);
                        lastLineWasBlank = thisLineIsBlank;
                    }
                };
                proc.EnableRaisingEvents = true;
                proc.Exited += (_,__) => exited = true;

                Trace.WriteLine(commandLine);

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                while (!exited) Thread.Yield();

                return proc.ExitCode;
            }

        }

    }

}
