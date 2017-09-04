using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace
nugit
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
    : this(new Dependency[0])
{
}


public
DotNuGit(IEnumerable<Dependency> dependencies)
{
    if (dependencies == null) throw new ArgumentNullException("dependencies");
    Dependencies = new ReadOnlyCollection<Dependency>(dependencies.ToList());
}


/// <summary>
/// Other repositories required by this one
/// </summary>
///
public IList<Dependency>
Dependencies
{
    get;
    private set;
}


}
}
