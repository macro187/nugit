using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MacroGuards;
using MacroGit;
using NuGit.Infrastructure;

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
        }


        /// <summary>
        /// Workspace the repository is in
        /// </summary>
        ///
        public Workspace Workspace
        {
            get;
        }


        /// <summary>
        /// Name of the repository subdirectory
        /// </summary>
        ///
        public GitRepositoryName Name
        {
            get;
        }


        /// <summary>
        /// Read .nugit information
        /// </summary>
        ///
        public DotNuGit ReadDotNuGit()
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


        /// <summary>
        /// Read dependency information from .nugit.lock
        /// </summary>
        ///
        public IList<Dependency> ReadNuGitLock()
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


        /// <summary>
        /// Write dependency information to .nugit.lock
        /// </summary>
        ///
        public void WriteNuGitLock(ICollection<Dependency> dependencies)
        {
            Guard.NotNull(dependencies, nameof(dependencies));

            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit.lock");

            if (dependencies.Count == 0)
            {
                File.Delete(path);
                return;
            }

            File.WriteAllLines(
                path,
                dependencies.Select(d => new DependencyUrl(d).ToString()));
        }


        /// <summary>
        /// Delete .nugit.lock
        /// </summary>
        ///
        public void DeleteNuGitLock()
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit.lock");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }


        /// <summary>
        /// Determine full path to directory that does (or should) contain the .nugit file
        /// </summary>
        ///
        string GetDotNuGitDir()
        {
            string dotNuGitDir = Path.Combine(RootPath, ".nugit");
            if (Directory.Exists(dotNuGitDir)) return dotNuGitDir;
            return RootPath;
        }

    }

}
