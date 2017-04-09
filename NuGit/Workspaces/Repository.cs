using System;
using System.IO;
using IOPath = System.IO.Path;
using System.Collections.Generic;
using System.Linq;
using MacroGuards;
using MacroGit;
using NuGit.Infrastructure;

namespace NuGit.Workspaces
{

    /// <summary>
    /// A NuGit repository
    /// </summary>
    ///
    public class Repository
        : GitRepository
    {

        internal Repository(Workspace workspace, GitRepositoryName name)
            : base(
                IOPath.Combine(
                    Guard.NotNull(workspace, nameof(workspace)).RootPath,
                    Guard.NotNull(name, nameof(name))))
        {
            Workspace = workspace;
            Name = name;
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
            string path = IOPath.Combine(dotNuGitDir, ".nugit");

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
        public IList<LockDependency> ReadNuGitLock()
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = IOPath.Combine(dotNuGitDir, ".nugit.lock");
            if (!File.Exists(path)) return new LockDependency[0];

            var result = new List<LockDependency>();
            int lineNumber = 0;
            foreach (var rawline in File.ReadLines(path))
            {
                lineNumber++;
                var line = rawline.Trim();

                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;

                var a = line.Split(' ');
                if (a.Length != 3)
                    throw new FileParseException(
                        "Expected URL, commit name, and commit ID",
                        lineNumber,
                        rawline);
                
                GitUrl url;
                try
                {
                    url = new GitUrl(a[0]);
                }
                catch (FormatException fe)
                {
                    throw new FileParseException(
                        "Expected valid Git URL",
                        lineNumber,
                        rawline,
                        fe);
                }

                GitCommitName commitName;
                try
                {
                    commitName = new GitCommitName(a[1]);
                }
                catch (FormatException fe)
                {
                    throw new FileParseException(
                        "Expected valid Git commit name",
                        lineNumber,
                        rawline,
                        fe);
                }
                
                GitCommitName commitId;
                try
                {
                    commitId = new GitCommitName(a[2]);
                }
                catch (FormatException fe)
                {
                    throw new FileParseException(
                        "Expected valid Git commit identifier",
                        lineNumber,
                        rawline,
                        fe);
                }

                result.Add(new LockDependency(url, commitName, commitId));
            }

            return result;
        }


        /// <summary>
        /// Write dependency information to .nugit.lock
        /// </summary>
        ///
        public void WriteNuGitLock(ICollection<LockDependency> dependencies)
        {
            Guard.NotNull(dependencies, nameof(dependencies));

            string dotNuGitDir = GetDotNuGitDir();
            string path = IOPath.Combine(dotNuGitDir, ".nugit.lock");

            if (dependencies.Count == 0)
            {
                File.Delete(path);
                return;
            }

            File.WriteAllLines(
                path,
                dependencies.Select(d =>
                    string.Concat(
                        d.Url,
                        " ",
                        d.CommitName,
                        " ",
                        d.CommitId)));
        }


        /// <summary>
        /// Delete .nugit.lock
        /// </summary>
        ///
        public void DeleteNuGitLock()
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = IOPath.Combine(dotNuGitDir, ".nugit.lock");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }


        /// <summary>
        /// Determine full path to directory that does (or should) contain the .nugit file
        /// </summary>
        ///
        string GetDotNuGitDir()
        {
            string dotNuGitDir = IOPath.Combine(Path, ".nugit");
            if (Directory.Exists(dotNuGitDir)) return dotNuGitDir;
            return Path;
        }

    }

}
