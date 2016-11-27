using System;
using NuGit.Infrastructure;

namespace NuGit.VisualStudio
{

    /// <summary>
    /// A Visual Studio solution-to-project build configuration mapping
    /// </summary>
    ///
    /// <remarks>
    /// Mappings are in <c>.sln</c> files in blocks in the following format:
    /// <code>
    /// GlobalSection(ProjectConfigurationPlatforms) = postSolution
    ///     &lt;ProjectId&gt;.&lt;ProjectBuildConfiguration&gt;.&lt;Property&gt; = &lt;SolutionBuildConfiguration&gt;
    ///     ...
    /// EndGlobalSection
    /// </code>
    /// </remarks>
    /// 
    public class VisualStudioBuildConfigurationMapping
    {

        public VisualStudioBuildConfigurationMapping(
            string projectId,
            string projectBuildConfiguration,
            string property,
            string solutionBuildConfiguration,
            int lineNumber)
        {
            if (projectId == null) throw new ArgumentNullException("projectId");
            if (projectBuildConfiguration == null) throw new ArgumentNullException("projectBuildConfiguration");
            if (property == null) throw new ArgumentNullException("property");
            if (solutionBuildConfiguration == null) throw new ArgumentNullException("solutionBuildConfiguration");
            ProjectId = projectId;
            ProjectBuildConfiguration = projectBuildConfiguration;
            Property = property;
            SolutionBuildConfiguration = solutionBuildConfiguration;
            LineNumber = lineNumber;
        }
        

        public string ProjectId { get; private set; }


        public string ProjectBuildConfiguration { get; private set; }


        public string Property { get; private set; }


        public string SolutionBuildConfiguration { get; private set; }


        public int LineNumber { get; private set; }


        public override string ToString()
        {
            return StringExtensions.FormatInvariant(
                "Line {0}: {1}.{2}.{3} = {4}",
                LineNumber + 1,
                ProjectId,
                SolutionBuildConfiguration,
                Property,
                ProjectBuildConfiguration);
        }

    }

}
