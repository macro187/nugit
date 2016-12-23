﻿using System;
using System.IO;
using NuGit.Infrastructure;
using NuGit.Git;
using System.Collections.Generic;
using System.Linq;

namespace NuGit.Workspaces
{

    /// <summary>
    /// A root directory that contains repository subdirectories
    /// </summary>
    ///
    public class Workspace
    {

        /// <summary>
        /// Name of special workspace subdirectory containing wrapper scripts for running programs in the repositories
        /// in the workspace
        /// </summary>
        ///
        const string ProgramWrapperDirectoryName = ".bin";


        /// <summary>
        /// Initialise a new workspace
        /// </summary>
        ///
        /// <param name="rootPath">
        /// Path to the workspace's root directory
        /// </param>
        ///
        public Workspace(string rootPath)
        {
            if (rootPath == null) throw new ArgumentNullException("rootPath");
            if (!Directory.Exists(rootPath)) throw new ArgumentException("Not a directory", "rootPath");

            RootPath = Path.GetFullPath(rootPath);
        }


        /// <summary>
        /// Full path to the workspace's root directory
        /// </summary>
        ///
        public string RootPath
        {
            get;
            private set;
        }


        /// <summary>
        /// Get a repository in the workspace
        /// </summary>
        ///
        /// <param name="name">
        /// Name of the repository
        /// </param>
        ///
        /// <returns>
        /// The repository named <paramref name="name"/>
        /// </returns>
        ///
        /// <exception cref="ArgumentException">
        /// No repository named <paramref name="name"/> exists in the workspace
        /// </exception>
        ///
        public Repository GetRepository(GitRepositoryName name)
        {
            var repository = FindRepository(name);
            if (repository == null)
                throw new ArgumentException(
                    StringExtensions.FormatInvariant(
                        "No repository named '{0}' in workspace",
                        name),
                    "name");
            return repository;
        }


        /// <summary>
        /// Look for a repository in the workspace
        /// </summary>
        ///
        /// <param name="name">
        /// Name of the sought-after repository
        /// </param>
        ///
        /// <returns>
        /// The repository in the workspace named <paramref name="name"/>
        /// - OR -
        /// <c>null</c> if no such repository exists
        /// </returns>
        ///
        public Repository FindRepository(GitRepositoryName name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Blank", "name");

            if (!Repository.IsRepository(Path.Combine(RootPath, name))) return null;
            return new Repository(this, name);
        }


        /// <summary>
        /// Locate all repositories in the workspace
        /// </summary>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Not static, this is re-read from disk each call")]
        public IEnumerable<Repository> GetRepositories()
        {
            return
                Directory.EnumerateDirectories(RootPath)
                    .Where(path => Repository.IsRepository(path))
                    .Select(path => new GitRepositoryName(Path.GetFileName(path)))
                    .Select(name => new Repository(this, name));
        }


        /// <summary>
        /// Get full path to (and if necessary create) a special workspace subdirectory for wrapper scripts that run
        /// programs in the repositories in the workspace
        /// </summary>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "This method can have side-effects")]
        public string GetProgramWrapperDirectory()
        {
            var path = Path.Combine(RootPath, ProgramWrapperDirectoryName);
            
            if (!Directory.Exists(path))
            {
                using (TraceExtensions.Step("Creating " + path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            
            return path;
        }

    }

}
