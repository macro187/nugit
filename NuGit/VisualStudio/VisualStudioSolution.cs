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
    /// A Visual Studio solution (<c>.sln</c>) file
    /// </summary>
    ///
    /// <remarks>
    /// This class contains the entire contents of the file, and can interpret and modify some parts of it without
    /// disturbing the others.
    /// </remarks>
    ///
    public class VisualStudioSolution
    {
        
        const string SolutionFolderTypeId = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";


        /// <summary>
        /// Find and load the solution in a directory
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
        /// Load a solution from a <c>.sln</c> file
        /// </summary>
        ///
        /// <remarks>
        /// This constructor can throw IO-related exceptions, see <see cref="File.ReadLines(string)"/> for details.
        /// </remarks>
        ///
        public VisualStudioSolution(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            Path = path;
            _lines = File.ReadLines(path).ToList();
            Load();
        }


        /// <summary>
        /// The location of the <c>.sln</c> file
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


        public int GlobalStartLineNumber
        {
            get; private set;
        }


        /// <summary>
        /// Project references
        /// </summary>
        ///
        public IEnumerable<VisualStudioProjectReference> ProjectReferences
        {
            get { return _projectReferences; }
        }

        IList<VisualStudioProjectReference> _projectReferences;


        /// <summary>
        /// Solution folders
        /// </summary>
        ///
        /// <remarks>
        /// Solution folders are implemented as a special kind of project reference
        /// </remarks>
        ///
        public IEnumerable<VisualStudioProjectReference> SolutionFolders
        {
            get { return ProjectReferences.Where(p => p.TypeId == SolutionFolderTypeId); }
        }


        /// <summary>
        /// Build configuration mappings
        /// </summary>
        ///
        public IEnumerable<VisualStudioBuildConfigurationMapping> BuildConfigurationMappings
        {
            get { return _buildConfigurationMappings; }
        }

        IList<VisualStudioBuildConfigurationMapping> _buildConfigurationMappings;


        /// <summary>
        /// Nested project entries
        /// </summary>
        ///
        public IEnumerable<VisualStudioNestedProject> NestedProjects
        {
            get { return _nestedProjects; }
        }

        IList<VisualStudioNestedProject> _nestedProjects;


        /// <summary>
        /// Interpret information in the solution file
        /// </summary>
        ///
        void Load()
        {
            GlobalStartLineNumber = 0;
            _projectReferences = new List<VisualStudioProjectReference>();
            _nestedProjects = new List<VisualStudioNestedProject>();
            _buildConfigurationMappings = new List<VisualStudioBuildConfigurationMapping>();

            int lineNumber = 0;
            int projectReferenceStart = 0;
            int nestedProjectsStart = 0;
            int projectConfigurationsStart = 0;
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
                // In project configurations block
                //
                if (projectConfigurationsStart != 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        projectConfigurationsStart = 0;
                        continue;
                    }

                    match = Regex.Match(line, "^\\s*([^.]+)\\.([^.]+)\\.(.+) = (.+)$");
                    if (!match.Success)
                        throw new FileParseException(
                            "Expected '{guid}.{configuration}.{property} = {configuration}'",
                            lineNumber,
                            line);

                    _buildConfigurationMappings.Add(
                        new VisualStudioBuildConfigurationMapping(
                            match.Groups[1].Value,
                            match.Groups[2].Value,
                            match.Groups[3].Value,
                            match.Groups[4].Value.Trim(),
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
                // Starting nested projects block
                //
                if (line.Trim() == "GlobalSection(ProjectConfigurationPlatforms) = postSolution")
                {
                    projectConfigurationsStart = lineNumber;
                    continue;
                }

                //
                // Start of "Global" section
                //
                if (line.Trim() == "Global")
                {
                    GlobalStartLineNumber = lineNumber;
                    continue;
                }

                //
                // Nothing special
                //
            }

            if (GlobalStartLineNumber == 0)
                throw new FileParseException("No 'Global' section in file", 1, "");
        }


        /// <summary>
        /// Save information to the <c>.sln</c> file
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

    }

}
