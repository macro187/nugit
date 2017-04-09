using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Reflection;
using MacroGit;
using NuGit.Infrastructure;
using NuGit.Workspaces;

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
                    case "UPDATE":
                        return Update(args);
                    case "CLONE":
                        return Clone(args);
                    case "INSTALL":
                        return Install(args);
                    case "PROGRAMS":
                        return Programs(args);
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
            Trace.TraceInformation("==============================");
            Trace.TraceInformation("{0} v{1}.{2}", name, major, minor);
            Trace.TraceInformation(description);
            Trace.TraceInformation("{0} {1}", copyright, authors);
            Trace.TraceInformation("==============================");

        }


        /// <summary>
        /// Print program usage
        /// </summary>
        ///
        static void Usage()
        {
            Trace.WriteLine("");
            Trace.WriteLine("");
            using (var reader = new StreamReader(
                Assembly.GetCallingAssembly().GetManifestResourceStream("NuGit.readme.md")))
            {
                foreach (
                    var line
                    in ReadmeParser.SelectSections(
                        reader.ReadAllLines(),
                        "Synopsis",
                        "Commands"))
                {
                    Trace.WriteLine(line);
                }
            }
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
        /// The <c>update</c> command
        /// </summary>
        ///
        static int Update(Queue<string> args)
        {
            if (args.Any()) throw new UserErrorException("Too many arguments");
            var repository = WhereAmI();
            if (repository == null) throw new UserErrorException("Not in a repository");

            repository.DeleteNuGitLock();
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
                    : new Workspace(Environment.CurrentDirectory);

            DependencyTraverser.Traverse(workspace, new Dependency[] { new Dependency(url, version) });

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

            Installer.Install(repository);

            return 0;
        }


        /// <summary>
        /// The <c>programs</c> command
        /// </summary>
        ///
        static int Programs(Queue<string> args)
        {
            if (args.Any()) throw new UserErrorException("Too many arguments");

            var repository = WhereAmI();

            if (repository != null)
            {
                ProgramWrapperGenerator.GenerateProgramWrappers(repository);
            }
            else
            {
                ProgramWrapperGenerator.GenerateProgramWrappers(new Workspace(Environment.CurrentDirectory));
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
        static Repository WhereAmI()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            while (true)
            {
                if (dir.Parent == null) return null;
                if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) break;
                dir = dir.Parent;
            }
            return new Workspace(dir.Parent.FullName).FindRepository(new GitRepositoryName(dir.Name));
        }

    }

}
