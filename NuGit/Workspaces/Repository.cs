using System;
using System.IO;
using System.Collections.Generic;
using MacroGit;
using NuGit.Infrastructure;
using System.Linq;

namespace NuGit.Workspaces
{

    /// <summary>
    /// A repository subdirectory
    /// </summary>
    ///
    public class Repository
    {

        /// <summary>
        /// Determine whether a directory is a Git repository
        /// </summary>
        ///
        public static bool IsRepository(string directoryPath)
        {
            return Directory.Exists(Path.Combine(directoryPath, ".git"));
        }


        internal Repository(Workspace workspace, GitRepositoryName name)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            if (name == null) throw new ArgumentNullException("name");
            RootPath = Path.Combine(workspace.RootPath, name);
            Name = name;
            Workspace = workspace;
        }


        /// <summary>
        /// Full path to the repository's root directory
        /// </summary>
        ///
        public string RootPath
        {
            get;
            private set;
        }


        /// <summary>
        /// Workspace the repository is in
        /// </summary>
        ///
        public Workspace Workspace
        {
            get;
            private set;
        }


        /// <summary>
        /// Name of the repository subdirectory
        /// </summary>
        ///
        public GitRepositoryName Name
        {
            get;
            private set;
        }


        /// <summary>
        /// Get the repository's .nugit information
        /// </summary>
        ///
        /// <remarks>
        /// Result can be affected by <see cref="Checkout()"/>.
        /// </remarks>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Emphasise that information is reread on each call")]
        public DotNuGit GetDotNuGit()
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit");

            if (!File.Exists(path))
                return new DotNuGit();

            try
            {
                return DotNuGitParser.Parse(File.ReadLines(path));
            }
            catch (FileParseException e)
            {
                e.Path = path;
                throw;
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Reads from a file whose contents can change, so better as a method to imply action")]
        public IList<Dependency> GetDotNuGitLock()
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit.lock");
            if (!File.Exists(path)) return new Dependency[0];

            var result = new List<Dependency>();
            int lineNumber = 0;
            foreach (var rawline in File.ReadLines(path))
            {
                lineNumber++;
                var line = rawline.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                DependencyUrl url;
                try
                {
                    url = new DependencyUrl(line);
                }
                catch (FormatException fe)
                {
                    throw new FileParseException(
                        "Invalid dependency URL encountered",
                        lineNumber,
                        rawline,
                        fe);
                }
                result.Add(url.Dependency);
            }

            return result;
        }


        public void SetDotNuGitLock(IEnumerable<Dependency> dependencies)
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit.lock");
            File.WriteAllLines(
                path,
                dependencies.Select(d => new DependencyUrl(d).ToString()));
        }


        public void ClearDotNuGitLock()
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit.lock");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }


        string GetDotNuGitDir()
        {
            string dotNuGitDir = Path.Combine(RootPath, ".nugit");
            if (Directory.Exists(dotNuGitDir)) return dotNuGitDir;
            return RootPath;
        }

    }

}
