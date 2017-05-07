using System;
using MacroSystem;

namespace nugit.VisualStudio
{

    /// <summary>
    /// A Visual Studio solution nested project entry
    /// </summary>
    ///
    /// <remarks>
    /// Nested projects are in <c>.sln</c> files in blocks in the following format:
    /// <code>
    /// GlobalSection(NestedProjects) = preSolution
    ///     &lt;ChildProjectId&gt; = &lt;ParentProjectId&gt; 
    ///     ...
    /// EndGlobalSection
    /// </code>
    /// </remarks>
    /// 
    public class VisualStudioNestedProject
    {

        public VisualStudioNestedProject(string childProjectId, string parentProjectId, int lineNumber)
        {
            if (parentProjectId == null) throw new ArgumentNullException("parentProjectId");
            if (childProjectId == null) throw new ArgumentNullException("childProjectId");
            ChildProjectId = childProjectId;
            ParentProjectId = parentProjectId;
            LineNumber = lineNumber;
        }
        

        public string ParentProjectId { get; private set; }


        public string ChildProjectId { get; private set; }


        public int LineNumber { get; private set; }


        public static string Format(string childProjectId, string parentProjectId)
        {
            if (parentProjectId == null) throw new ArgumentNullException("parentProjectId");
            if (childProjectId == null) throw new ArgumentNullException("childProjectId");
            return childProjectId + " = " + parentProjectId;
        }


        public override string ToString()
        {
            return StringExtensions.FormatInvariant(
                "Line {0}: {1}",
                LineNumber + 1,
                Format(ChildProjectId, ParentProjectId));
        }

    }

}
