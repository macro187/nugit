using System;
using System.Collections.Generic;
using NuGit.Git;
using NuGit.Infrastructure;

namespace NuGit.Workspaces
{

    /// <summary>
    /// <c>.nugit</c> parser
    /// </summary>
    ///
    public static class DotNuGitParser
    {

        public static DotNuGit Parse(IEnumerable<string> lines)
        {
            if (lines == null) throw new ArgumentNullException("lines");

            var dependencies = new List<GitDependencyInfo>();

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
                // <giturl>
                //
                GitUrl url;
                try
                {
                    url = new GitUrl(line.Trim());
                }
                catch (FormatException fe)
                {
                    throw new FileParseException(
                        "Invalid Git repository URL: " + fe.Message,
                        lineNumber,
                        line,
                        fe);
                }

                dependencies.Add(new GitDependencyInfo(url, url.Commit ?? new GitCommitName("master")));
            }

            return new DotNuGit(dependencies);
        }

    }

}
