using System;
using NuGit.Infrastructure;

namespace NuGit.VisualStudio
{

    /// <summary>
    /// A Visual Studio solution reference to a project
    /// </summary>
    ///
    /// <remarks>
    /// References are in <c>.sln</c> files in the following format:
    /// <code>
    /// Project("&lt;TypeId&gt;") = "&lt;Name&gt;", "&lt;Location&gt;", "&lt;Id&gt;"
    /// ...
    /// EndProject
    /// </code>
    /// </remarks>
    /// 
    public class VisualStudioProjectReference
    {

        public static readonly string SolutionFolderTypeId = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";


        public VisualStudioProjectReference(
            string id,
            string typeId,
            string name,
            string location,
            int lineNumber,
            int lineCount)
        {
            Id = id;
            TypeId = typeId;
            Name = name;
            Location = location;
            LineNumber = lineNumber;
            LineCount = lineCount;
        }
        

        public string Id { get; private set; }


        public string TypeId { get; private set; }


        public string Name { get; private set; }


        public string Location { get; private set; }


        public int LineNumber { get; private set; }


        public int LineCount { get; private set; }


        public static string FormatStart(string typeId, string name, string location, string id)
        {
            if (typeId == null) throw new ArgumentNullException("typeId");
            if (name == null) throw new ArgumentNullException("name");
            if (location == null) throw new ArgumentNullException("location");
            if (id == null) throw new ArgumentNullException("id");

            return StringExtensions.FormatInvariant(
                "Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"",
                typeId, name, location, id);
        }


        public static string FormatEnd()
        {
            return "EndProject";
        }


        public override string ToString()
        {
            return StringExtensions.FormatInvariant(
                "Line {0}:{1}: {2}",
                LineNumber + 1,
                LineCount,
                FormatStart(TypeId, Name, Location, Id));
        }

    }

}
