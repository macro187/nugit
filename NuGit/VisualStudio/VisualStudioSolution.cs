using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Initialise a new solution from the lines of text in the <c>.sln</c> file
        /// </summary>
        ///
        public VisualStudioSolution(IEnumerable<string> lines)
        {
            if (lines == null) throw new ArgumentNullException("lines");
            _lines = lines.ToList();
            Parse();
        }


        /// <summary>
        /// The lines of text in the file
        /// </summary>
        ///
        public IEnumerable<string> Lines
        {
            get { return _lines; }
        }

        IList<string> _lines;


        /// <summary>
        /// Parse information from file contents
        /// </summary>
        ///
        void Parse()
        {
            _projectReferences = new List<VisualStudioProjectReference>();

            int lineNumber = 0;
            int startLine = 0;
            string id = "";
            string typeId = "";
            string name = "";
            string location = "";
            foreach (var line in Lines)
            {
                lineNumber++;

                //
                // In a project reference
                //
                if (startLine != 0)
                {
                    if (line.Trim() == "EndProject")
                    {
                        _projectReferences.Add(
                            new VisualStudioProjectReference(
                                id, typeId, name, location, startLine, lineNumber - startLine + 1));
                        startLine = 0;
                        id = "";
                        typeId = "";
                        name = "";
                        location = "";
                    }
                    continue;
                }

                //
                // Starting a project reference
                //
                var match = Regex.Match(line, "Project\\(\"([^\"]*)\"\\) = \"([^\"]*)\", \"([^\"]*)\", \"([^\"]*)\"");
                if (match.Success)
                {
                    if (startLine != 0)
                        throw new FileParseException(
                            "Expected 'EndProject'",
                            lineNumber,
                            line);
                    startLine = lineNumber;
                    typeId = match.Groups[1].Value;
                    name = match.Groups[2].Value;
                    location = match.Groups[3].Value;
                    id = match.Groups[4].Value;
                    continue;
                }

                //
                // Nothing special
                //
            }
        }


        public IEnumerable<VisualStudioProjectReference> ProjectReferences
        {
            get { return _projectReferences; }
        }

        IList<VisualStudioProjectReference> _projectReferences;

    }

}
