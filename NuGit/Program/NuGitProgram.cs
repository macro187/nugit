using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Reflection;
using NuGit.Infrastructure;
using NuGit.Git;
using NuGit.Workspaces;
using NuGit.FileSystemWorkspaces;
using NuGit.VisualStudio;

namespace NuGit
{

    static class NuGitProgram
    {

        static int Main(string[] argArray)
        {
            var traceListener = new ConsoleApplicationTraceListener();
            Trace.Listeners.Add(traceListener);
            try
            {
                var args = new Queue<string>(argArray);

                //
                // Global switches
                //
                while (args.Count > 0 && args.Peek().ToUpperInvariant().StartsWith("--", StringComparison.Ordinal))
                {
                    string swch = args.Dequeue().ToUpperInvariant();
                    switch (swch)
                    {
                        case "--QUIET":
                            traceListener.Quiet = true;
                            break;
                        default:
                            Usage();
                            throw new UserErrorException("Unrecognised switch '" + swch + "'"); 
                    }
                }

                //
                // Print program banner
                //
                Banner();

                //
                // Get <command>
                //
                if (!args.Any())
                {
                    Usage();
                    throw new UserErrorException("No <command> specified");
                }
                string command = args.Dequeue();

                //
                // Dispatch based on <command>
                //
                switch (command.ToUpperInvariant())
                {
                    case "HELP":
                        return Help(args);
                    case "RESTORE":
                        return Restore(args);
                    case "CLONE":
                        return Clone(args);
                    case "INSTALL":
                        return Install(args);
                    default:
                        Usage();
                        throw new UserErrorException("Unrecognised <command>");
                }
            }

            //
            // An expected user-facing error occurred
            //
            catch (UserErrorException ue)
            {
                Trace.WriteLine("");
                Trace.TraceError(ue.Message);
                return 1;
            }

            //
            // An unexpected internal error occurred
            //
            catch (Exception e)
            {
                Trace.WriteLine("");
                Trace.TraceError("An unexpected error occurred in the program");
                Trace.TraceError(e.ToString());
                return 1;
            }
        }


        /// <summary>
        /// Print program banner
        /// </summary>
        ///
        static void Banner()
        {
            var name = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName;
            var description = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileDescription;
            var major = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductMajorPart;
            var minor = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductMinorPart;
            var copyright = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).LegalCopyright;
            var authors = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).CompanyName;
            Trace.TraceInformation("");
            Trace.TraceInformation("--------------------------------------");
            Trace.TraceInformation("{0} - {1}", name, description);
            Trace.TraceInformation("Version {0}.{1}", major, minor);
            Trace.TraceInformation("{0} {1}", copyright, authors);
            Trace.TraceInformation("--------------------------------------");

        }


        /// <summary>
        /// Print program usage
        /// </summary>
        ///
        static void Usage()
        {
            Trace.WriteLine("");
            Trace.WriteLine("SYNOPSIS");
            Trace.WriteLine("");
            Trace.WriteLine("  NuGit.exe [--quiet] <command> [<args>]");
            Trace.WriteLine("");
            Trace.WriteLine("OPTIONS");
            Trace.WriteLine("");
            Trace.WriteLine("  --quiet");
            Trace.WriteLine("    Mute unnecessary output");
            Trace.WriteLine("");
            Trace.WriteLine("  <command>");
            Trace.WriteLine("    The NuGit command to execute");
            Trace.WriteLine("");
            Trace.WriteLine("  <args>");
            Trace.WriteLine("    Command-specific options and arguments");
            Trace.WriteLine("");
            Trace.WriteLine("COMMANDS");
            Trace.WriteLine("");
            Trace.WriteLine("  help");
            Trace.WriteLine("    Display NuGit command line usage information");
            Trace.WriteLine("");
            Trace.WriteLine("  restore");
            Trace.WriteLine("    Restore dependencies for the current repository");
            Trace.WriteLine("");
            Trace.WriteLine("  clone <url> [<version>]");
            Trace.WriteLine("    Clone a repository into the current workspace and restore its dependencies");
            Trace.WriteLine("");
            Trace.WriteLine("    <url>");
            Trace.WriteLine("      URL of repository to clone");
            Trace.WriteLine("");
            Trace.WriteLine("    <version>");
            Trace.WriteLine("      Commit to use (default master)");
            Trace.WriteLine("");
            Trace.WriteLine("  install");
            Trace.WriteLine("    (Re)install dependencies into current repository's Visual Studio solution");
        }


        /// <summary>
        /// The <c>help</c> command
        /// </summary>
        ///
        static int Help(Queue<string> args)
        {
            Usage();
            if (args.Any()) throw new UserErrorException("Too many arguments");
            return 0;
        }


        /// <summary>
        /// The <c>restore</c> command
        /// </summary>
        ///
        static int Restore(Queue<string> args)
        {
            if (args.Any()) throw new UserErrorException("Too many arguments");
            var repository = WhereAmI();
            if (repository == null) throw new UserErrorException("Not in a repository");

            DependencyTraverser.Traverse(repository);

            return 0;
        }


        /// <summary>
        /// The <c>clone</c> command
        /// </summary>
        ///
        static int Clone(Queue<string> args)
        {
            if (!args.Any()) throw new UserErrorException("Expected <url>");
            var url = new GitUrl(args.Dequeue());
            var version = new GitCommitName(args.Any() ? args.Dequeue() : "master");
            if (args.Any()) throw new UserErrorException("Too many arguments");

            var repository = WhereAmI();
            var workspace =
                repository != null
                    ? repository.Workspace
                    : new FileSystemWorkspace(Environment.CurrentDirectory);

            DependencyTraverser.Traverse(workspace, new GitDependencyInfo[] { new GitDependencyInfo(url, version) });

            return 0;
        }


        /// <summary>
        /// The <c>install</c> command
        /// </summary>
        ///
        static int Install(Queue<string> args)
        {
            if (args.Any()) throw new UserErrorException("Too many arguments");

            var repository = WhereAmI();
            if (repository == null) throw new UserErrorException("Not in a repository");

            var slnFiles = Directory.GetFiles(repository.RootPath, "*.sln");
            if (slnFiles.Length == 0)
            {
                Trace.TraceWarning("No .sln file(s) found in current repository, doing nothing");
                return 0;
            }
            if (slnFiles.Length > 1)
            {
                throw new UserErrorException("More than one .sln file found in current repository");
            }
            var slnFile = slnFiles[0];
            VisualStudioSolution solution;
            try
            {
                solution = new VisualStudioSolution(File.ReadLines(slnFile));
            }
            catch (FileParseException fpe)
            {
                fpe.Path = slnFile;
                throw;
            }
            foreach (var r in solution.ProjectReferences)
            {
                Trace.TraceInformation(r.ToString());
            }
            return 0;
        }


        /// <summary>
        /// Locate the repository the current directory is in
        /// </summary>
        ///
        /// <returns>
        /// The repository the current directory is in
        /// - OR -
        /// <c>null</c> if it is not in a repository
        /// </returns>
        ///
        static FileSystemRepository WhereAmI()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            while (true)
            {
                if (dir.Parent == null) return null;
                if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) break;
                dir = dir.Parent;
            }
            return new FileSystemWorkspace(dir.Parent.FullName).FindRepository(new GitRepositoryName(dir.Name));
        }

    }

}
