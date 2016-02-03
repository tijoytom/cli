using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build
{
    public class CompileTargets
    {
        public static readonly string CoreCLRVersion = "1.0.1-rc2-23728";

        public static readonly string[] AssembliesToCrossGen = new[]
        {
            "mscorlib.dll",
            "System.Collections.Immutable.dll",
            "System.Reflection.Metadata.dll",
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "Microsoft.CodeAnalysis.VisualBasic.dll",
            "csc.dll",
            "vbc.dll"
        };

        public static readonly string[] BinariesForCoreHost = new[]
        {
            "csi",
            "csc",
            "vbc"
        };

        public static readonly string[] ProjectsToPublish = new[]
        {
            "dotnet"
        };

        public static readonly string[] FilesToClean = new[]
        {
            "README.md",
            "Microsoft.DotNet.Runtime.exe",
            "Microsoft.DotNet.Runtime.dll",
            "Microsoft.DotNet.Runtime.deps",
            "Microsoft.DotNet.Runtime.pdb"
        };

        public static readonly string[] ProjectsToPack = new[]
        {
            "Microsoft.DotNet.Cli.Utils",
            "Microsoft.DotNet.ProjectModel",
            "Microsoft.DotNet.ProjectModel.Loader",
            "Microsoft.DotNet.ProjectModel.Workspaces",
            "Microsoft.Extensions.DependencyModel",
            "Microsoft.Extensions.Testing.Abstractions"
        };

        [Target("CompileCoreHost,CompileStage1,CompileStage2")]
        public static BuildTargetResult Compile(BuildTargetContext c)
        {
            return c.Success();
        }

        [Target]
        public static BuildTargetResult CompileCoreHost(BuildTargetContext c)
        {
            // Generate build files
            var cmakeOut = Path.Combine(OutputDir.Corehost, "cmake");
            FS.Mkdirp(cmakeOut);
            Command.Create("cmake",
                Path.Combine(c.BuildContext.BuildDirectory, "src", "corehost"),
                "-G",
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Visual Studio 14 2015 Win64" : "Unix Makefiles")
                .WorkingDirectory(cmakeOut)
                .Execute()
                .EnsureSuccessful();

            var configuration = (string)c.BuildContext["Configuration"];

            // Run the build
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var pf32 = RuntimeInformation.OSArchitecture == Architecture.X64 ?
                    Environment.GetEnvironmentVariable("ProgramFiles(x86)") :
                    Environment.GetEnvironmentVariable("ProgramFiles");

                if (configuration.Equals("Release"))
                {
                    // Cmake calls it "RelWithDebInfo" in the generated MSBuild
                    configuration = "RelWithDebInfo";
                }

                Command.Create(
                    Path.Combine(pf32, "MSBuild", "14.0", "Bin", "MSBuild.exe"),
                    Path.Combine(cmakeOut, "ALL_BUILD.vcxproj"),
                    $"/p:Configuration={configuration}")
                    .Execute()
                    .EnsureSuccessful();

                // Copy the output out
                File.Copy(Path.Combine(cmakeOut, "cli", "Debug", "corehost.exe"), Path.Combine(OutputDir.Corehost, "corehost.exe"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "Debug", "corehost.pdb"), Path.Combine(OutputDir.Corehost, "corehost.pdb"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", "Debug", "hostpolicy.dll"), Path.Combine(OutputDir.Corehost, "hostpolicy.dll"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", "Debug", "hostpolicy.pdb"), Path.Combine(OutputDir.Corehost, "hostpolicy.pdb"), overwrite: true);
            }
            else
            {
                Command.Create("make")
                    .WorkingDirectory(OutputDir.Corehost)
                    .Execute()
                    .EnsureSuccessful();

                // Copy the output out
                File.Copy(Path.Combine(cmakeOut, "cli", "Debug", "corehost"), Path.Combine(OutputDir.Corehost, "corehost"), overwrite: true);
                File.Copy(Path.Combine(cmakeOut, "cli", "dll", "Debug", $"hostpolicy.{Constants.DynamicLibSuffix}"), Path.Combine(OutputDir.Corehost, $"hostpolicy.{Constants.DynamicLibSuffix}"), overwrite: true);
            }

            return c.Success();
        }

        [Target]
        public static BuildTargetResult CompileStage1(BuildTargetContext c)
        {
            CleanProjects(c);
            return CompileStage(c,
                dotnet: DotNetCli.Stage0,
                outputDir: OutputDir.Stage1);
        }

        [Target]
        public static BuildTargetResult CompileStage2(BuildTargetContext c)
        {
            CleanProjects(c);
            return CompileStage(c,
                dotnet: DotNetCli.Stage1,
                outputDir: OutputDir.Stage2);
        }

        private static BuildTargetResult CompileStage(BuildTargetContext c, DotNetCli dotnet, string outputDir)
        {
            Directory.Delete(outputDir, recursive: true);

            dotnet.SetDotNetHome();

            var configuration = (string)c.BuildContext["Configuration"];
            var binDir = Path.Combine(outputDir, "bin");
            var runtimeOutputDir = Path.Combine(outputDir, "runtime", "coreclr");

            FS.Mkdirp(binDir);
            FS.Mkdirp(runtimeOutputDir);

            foreach (var project in ProjectsToPublish)
            {
                dotnet.Publish(
                    "--native-subdirectory",
                    "--output",
                    binDir,
                    "--configuration",
                    configuration,
                    Path.Combine(c.BuildContext.BuildDirectory, "src", project))
                    .Execute()
                    .EnsureSuccessful();
            }

            // Publish the runtime
            dotnet.Publish(
                "--output",
                runtimeOutputDir,
                "--configuration",
                configuration,
                Path.Combine(c.BuildContext.BuildDirectory, "src", "Microsoft.DotNet.Runtime"))
                .Execute()
                .EnsureSuccessful();

            // Clean bogus files
            foreach (var fileToClean in FilesToClean)
            {
                var pathToClean = Path.Combine(runtimeOutputDir, fileToClean);
                if (File.Exists(pathToClean))
                {
                    File.Delete(pathToClean);
                }
            }

            // Copy corehost
            File.Copy(Path.Combine(OutputDir.Corehost, $"corehost{Constants.ExeSuffix}"), Path.Combine(binDir, $"corehost{Constants.ExeSuffix}"), overwrite: true);
            File.Copy(Path.Combine(OutputDir.Corehost, $"{Constants.DynamicLibPrefix}hostpolicy{Constants.DynamicLibSuffix}"), Path.Combine(binDir, $"{Constants.DynamicLibPrefix}hostpolicy{Constants.DynamicLibSuffix}"), overwrite: true);

            // Corehostify binaries
            foreach(var binaryToCorehostify in BinariesForCoreHost)
            {
                // Yes, it is .exe even on Linux. This is the managed exe we're working with
                File.Move(Path.Combine(binDir, $"{binaryToCorehostify}.exe"), Path.Combine(binDir, $"{binaryToCorehostify}.dll"));
                File.Copy(Path.Combine(binDir, $"corehost{Constants.ExeSuffix}"), Path.Combine(binDir, binaryToCorehostify + Constants.ExeSuffix));
            }

            // Crossgen Roslyn
            var result = Crossgen(c, binDir);
            if(!result.Success)
            {
                return result;
            }

            // Copy AppDeps
            result = CopyAppDeps(c, binDir);
            if(!result.Success)
            {
                return result;
            }

            return c.Success();
        }

        private static BuildTargetResult CopyAppDeps(BuildTargetContext c, string outputDir)
        {
            c.Warn("Copy App Deps not yet implenented");
            return c.Success();
        }

        private static BuildTargetResult Crossgen(BuildTargetContext c, string outputDir)
        {
            // Find crossgen
            string packageId;
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                packageId = "runtime.win7-x64.Microsoft.NETCore.Runtime.CoreCLR";
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return c.Failed("Haven't done Linux crossgen yet. Need to detect Ubuntu vs CentOS");
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                packageId = "runtime.osx.10.10.Microsoft.NETCore.Runtime.CoreCLR";
            }
            else
            {
                return c.Failed("Unsupported OS Platform");
            }

            var crossGenExePath = Path.Combine(
                    Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? GetNuGetPackagesDir(),
                    packageId,
                    CoreCLRVersion,
                    "tools",
                    $"crossgen{Constants.ExeSuffix}");

            // We have to copy crossgen next to mscorlib
            var crossgen = Path.Combine(outputDir, $"crossgen{Constants.ExeSuffix}");
            File.Copy(crossGenExePath, crossgen, overwrite: true);
            FS.Chmod(crossgen, "a+x");

            // And if we have mscorlib.ni.dll, we need to rename it to mscorlib.dll
            if(File.Exists(Path.Combine(outputDir, "mscorlib.ni.dll")))
            {
                File.Copy(Path.Combine(outputDir, "mscorlib.ni.dll"), Path.Combine(outputDir, "mscorlib.dll"), overwrite: true);
            }

            foreach (var assemblyToCrossgen in AssembliesToCrossGen)
            {
                c.Info($"Crossgenning {assemblyToCrossgen}");
                Command.Create(crossgen, "-nologo", "-platform_assemblies_paths", outputDir, assemblyToCrossgen)
                    .WorkingDirectory(outputDir)
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute()
                    .EnsureSuccessful();
            }
            c.Info("Crossgen complete");

            // Check if csc/vbc.ni.exe exists, and overwrite the dll with it just in case
            if(File.Exists(Path.Combine(outputDir, "csc.ni.exe")) && !File.Exists(Path.Combine(outputDir, "csc.ni.dll")))
            {
                File.Move(Path.Combine(outputDir, "csc.ni.exe"), Path.Combine(outputDir, "csc.ni.dll"));
            }

            if(File.Exists(Path.Combine(outputDir, "vbc.ni.exe")) && !File.Exists(Path.Combine(outputDir, "vbc.ni.dll")))
            {
                File.Move(Path.Combine(outputDir, "vbc.ni.exe"), Path.Combine(outputDir, "vbc.ni.dll"));
            }

            return c.Success();
        }

        private static string GetNuGetPackagesDir()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".nuget", "packages");
            }
            return Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".nuget", "packages");
        }

        private static void CleanProjects(BuildTargetContext c)
        {
            foreach(var projectDir in Directory.EnumerateDirectories(Path.Combine(c.BuildContext.BuildDirectory, "src")))
            {
                var binDir = Path.Combine(projectDir, "bin");
                if (Directory.Exists(binDir))
                {
                    Directory.Delete(binDir, recursive: true);
                    c.Info($"Deleting {binDir}");
                }

                var objDir = Path.Combine(projectDir, "obj");
                if (Directory.Exists(objDir))
                {
                    Directory.Delete(objDir, recursive: true);
                    c.Info($"Deleting {objDir}");
                }
            }
        }
    }
}
