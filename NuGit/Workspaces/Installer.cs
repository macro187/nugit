using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MacroGit;
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
            VisualStudioProjectTypeIds.SolutionFolder,
            VisualStudioProjectTypeIds.Test,
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
            var slnLocalPath = GetLocalPath(repository.RootPath, Path.GetDirectoryName(sln.Path));
            var slnLocalPathComponents = SplitPath(slnLocalPath);
            var slnToWorkspacePath = Path.Combine(Enumerable.Repeat("..", slnLocalPathComponents.Length + 1).ToArray());

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

                var dependencySlnLocalPath =
                    GetLocalPath(dependencyRepository.RootPath, Path.GetDirectoryName(dependencySln.Path));

                var dependencyProjects =
                    dependencySln.ProjectReferences
                        .Where(p => !string.IsNullOrWhiteSpace(p.TypeId))
                        .Where(p => !DependencyTypeBlackList.Contains(p.TypeId))
                        .Where(p => !string.IsNullOrWhiteSpace(p.Location))
                        .Where(p => !Path.IsPathRooted(p.Location))
                        .Where(p => !p.Location.StartsWith("..", StringComparison.Ordinal))
                        .Where(p => !p.GetProject().ProjectTypeGuids.Intersect(DependencyTypeBlackList).Any())
                        .OrderBy(p => p.Name)
                        .ToList();
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


        static string[] SplitPath(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            return path.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);
        }


        static string GetLocalPath(string fromPath, string toPath)
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
