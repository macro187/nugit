using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NuGit.Infrastructure;

namespace NuGit.VisualStudio
{

    /// <summary>
    /// Information in a Visual Studio solution file
    /// </summary>
    ///
    public class VisualStudioSolution
    {

        /// <summary>
        /// Find a Visual Studio solution in a directory
        /// </summary>
        ///
        /// <returns>
        /// The solution
        /// - OR -
        /// <c>null</c> if the directory contained no solution
        /// </returns>
        ///
        /// <exception cref="UserErrorException">
        /// The directory contained more than one solution
        /// </exception>
        ///
        public static VisualStudioSolution Find(string directoryPath)
        {
            if (directoryPath == null) throw new ArgumentNullException("directoryPath");
            if (!Directory.Exists(directoryPath))
            {
                throw new ArgumentException("Directory doesn't exist", "directoryPath");
            }

            var slnPaths = Directory.GetFiles(directoryPath, "*.sln");
            if (slnPaths.Length == 0) return null;
            if (slnPaths.Length > 1)
            {
                throw new UserErrorException(
                    StringExtensions.FormatInvariant(
                        "More than one .sln found in {0}",
                        directoryPath));
            }
            var slnPath = slnPaths[0];

            return new VisualStudioSolution(slnPath);
        }


        /// <summary>
        /// Load a solution from an <c>.sln</c> file
        /// </summary>
        ///
        public VisualStudioSolution(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            Path = path;
            _lines = File.ReadLines(path).ToList();
            Load();
        }


        /// <summary>
        /// The location of the solution file
        /// </summary>
        ///
        public string Path
        {
            get;
            private set;
        }


        /// <summary>
        /// Lines of text in the solution file
        /// </summary>
        ///
        public IEnumerable<string> Lines
        {
            get { return _lines; }
        }

        IList<string> _lines;


        /// <summary>
        /// Load information from .sln file
        /// </summary>
        ///
        void Load()
        {
            _projectReferences = new List<VisualStudioProjectReference>();
            _nestedProjects = new List<VisualStudioNestedProject>();

            int lineNumber = 0;
            int projectReferenceStart = 0;
            int nestedProjectsStart = 0;
            string id = "";
            string typeId = "";
            string name = "";
            string location = "";
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
                // In a project reference block
                //
                if (projectReferenceStart != 0)
                {
                    if (line.Trim() == "EndProject")
                    {
                        _projectReferences.Add(
                            new VisualStudioProjectReference(
                                id, typeId, name, location, projectReferenceStart, lineNumber - projectReferenceStart + 1));
                        projectReferenceStart = 0;
                        id = "";
                        typeId = "";
                        name = "";
                        location = "";
                    }
                    continue;
                }

                //
                // In nested projects block
                //
                if (nestedProjectsStart != 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        nestedProjectsStart = 0;
                        continue;
                    }

                    match = Regex.Match(line, "(\\S+) = (\\S+)");
                    if (!match.Success)
                        throw new FileParseException(
                            "Expected '{guid} = {guid}'",
                            lineNumber,
                            line);

                    _nestedProjects.Add(
                        new VisualStudioNestedProject(
                            match.Groups[1].Value,
                            match.Groups[2].Value,
                            lineNumber));

                    continue;
                }

                //
                // Starting a project reference
                //
                match = Regex.Match(line, "Project\\(\"([^\"]*)\"\\) = \"([^\"]*)\", \"([^\"]*)\", \"([^\"]*)\"");
                if (match.Success)
                {
                    if (projectReferenceStart != 0)
                        throw new FileParseException(
                            "Expected 'EndProject'",
                            lineNumber,
                            line);

                    projectReferenceStart = lineNumber;
                    typeId = match.Groups[1].Value;
                    name = match.Groups[2].Value;
                    location = match.Groups[3].Value;
                    id = match.Groups[4].Value;

                    continue;
                }

                //
                // Starting nested projects block
                //
                if (line.Trim() == "GlobalSection(NestedProjects) = preSolution")
                {
                    nestedProjectsStart = lineNumber;
                    continue;
                }

                //
                // Nothing special
                //
            }
        }


        /// <summary>
        /// Save information to .sln file
        /// </summary>
        ///
        public void Save()
        {
            //
            // Visual Studio writes .sln files in UTF-8 with BOM and Windows-style line endings
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


        /// <summary>
        /// Project references in the solution file
        /// </summary>
        ///
        public IEnumerable<VisualStudioProjectReference> ProjectReferences
        {
            get { return _projectReferences; }
        }

        IList<VisualStudioProjectReference> _projectReferences;


        /// <summary>
        /// Nested project entries in the solution file
        /// </summary>
        ///
        public IEnumerable<VisualStudioNestedProject> NestedProjects
        {
            get { return _nestedProjects; }
        }

        IList<VisualStudioNestedProject> _nestedProjects;

    }

}
