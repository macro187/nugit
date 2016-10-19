using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NuGit
{

    /// <summary>
    /// Information in a <c>.nugit</c> repository configuration file
    /// </summary>
    ///
    public class DotNuGit
    {

        public DotNuGit()
            : this(new DependencyInfo[0])
        {
        }


        public DotNuGit(IEnumerable<DependencyInfo> dependencies)
        {
            if (dependencies == null) throw new ArgumentNullException("dependencies");
            Dependencies = new ReadOnlyCollection<DependencyInfo>(dependencies.ToList());
        }


        /// <summary>
        /// Other repositories required by this one
        /// </summary>
        ///
        public IList<DependencyInfo> Dependencies
        {
            get;
            private set;
        }

    }

}
