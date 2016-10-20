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
            : this(new GitDependencyInfo[0])
        {
        }


        public DotNuGit(IEnumerable<GitDependencyInfo> dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException("dependencies");
            Dependencies = new ReadOnlyCollection<GitDependencyInfo>(dependencies.ToList());
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

    }

}
