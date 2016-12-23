Description
===========

NuGit is a software development "package" manager that works with regular Git
repositories instead of binary packages.

NuGit operates according to `.nugit` files placed (usually) in the root
directories of Git repositories.  These files contain lists of other Git
repositories required, optionally with particular revisions.  When run, NuGit
recursively fetches and checks out those Git repositories as siblings, thereby
restoring all directly and transitively required Git repositories at the
required revisions.

Although NuGit works with all kinds of Git repositories, it includes
.NET-specific functionality for adding projects from required repositories to
Visual Studio solutions so they can be easily used via project references.
This makes it suitable for use as a simplified, source code-based replacement
for NuGet, hence the play on its name.


Usage
=====

List URLs of required Git repositories in a `.nugit` file in the root
directory of your repository.

Run `nugit restore` from within your repository.  NuGit will fetch and check
out the repositories listed, then do the same for all of them, recursively.

If you are doing .NET development, run `nugit install` from within your
repository.  NuGit will restore all required Git repositories as above, and
then add (most) .NET projects from those repositories to your Visual Studio
solution, organised into solution folders by repository.  Add project
references to them as required.  Re-run `nugit install` any time to refresh
the projects in your solution.

If you want to make an existing .NET repository work with NuGit without
affecting how it currently works, put your `.nugit` file inside a `.nugit/`
subdirectory along with a NuGit-specific Visual Studio solution and
project(s).  If present, NuGit will use them instead.


File Format
===========

`.nugit` files are plain text files.  Empty lines and lines beginning with
hash characters are ignored.

    #
    # Required Git repositories
    #
    https://example.com/example1.git
    https://example.com/example2.git#master
    https://example.com/example3.git#v1.0
    https://example.com/example4.git#2482911091ab7219ba544aeb6969f07904a2d1b0

    #
    # Programs provided by this repository
    #
    program: relative/path/to/program.exe


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

    programs
        Build or update program wrapper scripts in `<workspace>/.bin` for
        programs in the current repository (if run from a repository) or
        programs in all repositories (if run from a workspace)

