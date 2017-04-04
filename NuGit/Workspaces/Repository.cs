using System;
using System.IO;
using System.Collections.Generic;
using NuGit.Infrastructure;
using NuGit.Git;
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
        public IList<GitUrl> GetDotNuGitLock()
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit.lock");
            if (!File.Exists(path)) return new GitUrl[0];

            var result = new List<GitUrl>();
            int lineNumber = 0;
            foreach (var rawline in File.ReadLines(path))
            {
                lineNumber++;
                var line = rawline.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                GitUrl url;
                try
                {
                    url = new GitUrl(line);
                }
                catch (FormatException fe)
                {
                    throw new FileParseException(
                        "Invalid Git URL encountered",
                        lineNumber,
                        rawline,
                        fe);
                }
                result.Add(url);
            }

            return result;
        }


        public void SetDotNuGitLock(IEnumerable<GitUrl> urls)
        {
            string dotNuGitDir = GetDotNuGitDir();
            string path = Path.Combine(dotNuGitDir, ".nugit.lock");
            File.WriteAllLines(
                path,
                urls.Select(url => string.Concat(url.ToString(), "#", url.Commit)));
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
