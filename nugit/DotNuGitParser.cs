using System;
using System.Collections.Generic;
using System.IO;
using MacroExceptions;


namespace
nugit
{


/// <summary>
/// <c>.nugit</c> parser
/// </summary>
///
public static class
DotNuGitParser
{


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance",
    "CA1820:TestForEmptyStringsUsingStringLength",
    Justification = "Testing against \"\" expresses intent more clearly")]
public static DotNuGit
Parse(IEnumerable<string> lines)
{
    if (lines == null) throw new ArgumentNullException("lines");

    var dependencies = new List<Dependency>();

    int lineNumber = 0;
    foreach (string line in lines)
    {
        lineNumber++;

        //
        // Empty / whitespace-only
        //
        if (string.IsNullOrWhiteSpace(line)) continue;

        //
        // # <comment>
        //
        if (line.StartsWith("#", StringComparison.Ordinal)) continue;

        //
        // <dependencyurl>
        //
        DependencyUrl url;
        try
        {
            url = new DependencyUrl(line.Trim());
        }
        catch (FormatException fe)
        {
            throw new TextFileParseException(
                "Invalid dependency URL: " + fe.Message,
                lineNumber + 1,
                line,
                fe);
        }

        dependencies.Add(url.Dependency);
    }

    return new DotNuGit(dependencies);
}


}
}
