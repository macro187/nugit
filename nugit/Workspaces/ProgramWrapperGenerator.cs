using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using nugit.Infrastructure;

namespace nugit.Workspaces
{

    /// <summary>
    /// Program wrapper generator
    /// </summary>
    ///
    public static class ProgramWrapperGenerator
    {

        /// <summary>
        /// Generate wrapper scripts for programs in all repositories in a workspace
        /// </summary>
        ///
        public static void GenerateProgramWrappers(Workspace workspace)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");

            var scripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var repository in workspace.GetRepositories())
            {
                scripts.AddRange(GenerateProgramWrappers(repository));
            }

            using (TraceExtensions.Step("Deleting orphan program wrapper scripts"))
            {
                foreach (var file in Directory.GetFiles(workspace.GetProgramWrapperDirectory()))
                {
                    if (scripts.Contains(file)) continue;
                    Trace.WriteLine(file);
                    File.Delete(file);
                }
            }
        }


        /// <summary>
        /// Generate wrapper scripts for programs in a repository
        /// </summary>
        ///
        /// <returns>
        /// Paths of all generated wrapper scripts
        /// </returns>
        ///
        public static IEnumerable<string> GenerateProgramWrappers(Repository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            var paths = new List<string>();

            var dotNugit = repository.ReadDotNugit();
            if (dotNugit.Programs.Count == 0) return paths;

            using (TraceExtensions.Step("Writing program wrapper script(s) for " + repository.Name))
            {
                var programDirectory = repository.Workspace.GetProgramWrapperDirectory();

                foreach (var program in dotNugit.Programs)
                {
                    var programBase = Path.GetFileNameWithoutExtension(program);
                    var target = Path.Combine("..", repository.Name, program);
                    var cmdPath = Path.Combine(programDirectory, programBase) + ".cmd";
                    var shPath = Path.Combine(programDirectory, programBase);
                    var cmd = GenerateCmd(target);
                    var sh = GenerateSh(target);

                    Trace.WriteLine(cmdPath);
                    if (File.Exists(cmdPath)) File.Move(cmdPath, cmdPath); // In case only the casing has changed
                    File.WriteAllText(cmdPath, cmd);
                    paths.Add(cmdPath);

                    Trace.WriteLine(shPath);
                    if (File.Exists(shPath)) File.Move(shPath, shPath); // In case only the casing has changed
                    File.WriteAllText(shPath, sh);
                    paths.Add(shPath);

                    if (!IsOnWindows())
                    {
                        Process.Start("chmod", "u+x \"" + shPath + "\"").WaitForExit();
                    }
                }
            }

            return paths;
        }


        static string GenerateCmd(string target)
        {
            target = target.Replace("/", "\\");
            return "@\"%~dp0" + target + "\" %*\r\n";
        }


        static string GenerateSh(string target)
        {
            var mono = IsOnWindows() ? "" : "mono --debug ";
            target = target.Replace("\\", "/");
            return
                "#!/bin/bash\n" +
                mono + "\"$(dirname $0)/" + target + "\" \"$@\"\n";
        }


        static bool IsOnWindows()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    return false;
                default:
                    return true;
            }
        }

    }

}
