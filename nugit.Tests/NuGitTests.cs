using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MacroDiagnostics;
using MacroGit;

namespace nugit.Tests
{

    [TestClass]
    public class NuGitTests
    {

        #if NET7_0
        const string frameworkMoniker = "net7.0";
        #elif NET461
        const string frameworkMoniker = "net461";
        #elif NETCOREAPP2_0
        const string frameworkMoniker = "netcoreapp2.0";
        #else
        #error Unrecognised build framework
        #endif

        static readonly string here = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        //
        // Source Git repos
        //
        static readonly string srcroot = Path.Combine(here, "src");
        static readonly string srcDirA = Path.Combine(srcroot, "a");
        static readonly string srcDirB = Path.Combine(srcroot, "b");
        static readonly string srcDirC = Path.Combine(srcroot, "c");
        static readonly GitUrl srcUrlA = new GitUrl(Path.Combine("file://" + srcDirA));
        static readonly GitUrl srcUrlB = new GitUrl(Path.Combine("file://" + srcDirB));
        static readonly GitUrl srcUrlC = new GitUrl(Path.Combine("file://" + srcDirC));

        //
        // Workspace Git repos
        //
        static readonly string wrkroot = Path.Combine(here, "wrk");
        static readonly string wrkDirA = Path.Combine(wrkroot, "a");
        static readonly string wrkDirB = Path.Combine(wrkroot, "b");
        static readonly string wrkDirC = Path.Combine(wrkroot, "c");
        static GitRepository wrkRepoA;


        //
        // nugit.exe under test
        //
        static readonly string nugitPath = Path.GetFullPath(Path.Combine(
            here,
            "..", "..", "..", "..",
            "nugit", "bin", "Debug",
            frameworkMoniker,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "nugit.exe"
                : "nugit"));


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
            try
            {
                BuildRepo("a", new GitUrl[] { srcUrlB });
                BuildRepo("b", new GitUrl[] { srcUrlC });
                BuildRepo("c", new GitUrl[] {});
            }
            catch (GitException ge)
            {
                context.WriteLine(ge.CommandLine);
                context.WriteLine(ge.Output);
                throw;
            }
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
        /// Invoke the nugit.exe under test, for use by tests
        /// </summary>
        ///
        static void Nugit(GitRepository repo, string command, params string[] args)
        {
            var allArgs = new [] { command }.Concat(args).ToArray();
            var r = ProcessExtensions.ExecuteCaptured(true, true, repo.Path, nugitPath, allArgs);
            if (r.ExitCode != 0) throw new Exception("nugit failed");
        }


        [TestMethod]
        public void Update_Clones_Direct_Dependencies()
        {
            Nugit(wrkRepoA, "update");
            Assert.IsTrue(GitRepository.IsRepository(wrkDirA));
            Assert.IsTrue(GitRepository.IsRepository(wrkDirB));
        }


        [TestMethod]
        public void Update_Clones_Transitive_Dependencies()
        {
            Nugit(wrkRepoA, "update");
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
            var dotNugit = Path.Combine(dir, ".nugit");

            var repo = GitRepository.Init(dir);
            repo.Config("user.email", "test@example.com");

            if (dependencies.Length > 0)
            {
                File.AppendAllLines(dotNugit, dependencies.Select(d => d.ToString()));
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
