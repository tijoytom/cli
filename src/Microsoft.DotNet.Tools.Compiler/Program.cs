using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tools.Compiler
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "dotnet compile";
            app.FullName = ".NET Compiler";
            app.Description = "Compiler for the .NET Platform";
            app.HelpOption("-h|--help");

            var output = app.Option("-o|--output <OUTPUT_DIR>", "Directory in which to place outputs", CommandOptionType.SingleValue);
            var framework = app.Option("-f|--framework <FRAMEWORK>", "Target framework to compile for", CommandOptionType.SingleValue);
            var packages = app.Option("-p|--packages <PACKAGES_DIRECTORY>", "Path to the directories containing packages to resolve.", CommandOptionType.MultipleValue);
            var project = app.Argument("<PROJECT>", "The project to compile, defaults to the current directory. Can be a path to a project.json or a project directory");

            app.OnExecute(async () =>
            {
                // Validate arguments
                CheckArg(framework, "--framework");

                // Load the project
                var fx = NuGetFramework.Parse(framework.Value());
                var projectContext = await ProjectContext.CreateAsync(project.Value ?? Directory.GetCurrentDirectory(), fx);
                return Compile(projectContext, fx, output.Value(), packages.Values);
            });

            return app.Execute(args);
        }

        private static void CheckArg(CommandOption argument, string name)
        {
            if (!argument.HasValue())
            {
                // TODO: GROOOOOOSS
                Console.Error.WriteLine($"Missing required argument: {name}");
                throw new Exception();
            }
        }

        private static int Compile(ProjectContext project, NuGetFramework framework, string outputPath, IEnumerable<string> packagesDirectories)
        {
            // Make output directory
            // TODO(anurse): per-framework and per-configuration output dir
            // TODO(anurse): configurable base output dir? (maybe dotnet compile doesn't support that?)
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.Combine(project.ProjectDirectory, "bin");
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Build csc args, and report stuff to the user
            var cscArgs = new List<string>()
            {
                "-nostdlib",
                $"-out:{Path.Combine(outputPath, project.Name + ".dll")}"
            };

            // TODO: For now, we only support locating things from packages. Proj->proj is still to be done.
            foreach (var library in project.Libraries.Where(l => l.Type.Equals(LibraryType.Package)))
            {
                // Resolve the path to the library
                string path = LocateLibrary(library, project.Workspace, packagesDirectories);

                // TODO: This is a little verbose, we should add options to reduce the verbosity, but ideally that would
                // be via some kind of common stdout reporting system with exciting colors and stuff.
                Console.WriteLine($"  Using {library.Type} dependency {library.Name} {library.Version}");
                Console.WriteLine($"    Path: {path}");
                foreach (var asset in library.CompilationAssets.Where(a => !a.IsPlaceholder))
                {
                    Console.WriteLine($"    Assembly: {asset.Path}");
                    cscArgs.Add($"-r:{Path.Combine(path, asset.Path)}");
                }

                // Add shared sources to the compilation as well
                foreach (var asset in library.SourceAssets.Where(a => !a.IsPlaceholder))
                {
                    // NOTE(anurse): Even DNX isn't this verbose, but I really want to see if it's working...
                    Console.WriteLine($"    Source: {asset.Path}");
                    cscArgs.Add(Path.Combine(path, asset.Path));
                }

                Console.WriteLine();
            }

            // Add source files from the project to the compilation
            cscArgs.AddRange(project.GetCompilationSource());

            // Write a csc response file
            var rsp = Path.Combine(outputPath, "csc.rsp");
            if (File.Exists(rsp))
            {
                File.Delete(rsp);
            }
            File.WriteAllLines(rsp, cscArgs);

            // Run csc
            return Command.Create("csc", $"@{rsp}")
                .ForwardStdErr(Console.Error)
                .ForwardStdOut(Console.Out)
                .RunAsync()
                .Result
                .ExitCode;
        }

        private static string LocateLibrary(LibraryDescription library, WorkspaceContext workspace, IEnumerable<string> packagesDirectories)
        {
            foreach (var dir in packagesDirectories)
            {
                var path = TryLibraryLocation(dir, library);
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }

            string defaultDir = workspace.PackagesPath;
            if (string.IsNullOrEmpty(defaultDir))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    defaultDir = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".dnx", "packages");
                }
                else
                {
                    defaultDir = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".dnx", "packages");
                }
            }

            return TryLibraryLocation(defaultDir, library);
        }

        private static string TryLibraryLocation(string root, LibraryDescription library)
        {
            var candidatePath = Path.Combine(root, library.Name, library.Version.ToNormalizedString());
            if (Directory.Exists(candidatePath))
            {
                return candidatePath;
            }
            return null;
        }
    }
}
