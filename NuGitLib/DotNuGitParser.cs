﻿using System;
using System.Collections.Generic;
using System.IO;
using MacroExceptions;


namespace
NuGitLib
{


/// <summary>
/// <c>.nugit</c> parser
/// </summary>
///
public static class
DotNuGitParser
{


const string
PROGRAM_PREFIX = "program: ";


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance",
    "CA1820:TestForEmptyStringsUsingStringLength",
    Justification = "Testing against \"\" expresses intent more clearly")]
public static DotNuGit
Parse(IEnumerable<string> lines)
{
    if (lines == null) throw new ArgumentNullException("lines");

    var dependencies = new List<NuGitDependency>();
    var programs = new List<string>();

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
        // program: <program>
        //
        if (line.StartsWith(PROGRAM_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var program = line.Substring(PROGRAM_PREFIX.Length).Trim();
            if (program == "")
                throw new TextFileParseException(
                    "Expected <program>",
                    lineNumber + 1,
                    line);
            program = program.Replace('/', '\\');
            program = program.Replace('\\', Path.DirectorySeparatorChar);
            programs.Add(program);
            continue;
        }

        //
        // <dependencyurl>
        //
        NuGitDependencyUrl url;
        try
        {
            url = new NuGitDependencyUrl(line.Trim());
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

    return new DotNuGit(dependencies, programs);
}


}
}
