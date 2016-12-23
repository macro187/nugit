using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NuGit.Git;

namespace NuGit.Workspaces
{

    /// <summary>
    /// Information in a <c>.nugit</c> repository configuration file
    /// </summary>
    ///
    public class DotNuGit
    {

        public DotNuGit()
            : this(new GitDependencyInfo[0], new string[0])
        {
        }


        public DotNuGit(IEnumerable<GitDependencyInfo> dependencies, IEnumerable<string> programs)
        {
            if (dependencies == null) throw new ArgumentNullException("dependencies");
            if (programs == null) throw new ArgumentNullException("programs");
            Dependencies = new ReadOnlyCollection<GitDependencyInfo>(dependencies.ToList());
            Programs = new ReadOnlyCollection<string>(programs.ToList());
        }


        /// <summary>
        /// Other repositories required by this one
        /// </summary>
        ///
        public IList<GitDependencyInfo> Dependencies
        {
            get;
            private set;
        }


        /// <summary>
        /// Relative paths to program executables in this repository
        /// </summary>
        ///
        public ICollection<string> Programs
        {
            get;
            private set;
        }

    }

}
