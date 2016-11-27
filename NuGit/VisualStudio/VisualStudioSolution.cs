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
            if (directoryPath == null)
                throw new ArgumentNullException("directoryPath");
            if (!Directory.Exists(directoryPath))
                throw new ArgumentException("Directory doesn't exist", "directoryPath");

            var slnPaths = Directory.GetFiles(directoryPath, "*.sln");
            if (slnPaths.Length == 0)
                return null;
            if (slnPaths.Length > 1)
                throw new UserErrorException(
                    StringExtensions.FormatInvariant(
                        "More than one .sln found in {0}",
                        directoryPath));

            return new VisualStudioSolution(slnPaths[0]);
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


        public int GlobalEndLineNumber
        {
            get; private set;
        }


        public int NestedProjectsStartLineNumber
        {
            get; private set;
        }


        public int NestedProjectsEndLineNumber
        {
            get; private set;
        }


        public int SolutionConfigurationsStartLineNumber
        {
            get; private set;
        }


        public int SolutionConfigurationsEndLineNumber
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
            get { return ProjectReferences.Where(p => p.TypeId == VisualStudioProjectReference.SolutionFolderTypeId); }
        }


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
        /// Solution configurations
        /// </summary>
        ///
        public ISet<string> SolutionConfigurations
        {
            get { return _solutionConfigurations; }
        }

        ISet<string> _solutionConfigurations;


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
        /// Get a project reference with a specified id
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// No project with specified <paramref name="id"/> in solution
        /// </exception>
        ///
        public VisualStudioProjectReference GetProjectReference(string id)
        {
            if (id == null) throw new ArgumentNullException("id");

            var project = ProjectReferences.SingleOrDefault(p => p.Id == id);
            if (project == null)
                throw new ArgumentException(
                    StringExtensions.FormatInvariant("No project with Id {0} in solution", id),
                    "id");

            return project;
        }


        /// <summary>
        /// Add a project reference to the solution
        /// </summary>
        ///
        public void AddProjectReference(string typeId, string name, string location, string id)
        {
            if (typeId == null) throw new ArgumentNullException("typeId");
            if (name == null) throw new ArgumentNullException("name");
            if (location == null) throw new ArgumentNullException("location");
            if (id == null) throw new ArgumentNullException("id");
            
            _lines.Insert(
                GlobalStartLineNumber,
                VisualStudioProjectReference.FormatStart(typeId, name, location, id),
                VisualStudioProjectReference.FormatEnd());

            Load();
        }


        /// <summary>
        /// Delete a project reference
        /// </summary>
        ///
        public void DeleteProjectReference(VisualStudioProjectReference projectReference)
        {
            if (projectReference == null) throw new ArgumentNullException("projectReference");

            _lines.RemoveAt(projectReference.LineNumber, projectReference.LineCount);

            Load();
        }


        /// <summary>
        /// Delete a project reference and anything else relating to it
        /// </summary>
        ///
        public void DeleteProjectReferenceAndRelated(VisualStudioProjectReference projectReference)
        {
            if (projectReference == null) throw new ArgumentNullException("projectReference");

            var projectId = projectReference.Id;

            //
            // Delete related NestedProjects entries
            //
            for (;;)
            {
                var nesting =
                    NestedProjects.FirstOrDefault(n => n.ParentProjectId == projectId || n.ChildProjectId == projectId);
                if (nesting == null) break;
                    
                DeleteNestedProject(nesting);
            }

            //
            // Delete the project reference itself
            //
            DeleteProjectReference(GetProjectReference(projectId));
        }


        /// <summary>
        /// Add a nested projects section
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// The solution already contains a nested projects section
        /// </exception>
        ///
        public void AddNestedProjectsSection()
        {
            if (NestedProjectsStartLineNumber >= 0)
                throw new InvalidOperationException("Solution already contains a nested projects section");

            _lines.Insert(
                GlobalEndLineNumber,
                "\tGlobalSection(NestedProjects) = preSolution",
                "\tEndGlobalSection");

            Load();
        }


        /// <summary>
        /// Add a nested project entry
        /// </summary>
        ///
        public void AddNestedProject(string childProjectId, string parentProjectId)
        {
            if (parentProjectId == null) throw new ArgumentNullException("parentProjectId");
            if (childProjectId == null) throw new ArgumentNullException("childProjectId");

            if (NestedProjectsStartLineNumber < 0)
                AddNestedProjectsSection();

            _lines.Insert(
                NestedProjectsEndLineNumber,
                "\t\t" + VisualStudioNestedProject.Format(childProjectId, parentProjectId));

            Load();
        }


        /// <summary>
        /// Delete a nested project entry
        /// </summary>
        ///
        public void DeleteNestedProject(VisualStudioNestedProject nestedProject)
        {
            if (nestedProject == null) throw new ArgumentNullException("nestedProject");

            _lines.RemoveAt(nestedProject.LineNumber);

            Load();
        }


        /// <summary>
        /// Add a solution folder to the solution
        /// </summary>
        ///
        /// <returns>
        /// The project reference Id of the newly-added solution folder
        /// </returns>
        ///
        public string AddSolutionFolder(string name)
        {
            var id = Guid.NewGuid().ToString("B").ToUpperInvariant();

            AddProjectReference(
                VisualStudioProjectReference.SolutionFolderTypeId,
                name,
                name,
                id);

            return id;
        }


        /// <summary>
        /// Completely delete a solution folder and all its contents
        /// </summary>
        ///
        /// <remarks>
        /// To delete just the solution folder project reference, use <see cref="DeleteProjectReference(string)"/>
        /// </remarks>
        ///
        public void DeleteSolutionFolder(VisualStudioProjectReference solutionFolder)
        {
            if (solutionFolder == null)
                throw new ArgumentNullException("solutionFolder");

            if (solutionFolder.TypeId != VisualStudioProjectReference.SolutionFolderTypeId)
                throw new ArgumentException("Not a solution folder", "solutionFolder");

            var solutionFolderId = solutionFolder.Id;

            //
            // Delete child folders and projects
            //
            for(;;)
            {
                var childProject =
                    NestedProjects
                        .Where(np => np.ParentProjectId == solutionFolderId)
                        .Select(np => GetProjectReference(np.ChildProjectId))
                        .FirstOrDefault();
                if (childProject == null) break;
                
                if (childProject.TypeId == VisualStudioProjectReference.SolutionFolderTypeId)
                {
                    DeleteSolutionFolder(childProject);
                }
                else
                {
                    DeleteProjectReferenceAndRelated(childProject);
                }
            }

            //
            // Delete the folder itself
            //
            DeleteProjectReferenceAndRelated(GetProjectReference(solutionFolderId));
        }


        /// <summary>
        /// Interpret information in the solution file
        /// </summary>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Maintainability",
            "CA1502:AvoidExcessiveComplexity",
            Justification = "It's a big state machine, not sure how to decompose in a simple clear way")]
        void Load()
        {
            GlobalStartLineNumber = -1;
            GlobalEndLineNumber = -1;
            NestedProjectsStartLineNumber = -1;
            NestedProjectsEndLineNumber = -1;
            SolutionConfigurationsStartLineNumber = -1;
            SolutionConfigurationsEndLineNumber = -1;
            _projectReferences = new List<VisualStudioProjectReference>();
            _nestedProjects = new List<VisualStudioNestedProject>();
            _solutionConfigurations = new HashSet<string>();
            _buildConfigurationMappings = new List<VisualStudioBuildConfigurationMapping>();

            int lineNumber = -1;
            int projectReferenceStartLineNumber = -1;
            int projectConfigurationsStartLineNumber = -1;
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
                if (projectReferenceStartLineNumber >= 0)
                {
                    if (line.Trim() == "EndProject")
                    {
                        _projectReferences.Add(
                            new VisualStudioProjectReference(
                                id, typeId, name, location, projectReferenceStartLineNumber, lineNumber - projectReferenceStartLineNumber));
                        projectReferenceStartLineNumber = -1;
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
                if (NestedProjectsStartLineNumber >= 0 && NestedProjectsEndLineNumber < 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        NestedProjectsEndLineNumber = lineNumber;
                        continue;
                    }

                    match = Regex.Match(line, "(\\S+) = (\\S+)");
                    if (!match.Success)
                        throw new FileParseException(
                            "Expected '{guid} = {guid}'",
                            lineNumber + 1,
                            line);

                    _nestedProjects.Add(
                        new VisualStudioNestedProject(
                            match.Groups[1].Value,
                            match.Groups[2].Value,
                            lineNumber));

                    continue;
                }

                //
                // In solution configurations block
                //
                if (SolutionConfigurationsStartLineNumber >= 0 && SolutionConfigurationsEndLineNumber < 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        SolutionConfigurationsEndLineNumber = lineNumber;
                        continue;
                    }

                    match = Regex.Match(line, "^([^=]+) = (.+)$");
                    if (!match.Success)
                        throw new FileParseException(
                            "Expected '{configuration} = {configuration}'",
                            lineNumber + 1,
                            line);

                    _solutionConfigurations.Add(match.Groups[1].Value);

                    continue;
                }

                //
                // In project configurations block
                //
                if (projectConfigurationsStartLineNumber >= 0)
                {
                    if (line.Trim() == "EndGlobalSection")
                    {
                        projectConfigurationsStartLineNumber = -1;
                        continue;
                    }

                    match = Regex.Match(line, "^\\s*([^.]+)\\.([^.]+)\\.(.+) = (.+)$");
                    if (!match.Success)
                        throw new FileParseException(
                            "Expected '{guid}.{configuration}.{property} = {configuration}'",
                            lineNumber + 1,
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
                    if (projectReferenceStartLineNumber >= 0)
                        throw new FileParseException(
                            "Expected 'EndProject'",
                            lineNumber + 1,
                            line);

                    projectReferenceStartLineNumber = lineNumber;
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
                    NestedProjectsStartLineNumber = lineNumber;
                    continue;
                }


                //
                // Starting solution configurations block
                //
                if (line.Trim() == "GlobalSection(SolutionConfigurationPlatforms) = preSolution")
                {
                    SolutionConfigurationsStartLineNumber = lineNumber;
                    continue;
                }


                //
                // Starting project configurations block
                //
                if (line.Trim() == "GlobalSection(ProjectConfigurationPlatforms) = postSolution")
                {
                    projectConfigurationsStartLineNumber = lineNumber;
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
                // End of "Global" section
                //
                if (line.Trim() == "EndGlobal")
                {
                    GlobalEndLineNumber = lineNumber;
                    continue;
                }

                //
                // Nothing special
                //
            }

            if (GlobalStartLineNumber < 0)
                throw new FileParseException("No 'Global' section in file", 1, "");
            if (GlobalEndLineNumber < 0)
                throw new FileParseException("No 'EndGlobal' in file", 1, "");
            if (NestedProjectsStartLineNumber >= 0 && NestedProjectsEndLineNumber < 0)
                throw new FileParseException("No nested projects 'EndGlobalSection' in file", 1, "");
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
