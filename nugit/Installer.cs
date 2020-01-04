using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MacroGuards;
using MacroDiagnostics;
using MacroExceptions;
using MacroGit;
using MacroIO;
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
NuGitFolderPrefix = "nugit-";


/// <summary>
/// Import projects from required repositories into Visual Studio solution
/// </summary>
///
public static void
Install(NuGitRepository repository)
{
    Guard.NotNull(repository, nameof(repository));

    var sln = repository.FindVisualStudioSolution();
    if (sln == null) throw new UserException("No Visual Studio solution found in repo");

    var repoNames = DependencyTraverser.GetAllDependencies(repository, false);

    var oldFolderIds =
        sln.SolutionFolders
            .Where(f => IsNuGitFolder(f.Name))
            .ToDictionary(f => f.Name, f => f.Id);

    DeleteNuGitFolders(sln);

    foreach (var repoName in repoNames)
    {
        var folderName = NuGitFolderPrefix + repoName;
        oldFolderIds.TryGetValue(folderName, out string folderId);
        Install(repository, sln, repoName, folderName, folderId);
    }

    sln.Save();
}


static void
Install(
    NuGitRepository repository,
    VisualStudioSolution sln,
    GitRepositoryName dependencyName,
    string folderName,
    string folderId
    )
{
    var workspace = repository.Workspace;

    var slnLocalPath = PathExtensions.GetPathFromAncestor(sln.Path, repository.Path);
    var slnLocalPathComponents = PathExtensions.Split(slnLocalPath);
    var slnToWorkspacePath = Path.Combine(Enumerable.Repeat("..", slnLocalPathComponents.Length).ToArray());

    var dependencyRepository = workspace.GetRepository(dependencyName);

    var dependencySln = dependencyRepository.FindVisualStudioSolution();
    if (dependencySln == null) return;

    var dependencySlnDir = Path.GetDirectoryName(dependencySln.Path);

    var dependencyProjects = FindDependencyProjects(dependencyRepository, dependencySln);
    if (dependencyProjects.Count == 0) return;

    using (LogicalOperation.Start("Installing projects from " + dependencySln.Path))
    {
        // TODO Consider configurations in each individual dependency project, not just the solution
        var configurationsInCommon =
            sln.SolutionConfigurations.Intersect(dependencySln.SolutionConfigurations)
                .OrderBy(s => s)
                .ToList();

        var folder =
            folderId != null
                ? sln.AddSolutionFolder(folderName, folderId)
                : sln.AddSolutionFolder(folderName);

        foreach (var project in dependencyProjects)
        {
            var projectLocalPath =
                PathExtensions.GetPathFromAncestor(project.AbsoluteLocation, dependencyRepository.Path);

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
            sln.AddNestedProject(project.Id, folder.Id);

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


static List<VisualStudioSolutionProjectReference>
FindDependencyProjects(NuGitRepository repository, VisualStudioSolution sln)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(sln, nameof(sln));
    return
        sln.ProjectReferences
            .Where(p =>
                p.TypeId == VisualStudioProjectTypeIds.CSharp ||
                p.TypeId == VisualStudioProjectTypeIds.CSharpNew)
            .Where(p => !IsTestProject(p))
            .Where(p => !string.IsNullOrWhiteSpace(p.Location))
            .Where(p => PathExtensions.IsDescendantOf(p.AbsoluteLocation, repository.Path))
            .OrderBy(p => p.Name)
            .ToList();
}


static bool
IsTestProject(VisualStudioSolutionProjectReference project)
{
    Guard.NotNull(project, nameof(project));
    return
        project.Name.IndexOf(".Tests.", StringComparison.OrdinalIgnoreCase) > -1 ||
        project.Name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase);
}


static bool
IsNuGitFolder(string name)
{
    Guard.NotNull(name, nameof(name));
    return name.StartsWith(NuGitFolderPrefix, StringComparison.Ordinal);
}


static void
DeleteNuGitFolders(VisualStudioSolution sln)
{
    Guard.NotNull(sln, nameof(sln));
    while (true)
    {
        var folder = sln.SolutionFolders.FirstOrDefault(f => IsNuGitFolder(f.Name));
        if (folder == null) break;
        sln.DeleteSolutionFolder(folder);
    }
}


}
}
