using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MacroDiagnostics;
using MacroExceptions;
using MacroGit;
using MacroSln;


namespace
NuGitLib
{


/// <summary>
/// Dependency install algorithm
/// </summary>
///
public static class
NuGitInstaller
{


const string
NugitFolderPrefix = "nugit-";


/// <summary>
/// Import projects from required repositories into Visual Studio solution
/// </summary>
///
public static void
Install(NuGitRepository repository)
{
    if (repository == null) throw new ArgumentNullException("repository");

    var sln = VisualStudioSolution.Find(repository.Path);
    if (sln == null) throw new UserException("No .sln file in repo");

    DeleteNugitSolutionFolders(sln);

    foreach (var repoName in NuGitDependencyTraverser.GetAllDependencies(repository))
    {
        Install(repository, sln, repoName);
    }

    sln.Save();
}


static void
Install(NuGitRepository repository, VisualStudioSolution sln, GitRepositoryName dependencyName)
{
    var slnLocalPath = GetLocalPath(repository.Path, Path.GetDirectoryName(sln.Path));
    var slnLocalPathComponents = SplitPath(slnLocalPath);
    var slnToWorkspacePath = Path.Combine(Enumerable.Repeat("..", slnLocalPathComponents.Length + 1).ToArray());

    using (LogicalOperation.Start("Installing projects from " + dependencyName))
    {
        var dependencyRepository = repository.Workspace.GetRepository(dependencyName);

        var dependencySln = VisualStudioSolution.Find(dependencyRepository.Path);
        if (dependencySln == null)
        {
            Trace.TraceInformation("No .sln found");
            return;
        }
        Trace.TraceInformation("Found " + Path.GetFileName(dependencySln.Path));

        var dependencySlnLocalPath =
            GetLocalPath(dependencyRepository.Path, Path.GetDirectoryName(dependencySln.Path));

        var dependencyProjects = FindDependencyProjects(dependencySln);
        if (dependencyProjects.Count == 0)
        {
            Trace.TraceInformation("No projects found in solution");
            return;
        }

        // TODO Consider configurations in each individual dependency project, not just the solution
        var configurationsInCommon =
            sln.SolutionConfigurations.Intersect(dependencySln.SolutionConfigurations)
                .OrderBy(s => s)
                .ToList();

        var dependencyFolderId = sln.AddSolutionFolder(NugitFolderPrefix + dependencyName);

        foreach (var p in dependencyProjects)
        {
            Trace.TraceInformation("Installing " + Path.GetFileName(p.Location));

            //
            // Add reference to the dependency project
            //
            sln.AddProjectReference(
                p.TypeId,
                p.Name,
                Path.Combine(slnToWorkspacePath, dependencyName, dependencySlnLocalPath, p.Location),
                p.Id);

            //
            // Put it in the dependency's solution folder
            //
            sln.AddNestedProject(p.Id, dependencyFolderId);

            //
            // Add solution -> project configuration mappings
            //
            foreach (string configuration in configurationsInCommon)
            {
                sln.AddProjectConfiguration(p.Id, configuration, "ActiveCfg", configuration);
                sln.AddProjectConfiguration(p.Id, configuration, "Build.0", configuration);
            }
        }
    }
}


static List<VisualStudioProjectReference>
FindDependencyProjects(VisualStudioSolution sln)
{
    return sln.ProjectReferences
        .Where(p => !p.Name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
        .Where(p => !(p.Name.IndexOf(".Tests.", StringComparison.OrdinalIgnoreCase) > -1))
        .Where(p => !string.IsNullOrWhiteSpace(p.TypeId))
        .Where(p => !(p.TypeId == VisualStudioProjectTypeIds.SolutionFolder))
        .Where(p => !string.IsNullOrWhiteSpace(p.Location))
        .Where(p => !Path.IsPathRooted(p.Location))
        .Where(p => !p.Location.StartsWith("..", StringComparison.Ordinal))
        .Where(p => !p.GetProject().ProjectTypeGuids.Contains(VisualStudioProjectTypeIds.Test))
        .OrderBy(p => p.Name)
        .ToList();
}


static void
DeleteNugitSolutionFolders(VisualStudioSolution sln)
{
    using (LogicalOperation.Start("Deleting nugit-controlled solution folders"))
    {
        for (;;)
        {
            var folder =
                sln.SolutionFolders
                    .FirstOrDefault(f => f.Name.StartsWith(NugitFolderPrefix, StringComparison.Ordinal));
            if (folder == null) break;

            sln.DeleteSolutionFolder(folder);
        }
    }
}


static string[]
SplitPath(string path)
{
    if (path == null) throw new ArgumentNullException("path");
    return path.Split(
        new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
        StringSplitOptions.RemoveEmptyEntries);
}


static string
GetLocalPath(string fromPath, string toPath)
{
    if (fromPath == null)
        throw new ArgumentNullException("fromPath");
    if (toPath == null)
        throw new ArgumentNullException("toPath");
    if (toPath == fromPath)
        return "";

    var fromPathComponents = SplitPath(fromPath);
    var toPathComponents = SplitPath(toPath);

    if (!fromPathComponents.SequenceEqual(
        toPathComponents.Take(fromPathComponents.Length),
        StringComparer.Ordinal))
    {
        throw new ArgumentException("toPath isn't under fromPath", "toPath");
    }

    return Path.Combine(toPathComponents.Skip(fromPathComponents.Length).ToArray());
}


}
}
