nugit
=====

A Git-based dependency manager


Description
===========

The core functionality works with any Git repository, but nugit also includes
.NET-specific functionality making it suitable for use as a simplified
replacement for NuGet, hence the play on its name.

nugit is controlled by plain-text `.nugit` files located in the root
directories of Git repositories.


Requirements
============

Microsoft .NET Framework v4.0 or newer.


Features
========

Dependency Management
---------------------

nugit makes it easy to use other Git repositories from your own.

Specify URLs of other Git repositories you require in your own `.nugit` file:

    C:\workspace\myrepo> type .nugit

    https://example.com/repoa.git#master
    https://example.com/repob.git#v2
    ...

Based on that information, the nugit `restore` command makes all required
repositories available as peers to your own, at the required revisions,
including transitive dependencies:

    C:\workspace\myrepo> nugit restore

    ...

    C:\workspace\myrepo> dir /b ..

    repoa
    repob
    repoc
    repod
    myrepo


Visual Studio Projects
----------------------

nugit makes it easy to use .NET projects from other Git repositories.

The nugit `install` command maintains solution folders in your Visual Studio
solution containing projects from all required repositories.  Add project
references to them as needed.

Whenever you add or remove dependencies, update the solution folders by
re-running the `install` command.

nugit does not include unnecessary projects such as unit test suites.


Exported Programs
-----------------

nugit makes it easy to use programs from your repositories.

Specify paths to exportable programs in your `.nugit` file:

    C:\workspace\myrepo> type .nugit

    ...
    program: MyProgram/bin/Debug/MyProgram.exe

Based on that information, the nugit `programs` command maintains a `.bin`
directory containing executable wrapper scripts that run your programs
in-place.  Scripts for both Windows *cmd.exe* and Unix *bash* are maintained:

    C:\workspace\myrepo> nugit programs

    ...

    C:\workspace\myrepo> dir /b ..\.bin

    MyProgram
    MyProgram.cmd

    C:\workspace\myrepo> type ..\.bin\MyProgram

    #!/bin/bash
    "$(dirname $0)/../myrepo/MyProgram/bin/Debug/MyProgram.exe" "$@"

    C:\workspace\myrepo> type ..\.bin\MyProgram.cmd

    @"%~dp0..\myrepo\MyProgram\bin\Debug\MyProgram.exe" %*

Running the nugit `programs` command from your workspace builds scripts for
all programs in all repositories and cleans up orphan scripts.

Adding the `.bin` directory to your system path makes exported programs
available throughout your system.

On Windows, the *bash* scripts are suitable for use in *Git Bash*.  On Unix,
the *bash* scripts run your .NET programs using `mono --debug`.


Stealth Mode
------------

nugit can be used without affecting how your existing repository works.

If you put your `.nugit` file inside a `.nugit/` subdirectory along with a
nugit-specific Visual Studio solution and project(s), NuGit uses them instead.
Your nugit-specific solution and projects can use NuGit-style dependencies
(and be used as such) and the rest of your repository remains unchanged.


Synopsis
========

    nugit.exe <command> [<args>]

        <command>
            The nugit command to execute

        <args>
            Command-specific options and arguments


Commands
========

    help
        Display command line usage information

    update
        Restore dependencies listed in .nugit

        Recursively clone and/or checkout repositories and commits listed
        in .nugit, recording the exact sequence and commit identifiers in
        .nugit.lock

    restore
        Restore dependencies listed in .nugit.lock or .nugit

        If .nugit.lock is present, clone and/or checkout the exact repositories
        and commits recorded in it

        If .nugit.lock is not present, same as the update command

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
    program: Foo/bin/Debug/Foo.exe
    program: Bar/bin/Debug/Bar.exe


License
=======

[MIT License](https://github.com/macro187/nugit/blob/master/license.txt)


Copyright
=========

Copyright (c) 2016-2017  
Ron MacNeil \<<https://github.com/macro187>\>  


Continuous Integration
======================

### Appveyor (Windows)

[![Build status](https://ci.appveyor.com/api/projects/status/f3ng94vkp9kqkska?svg=true)](https://ci.appveyor.com/project/macro187/nugit)

