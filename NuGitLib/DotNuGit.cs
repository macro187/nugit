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
    : this(new NuGitDependency[0])
{
}


public
DotNuGit(IEnumerable<NuGitDependency> dependencies)
{
    if (dependencies == null) throw new ArgumentNullException("dependencies");
    Dependencies = new ReadOnlyCollection<NuGitDependency>(dependencies.ToList());
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


}
}
