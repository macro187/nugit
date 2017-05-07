using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace nugit
{

    /// <summary>
    /// Information in a <c>.nugit</c> repository configuration file
    /// </summary>
    ///
    public class DotNugit
    {

        public DotNugit()
            : this(new Dependency[0], new string[0])
        {
        }


        public DotNugit(IEnumerable<Dependency> dependencies, IEnumerable<string> programs)
        {
            if (dependencies == null) throw new ArgumentNullException("dependencies");
            if (programs == null) throw new ArgumentNullException("programs");
            Dependencies = new ReadOnlyCollection<Dependency>(dependencies.ToList());
            Programs = new ReadOnlyCollection<string>(programs.ToList());
        }


        /// <summary>
        /// Other repositories required by this one
        /// </summary>
        ///
        public IList<Dependency> Dependencies
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
