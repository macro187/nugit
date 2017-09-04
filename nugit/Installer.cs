using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MacroGuards;
using MacroDiagnostics;
using MacroExceptions;
using MacroGit;
using MacroSln;


namespace
nugit
{


/// <summary>
/// Dependency install algorithm
/// </summary>
///
public static class
Installer
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

    var sln = repository.FindVisualStudioSolution();
    if (sln == null) throw new UserException("No Visual Studio solution found in repo");

    DeleteNugitSolutionFolders(sln);

    foreach (var repoName in DependencyTraverser.GetAllDependencies(repository))
    {
        Install(repository, sln, repoName);
    }

    sln.Save();
}


static void
Install(NuGitRepository repository, VisualStudioSolution sln, GitRepositoryName dependencyName)
{
    var workspace = repository.Workspace;

    var slnLocalPath = GetLocalPath(repository.Path, Path.GetDirectoryName(sln.Path));
    var slnLocalPathComponents = SplitPath(slnLocalPath);
    var slnToWorkspacePath = Path.Combine(Enumerable.Repeat("..", slnLocalPathComponents.Length + 1).ToArray());

    using (LogicalOperation.Start("Installing projects from " + dependencyName))
    {
        var dependencyRepository = workspace.GetRepository(dependencyName);

        var dependencySln = dependencyRepository.FindVisualStudioSolution();
        if (dependencySln == null)
        {
            Trace.TraceInformation("No Visual Studio solution found");
            return;
        }

        var dependencySlnDir = Path.GetDirectoryName(dependencySln.Path);
        var dependencySlnLocalPath = GetLocalPath(dependencyRepository.Path, dependencySln.Path);

        Trace.TraceInformation("Found " + dependencySlnLocalPath);

        var dependencyProjects = FindDependencyProjects(dependencyRepository, dependencySln);
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

        foreach (var project in dependencyProjects)
        {
            var projectPath = Path.GetFullPath(Path.Combine(dependencySlnDir, project.Location));
            var projectLocalPath = GetLocalPath(dependencyRepository.Path, projectPath);

            Trace.TraceInformation("Installing " + projectLocalPath);

            //
            // Add reference to the dependency project
            //
            sln.AddProjectReference(
                project.TypeId,
                project.Name,
                Path.Combine(slnToWorkspacePath, dependencyName, projectLocalPath),
                project.Id);

            //
            // Put it in the dependency's solution folder
            //
            sln.AddNestedProject(project.Id, dependencyFolderId);

            //
            // Add solution -> project configuration mappings
            //
            foreach (string configuration in configurationsInCommon)
            {
                sln.AddProjectConfiguration(project.Id, configuration, "ActiveCfg", configuration);
                sln.AddProjectConfiguration(project.Id, configuration, "Build.0", configuration);
            }
        }
    }
}


static List<VisualStudioProjectReference>
FindDependencyProjects(NuGitRepository repository, VisualStudioSolution sln)
{
    var slnDir = Path.GetDirectoryName(sln.Path);
    return sln.ProjectReferences
        .Where(p => !p.Name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
        .Where(p => !(p.Name.IndexOf(".Tests.", StringComparison.OrdinalIgnoreCase) > -1))
        .Where(p => !string.IsNullOrWhiteSpace(p.TypeId))
        .Where(p => !(p.TypeId == VisualStudioProjectTypeIds.SolutionFolder))
        .Where(p => !string.IsNullOrWhiteSpace(p.Location))
        .Where(p => !Path.IsPathRooted(p.Location))
        .Where(p => PathContains(repository.Path, Path.GetFullPath(Path.Combine(slnDir, p.Location))))
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


static bool
PathContains(string ancestorPath, string descendantPath)
{
    Guard.NotNull(ancestorPath, nameof(ancestorPath));
    Guard.NotNull(descendantPath, nameof(descendantPath));

    var ancestorComponents = SplitPath(ancestorPath);
    var descendantComponents = SplitPath(descendantPath);

    return ancestorComponents.SequenceEqual(
        descendantComponents.Take(ancestorComponents.Length),
        StringComparer.Ordinal);
}


static string
GetLocalPath(string ancestorPath, string descendantPath)
{
    Guard.NotNull(ancestorPath, nameof(ancestorPath));
    Guard.NotNull(descendantPath, nameof(descendantPath));

    if (!PathContains(ancestorPath, descendantPath))
        throw new ArgumentException("Not an descendant of ancestorPath", nameof(descendantPath));

    return Path.Combine(SplitPath(descendantPath).Skip(SplitPath(ancestorPath).Length).ToArray());
}


}
}
