using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using nugit.Infrastructure;

namespace nugit.VisualStudio
{

    /// <summary>
    /// A Visual Studio project (<c>.csproj</c>) file
    /// </summary>
    ///
    /// <remarks>
    /// This class contains the entire contents of the file, and can interpret and modify some parts of it without
    /// disturbing the others.
    /// </remarks>
    ///
    public class VisualStudioProject
    {

        /// <summary>
        /// Load a project from a <c>.csproj</c> file
        /// </summary>
        ///
        /// <remarks>
        /// This constructor can throw IO-related exceptions, see <see cref="File.ReadLines(string)"/> for details.
        /// </remarks>
        ///
        public VisualStudioProject(string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            Path = path;
            _lines = File.ReadLines(path).ToList();
            Load();
        }


        /// <summary>
        /// The location of the <c>.csproj</c> file
        /// </summary>
        ///
        public string Path
        {
            get;
            private set;
        }


        /// <summary>
        /// Lines of text
        /// </summary>
        ///
        public IEnumerable<string> Lines
        {
            get { return _lines; }
        }

        IList<string> _lines;


        /// <summary>
        /// Project type guids
        /// </summary>
        ///
        public ISet<string> ProjectTypeGuids
        {
            get { return _projectTypeGuids; }
        }

        ISet<string> _projectTypeGuids;


        /// <summary>
        /// Interpret information in the solution file
        /// </summary>
        ///
        void Load()
        {
            _projectTypeGuids = new HashSet<string>();
            int lineNumber = -1;
            Match match;
            foreach (var line in Lines)
            {
                lineNumber++;
                
                //
                // Ignore blank lines and comments
                //
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Trim().StartsWith("#", StringComparison.Ordinal)) continue;

                //
                // ProjectTypeGuids
                //
                match = Regex.Match(line, "^\\s*<ProjectTypeGuids>([^<]+)</ProjectTypeGuids>\\s*$");
                if (match.Success)
                {
                    _projectTypeGuids.AddRange(match.Groups[1].Value.Split(';'));
                }

                //
                // Nothing special
                //
            }
        }


        /// <summary>
        /// Save information to the <c>.csproj</c> file
        /// </summary>
        ///
        public void Save()
        {
            //
            // Visual Studio writes .csproj files in UTF-8 with BOM and Windows-style line endings
            //
            var encoding = new UTF8Encoding(true);
            var newline = "\r\n";

            using (var f = new StreamWriter(Path, false, encoding))
            {
                f.NewLine = newline;
                foreach (var line in Lines)
                {
                    f.WriteLine(line);
                }
            }
        }

    }

}
