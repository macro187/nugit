using NuGit.Git;

namespace NuGit.Workspaces
{

    /// <summary>
    /// A root directory that contains repository subdirectories
    /// </summary>
    ///
    public interface IWorkspace
    {

        /// <summary>
        /// Look for a repository in the workspace
        /// </summary>
        ///
        /// <param name="name">
        /// Name of the sought-after repository
        /// </param>
        ///
        /// <returns>
        /// The repository in the workspace named <paramref name="name"/>
        /// - OR -
        /// <c>null</c> if no such repository exists
        /// </returns>
        ///
        IRepository FindRepository(GitRepositoryName name);


        /// <summary>
        /// Clone a new repository into the workspace
        /// </summary>
        ///
        IRepository CloneRepository(GitUrl gitUrl);

    }

}
