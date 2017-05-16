using System;
using System.IO;
using IOPath = System.IO.Path;
using System.Collections.Generic;
using System.Linq;
using MacroGuards;
using MacroExceptions;
using MacroGit;

namespace nugit
{

    /// <summary>
    /// A nugit repository
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
        public DotNugit ReadDotNugit()
        {
            string dotNugitDir = GetDotNugitDir();
            string path = IOPath.Combine(dotNugitDir, ".nugit");

            if (!File.Exists(path))
                return new DotNugit();

            try
            {
                return DotNugitParser.Parse(File.ReadLines(path));
            }
            catch (TextFileParseException e)
            {
                e.Path = path;
                throw;
            }
        }


        /// <summary>
        /// Read dependency information from .nugit.lock
        /// </summary>
        ///
        public IList<LockDependency> ReadNugitLock()
        {
            string dotNugitDir = GetDotNugitDir();
            string path = IOPath.Combine(dotNugitDir, ".nugit.lock");
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
                    throw new TextFileParseException(
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
                    throw new TextFileParseException(
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
                    throw new TextFileParseException(
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
                    throw new TextFileParseException(
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
        public void WriteNugitLock(ICollection<LockDependency> dependencies)
        {
            Guard.NotNull(dependencies, nameof(dependencies));

            string dotNugitDir = GetDotNugitDir();
            string path = IOPath.Combine(dotNugitDir, ".nugit.lock");

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
        public void DeleteNugitLock()
        {
            string dotNugitDir = GetDotNugitDir();
            string path = IOPath.Combine(dotNugitDir, ".nugit.lock");
            if (!File.Exists(path)) return;
            File.Delete(path);
        }


        /// <summary>
        /// Determine full path to directory that does (or should) contain the .nugit file
        /// </summary>
        ///
        string GetDotNugitDir()
        {
            string dotNugitDir = IOPath.Combine(Path, ".nugit");
            if (Directory.Exists(dotNugitDir)) return dotNugitDir;
            return Path;
        }

    }

}
