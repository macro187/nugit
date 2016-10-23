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


        public override string ToString()
        {
            return StringExtensions.FormatInvariant(
                "{0}:{1}: Project(\"{2}\") = \"{3}\", \"{4}\", \"{5}\"",
                LineNumber, LineCount, TypeId, Name, Location, Id);
        }

    }

}
