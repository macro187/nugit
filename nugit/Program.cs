using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Reflection;
using MacroIO;
using MacroExceptions;
using MacroConsole;
using MacroGit;


namespace
nugit
{


static class
Program
{


static int
Main(string[] args)
{
    var traceListener = new ConsoleApplicationTraceListener();
    Trace.Listeners.Add(traceListener);

    try
    {
        try
        {
            return Main2(new Queue<string>(args));
        }
        catch (TextFileParseException tfpe)
        {
            throw new UserException(tfpe);
        }
    }

    //
    // An expected user-facing error occurred
    //
    catch (UserException ue)
    {
        Trace.WriteLine("");
        foreach (var ex in ue.UserFacingExceptionChain) Trace.TraceError(ex.Message);
        return 1;
    }

    //
    // An unexpected internal error occurred
    //
    catch (Exception e)
    {
        Trace.WriteLine("");
        Trace.TraceError("An internal error occurred in nugit:");
        Trace.TraceError(ExceptionExtensions.Format(e));
        return 1;
    }
}


static int
Main2(Queue<string> args)
{
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
                throw new UserException("Unrecognised switch '" + swch + "'"); 
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
        throw new UserException("No <command> specified");
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
        case "INSTALL":
            return Install(args);
        default:
            Usage();
            throw new UserException("Unrecognised <command>");
    }
}


/// <summary>
/// Print program banner
/// </summary>
///
static void
Banner()
{
    var name = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName;
    var version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
    Trace.TraceInformation("");
    Trace.TraceInformation("==============================");
    Trace.TraceInformation("{0} {1}", name, version);
    Trace.TraceInformation("==============================");
    Trace.TraceInformation("");
}


/// <summary>
/// Print program usage
/// </summary>
///
static void
Usage()
{
    Trace.WriteLine("");
    Trace.WriteLine("");
    using (var reader = new StreamReader(
        Assembly.GetCallingAssembly().GetManifestResourceStream("nugit.readme.md")))
    {
        foreach (
            var line
            in ReadmeFilter.SelectSections(
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
static int
Help(Queue<string> args)
{
    Usage();
    if (args.Any()) throw new UserException("Too many arguments");
    return 0;
}


/// <summary>
/// The <c>restore</c> command
/// </summary>
///
static int
Restore(Queue<string> args)
{
    var exact = false;
    while (args.Any())
    {
        var arg = args.Dequeue();
        switch (arg)
        {
            case "--exact":
                exact = true;
                break;
            default:
                throw new UserException($"Unexpected argument '{arg}'");
        }
    }

    var repository = WhereAmI();
    if (repository == null) throw new UserException("Not in a repository");

    if (!repository.HasDotNuGitLock())
    {
        Trace.TraceError("No .nugit.lock file present, 'nugit update' to create it");
        throw new UserException("No .nugit.lock file present");
    }

    DependencyTraverser.Restore(repository, exact);

    return 0;
}


/// <summary>
/// The <c>update</c> command
/// </summary>
///
static int
Update(Queue<string> args)
{
    if (args.Any()) throw new UserException("Too many arguments");
    var repository = WhereAmI();
    if (repository == null) throw new UserException("Not in a repository");

    DependencyTraverser.Update(repository);

    return 0;
}


/// <summary>
/// The <c>install</c> command
/// </summary>
///
static int
Install(Queue<string> args)
{
    if (args.Any()) throw new UserException("Too many arguments");

    var repository = WhereAmI();
    if (repository == null) throw new UserException("Not in a repository");

    if (!repository.HasDotNuGitLock())
    {
        Trace.TraceError("No .nugit.lock file present, 'nugit update' to create it");
        throw new UserException("No .nugit.lock file present");
    }

    DependencyTraverser.Restore(repository, true);
    var dependencies = repository.ReadDotNuGitLock();
    Installer.Install(repository, dependencies);

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
static NuGitRepository
WhereAmI()
{
    var repo = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
    if (repo == null) return null;
    return
        new NuGitWorkspace(Path.GetDirectoryName(repo.Path))
            .FindRepository(new GitRepositoryName(Path.GetFileName(repo.Path)));
}


}
}
