Description
===========

NuGit is a tool that reads files named .nugit located in the root directories
of Git repositories and, according to the Git URLs listed there, fetches and
checks out particular revisions of other Git repositories as siblings.  It does
this recursively, thereby restoring all directly and transitively required Git
repositories to their specified revisions.

NuGit is suitable for use with Git repositories containing any kind of content.
In addition, it provides .NET-specific functionality aimed at making it a much
simpler replacement for the dependency management functionality of NuGet, hence
the play on its name.


Synopsis
========

    NuGit.exe <command> [<args>]

        <command>
            The NuGit command to execute

        <args>
            Command-specific options and arguments


Commands
========

    help
        Display command line usage information

    restore
        Clone and checkout the specified revisions of the Git repositories
        listed in the current repository's .nugit file, then do the same,
        recursively, for each
        
    clone <url> [<version>]
        Clone a repository into the current workspace and restore its required
        repositories as per the restore command

        <url>
            URL of the Git repository to clone

        <version>
            Git revision to use (default master)

    install
        Install Visual Studio projects from all required repositories into the
        current repository's Visual Studio solution, organised into solution
        folders by repository name
