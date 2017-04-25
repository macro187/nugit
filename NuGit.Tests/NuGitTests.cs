using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MacroDiagnostics;
using MacroGit;

namespace NuGit.Tests
{

    [TestClass]
    public class NuGitTests
    {

        static string here = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        //
        // Source Git repos
        //
        static string srcroot = Path.Combine(here, "src");
        static string srcDirA = Path.Combine(srcroot, "a");
        static string srcDirB = Path.Combine(srcroot, "b");
        static string srcDirC = Path.Combine(srcroot, "c");
        static GitUrl srcUrlA = new GitUrl(Path.Combine("file://" + srcDirA));
        static GitUrl srcUrlB = new GitUrl(Path.Combine("file://" + srcDirB));
        static GitUrl srcUrlC = new GitUrl(Path.Combine("file://" + srcDirC));

        //
        // Workspace Git repos
        //
        static string wrkroot = Path.Combine(here, "wrk");
        static string wrkDirA = Path.Combine(wrkroot, "a");
        static string wrkDirB = Path.Combine(wrkroot, "b");
        static string wrkDirC = Path.Combine(wrkroot, "c");
        static GitRepository wrkRepoA;

        //
        // NuGit.exe under test
        //
        static string nuGitPath = Path.GetFullPath(Path.Combine(
            here,
            "..", "..", "..",
            "NuGit", "bin", "Debug", "NuGit.exe"));


        /// <summary>
        /// Build interdependent source Git repositories for use by tests
        /// </summary>
        ///
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            if (Directory.Exists(srcroot)) NukeDirectory(srcroot);
            if (Directory.Exists(wrkroot)) NukeDirectory(wrkroot);
            Directory.CreateDirectory(srcroot);
            BuildRepo("a", new GitUrl[] { srcUrlB });
            BuildRepo("b", new GitUrl[] { srcUrlC });
            BuildRepo("c", new GitUrl[] {});
        }


        /// <summary>
        /// Begin all tests with a fresh clone of repoA
        /// </summary>
        ///
        [TestInitialize]
        public void TestInitialize()
        {
            if (Directory.Exists(wrkroot)) Directory.Delete(wrkroot, true);
            Directory.CreateDirectory(wrkroot);
            wrkRepoA = GitRepository.Clone(wrkroot, srcUrlA);
        }


        /// <summary>
        /// Invoke the NuGit.exe under test, for use by tests
        /// </summary>
        ///
        static void NuGit(GitRepository repo, string command, params string[] args)
        {
            var allArgs = new [] { command }.Concat(args).ToArray();
            var r = ProcessExtensions.ExecuteCaptured(true, true, repo.Path, nuGitPath, allArgs);
            if (r.ExitCode != 0) throw new Exception("NuGit failed");
        }


        [TestMethod]
        public void Update_Clones_Direct_Dependencies()
        {
            NuGit(wrkRepoA, "update");
            Assert.IsTrue(GitRepository.IsRepository(wrkDirA));
            Assert.IsTrue(GitRepository.IsRepository(wrkDirB));
        }


        [TestMethod]
        public void Update_Clones_Transitive_Dependencies()
        {
            NuGit(wrkRepoA, "update");
            Assert.IsTrue(GitRepository.IsRepository(wrkDirA));
            Assert.IsTrue(GitRepository.IsRepository(wrkDirB));
            Assert.IsTrue(GitRepository.IsRepository(wrkDirC));
        }


        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(wrkroot)) NukeDirectory(wrkroot);
        }


        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (Directory.Exists(srcroot)) NukeDirectory(srcroot);
            if (Directory.Exists(wrkroot)) NukeDirectory(wrkroot);
        }


        static void BuildRepo(string name, GitUrl[] dependencies)
        {
            var dir = Path.Combine(srcroot, name);
            var file = Path.Combine(dir, name + ".txt");
            var dotnugit = Path.Combine(dir, ".nugit");

            var repo = GitRepository.Init(dir);

            if (dependencies.Length > 0)
            {
                File.AppendAllLines(dotnugit, dependencies.Select(d => d.ToString()));
            }
            File.WriteAllText(file, name + "1");
            repo.StageChanges();
            repo.Commit(name + "1");

            File.WriteAllText(file, name + "2");
            repo.StageChanges();
            repo.Commit(name + "2");
        }


        static void NukeDirectory(string path)
        {
            var dir = new DirectoryInfo(path);
            var infos =
                new FileSystemInfo[] { dir }
                .Concat(dir.GetFileSystemInfos("*", SearchOption.AllDirectories));
            foreach (var info in infos) info.Attributes &= ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }

    }

}
