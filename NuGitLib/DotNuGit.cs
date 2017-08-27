using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace
NuGitLib
{


/// <summary>
/// Information in a <c>.nugit</c> repository configuration file
/// </summary>
///
public class
DotNuGit
{


public
DotNuGit()
    : this(new NuGitDependency[0], new string[0])
{
}


public
DotNuGit(IEnumerable<NuGitDependency> dependencies, IEnumerable<string> programs)
{
    if (dependencies == null) throw new ArgumentNullException("dependencies");
    if (programs == null) throw new ArgumentNullException("programs");
    Dependencies = new ReadOnlyCollection<NuGitDependency>(dependencies.ToList());
    Programs = new ReadOnlyCollection<string>(programs.ToList());
}


/// <summary>
/// Other repositories required by this one
/// </summary>
///
public IList<NuGitDependency>
Dependencies
{
    get;
    private set;
}


/// <summary>
/// Relative paths to program executables in this repository
/// </summary>
///
public ICollection<string>
Programs
{
    get;
    private set;
}


}
}
