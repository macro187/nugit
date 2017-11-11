using System;
using System.IO;
using MacroGit;
using System.Collections.Generic;
using System.Linq;
using MacroSystem;
using MacroDiagnostics;


namespace
nugit
{


/// <summary>
/// A root directory that contains repository subdirectories
/// </summary>
///
public class
NuGitWorkspace
{


/// <summary>
/// Initialise a new workspace
/// </summary>
///
/// <param name="rootPath">
/// Path to the workspace's root directory
/// </param>
///
public
NuGitWorkspace(string rootPath)
{
    if (rootPath == null) throw new ArgumentNullException("rootPath");
    if (!Directory.Exists(rootPath)) throw new ArgumentException("Not a directory", "rootPath");

    RootPath = Path.GetFullPath(rootPath);
}


/// <summary>
/// Full path to the workspace's root directory
/// </summary>
///
public string
RootPath
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
public NuGitRepository
GetRepository(GitRepositoryName name)
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
public NuGitRepository
FindRepository(GitRepositoryName name)
{
    if (name == null) throw new ArgumentNullException("name");
    if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Blank", "name");

    if (!GitRepository.IsRepository(Path.Combine(RootPath, name))) return null;
    return new NuGitRepository(this, name);
}


/// <summary>
/// Locate all repositories in the workspace
/// </summary>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1024:UsePropertiesWhereAppropriate",
    Justification = "Not static, this is re-read from disk each call")]
public IEnumerable<NuGitRepository>
GetRepositories()
{
    return
        Directory.EnumerateDirectories(RootPath)
            .Where(path => GitRepository.IsRepository(path))
            .Select(path => new GitRepositoryName(Path.GetFileName(path)))
            .Select(name => new NuGitRepository(this, name));
}


}
}
