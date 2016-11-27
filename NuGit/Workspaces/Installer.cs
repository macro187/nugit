using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NuGit.Git;
using NuGit.Infrastructure;
using NuGit.VisualStudio;

namespace NuGit.Workspaces
{

    /// <summary>
    /// Dependency install algorithm
    /// </summary>
    ///
    public static class Installer
    {

        const string NugitFolderPrefix = "nugit-";


        /// <summary>
        /// Types of projects to avoid installing as dependencies
        /// </summary>
        ///
        static readonly string[] DependencyTypeBlackList = {
            VisualStudioProjectReference.SolutionFolderTypeId,
        };


        /// <summary>
        /// Import projects from required repositories into Visual Studio solution
        /// </summary>
        ///
        public static void Install(Repository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            var sln = VisualStudioSolution.Find(repository.RootPath);
            if (sln == null) throw new UserErrorException("No .sln file in repo");

            DeleteNugitSolutionFolders(sln);

            foreach (var repoName in DependencyTraverser.GetAllDependencies(repository))
            {
                Install(repository, sln, repoName);
            }

            sln.Save();
        }


        static void Install(Repository repository, VisualStudioSolution sln, GitRepositoryName dependencyName)
        {
            using (TraceExtensions.Step("Installing projects from " + dependencyName))
            {
                var dependencyRepository = repository.Workspace.GetRepository(dependencyName);

                var dependencySln = VisualStudioSolution.Find(dependencyRepository.RootPath);
                if (dependencySln == null)
                {
                    Trace.TraceInformation("No .sln found");
                    return;
                }
                Trace.TraceInformation("Found " + Path.GetFileName(dependencySln.Path));

                var dependencyProjects =
                    dependencySln.ProjectReferences
                        .Where(p => !string.IsNullOrWhiteSpace(p.TypeId))
                        .Where(p => !DependencyTypeBlackList.Contains(p.TypeId))
                        .Where(p => !string.IsNullOrWhiteSpace(p.Location))
                        .Where(p => !Path.IsPathRooted(p.Location))
                        .Where(p => !p.Location.StartsWith("..", StringComparison.Ordinal))
                        .OrderBy(p => p.Name)
                        .ToList();
                if (dependencyProjects.Count == 0)
                {
                    Trace.TraceInformation("No projects found in solution");
                    return;
                }

                var dependencyFolderId = sln.AddSolutionFolder(NugitFolderPrefix + dependencyName);

                foreach (var p in dependencyProjects)
                {
                    Trace.TraceInformation("Installing " + Path.GetFileName(p.Location));
                    sln.AddProjectReference(
                        p.TypeId,
                        p.Name,
                        Path.Combine("..", dependencyName, p.Location),
                        p.Id);
                    sln.AddNestedProject(p.Id, dependencyFolderId);
                }
            }
        }


        static void DeleteNugitSolutionFolders(VisualStudioSolution sln)
        {
            using (TraceExtensions.Step("Deleting Nugit-controlled solution folders"))
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

    }

}
