using NuGit.Git;

namespace NuGit.Workspaces
{

    /// <summary>
    /// A repository subdirectory
    /// </summary>
    ///
    public interface IRepository
    {

        /// <summary>
        /// Workspace the repository is in
        /// </summary>
        ///
        IWorkspace Workspace { get; }


        /// <summary>
        /// Name of the repository subdirectory
        /// </summary>
        ///
        GitRepositoryName Name { get; }


        /// <summary>
        /// Switch the repository to a particular Git commit
        /// </summary>
        ///
        /// <remarks>
        /// Can affect the result of <see cref="GetDotNuGit()"/>.
        /// </remarks>
        ///
        void Checkout(GitCommitName commit);


        /// <summary>
        /// Get the repository's .nugit information
        /// </summary>
        ///
        /// <remarks>
        /// Result can be affected by <see cref="Checkout()"/>.
        /// </remarks>
        ///
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Emphasise that information is reread on each call")]
        DotNuGit GetDotNuGit();

    }

}
