using System;
using MacroSystem;

namespace NuGit.VisualStudio
{

    /// <summary>
    /// A Visual Studio solution-to-project configuration mapping entry
    /// </summary>
    ///
    /// <remarks>
    /// Entries are in <c>.sln</c> files in blocks in the following format:
    /// <code>
    /// GlobalSection(ProjectConfigurationPlatforms) = postSolution
    ///     &lt;ProjectId&gt;.&lt;ProjectConfiguration&gt;.&lt;Property&gt; = &lt;SolutionConfiguration&gt;
    ///     ...
    /// EndGlobalSection
    /// </code>
    /// </remarks>
    /// 
    public class VisualStudioProjectConfiguration
    {

        public VisualStudioProjectConfiguration(
            string projectId,
            string projectConfiguration,
            string property,
            string solutionConfiguration,
            int lineNumber)
        {
            if (projectId == null) throw new ArgumentNullException("projectId");
            if (projectConfiguration == null) throw new ArgumentNullException("projectConfiguration");
            if (property == null) throw new ArgumentNullException("property");
            if (solutionConfiguration == null) throw new ArgumentNullException("solutionConfiguration");
            ProjectId = projectId;
            ProjectConfiguration = projectConfiguration;
            Property = property;
            SolutionConfiguration = solutionConfiguration;
            LineNumber = lineNumber;
        }
        

        public string ProjectId { get; private set; }


        public string ProjectConfiguration { get; private set; }


        public string Property { get; private set; }


        public string SolutionConfiguration { get; private set; }


        public int LineNumber { get; private set; }


        public static string Format(
            string projectId,
            string projectConfiguration,
            string property,
            string solutionConfiguration)
        {
            if (projectId == null) throw new ArgumentNullException("projectId");
            if (projectConfiguration == null) throw new ArgumentNullException("projectConfiguration");
            if (property == null) throw new ArgumentNullException("property");
            if (solutionConfiguration == null) throw new ArgumentNullException("solutionConfiguration");

            return StringExtensions.FormatInvariant(
                "{0}.{1}.{2} = {3}",
                projectId,
                projectConfiguration,
                property,
                solutionConfiguration);
        }


        public override string ToString()
        {
            return StringExtensions.FormatInvariant(
                "Line {0}: {1}",
                LineNumber + 1,
                Format(
                    ProjectId,
                    SolutionConfiguration,
                    Property,
                    ProjectConfiguration));
        }

    }

}
