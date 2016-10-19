﻿using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Reflection;

namespace NuGit
{

    static class Program
    {

        static int Main(string[] argArray)
        {
            var traceListener = new NuGitTraceListener();
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
                            throw new NuGitUserErrorException("Unrecognised switch '" + swch + "'"); 
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
                    throw new NuGitUserErrorException("No <command> specified");
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
                    default:
                        Usage();
                        throw new NuGitUserErrorException("Unrecognised <command>");
                }
            }

            //
            // An expected user-facing error occurred
            //
            catch (NuGitUserErrorException ue)
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
        }


        /// <summary>
        /// The <c>help</c> command
        /// </summary>
        ///
        static int Help(Queue<string> args)
        {
            Usage();
            if (args.Any()) throw new NuGitUserErrorException("Too many arguments");
            return 0;
        }


        /// <summary>
        /// The <c>restore</c> command
        /// </summary>
        ///
        static int Restore(Queue<string> args)
        {
            if (args.Any()) throw new NuGitUserErrorException("Too many arguments");
            var repository = WhereAmI();
            if (repository == null) throw new NuGitUserErrorException("Not in a repository");

            Restorer.Restore(repository);

            return 0;
        }


        /// <summary>
        /// The <c>clone</c> command
        /// </summary>
        ///
        static int Clone(Queue<string> args)
        {
            if (!args.Any()) throw new NuGitUserErrorException("Expected <url>");
            var url = new GitUrl(args.Dequeue());
            var version = new GitCommitName(args.Any() ? args.Dequeue() : "master");
            if (args.Any()) throw new NuGitUserErrorException("Too many arguments");

            var repository = WhereAmI();
            var workspace =
                repository != null
                    ? repository.Workspace
                    : new Workspace(Environment.CurrentDirectory);

            Restorer.Restore(workspace, new DependencyInfo[] { new DependencyInfo(url, version) });

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
        static IRepository WhereAmI()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            while (true)
            {
                if (dir.Parent == null) return null;
                if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) break;
                dir = dir.Parent;
            }
            return new Workspace(dir.Parent.FullName).FindRepository(new RepositoryName(dir.Name));
        }

    }

}
