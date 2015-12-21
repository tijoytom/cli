using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using NuGet.Frameworks;

namespace Microsoft.Extensions.DependencyModel
{
    public static class DependencyContextBuilder
    {
        public static DependencyContext Build(CommonCompilerOptions compilerOptions, LibraryExporter libraryExporter, string configuration, NuGetFramework target, string runtime)
        {
            var dependencies = libraryExporter.GetAllExports();

            return new DependencyContext(target.DotNetFrameworkName, runtime,
                GetCompilationOptions(compilerOptions),
                GetLibraries(dependencies, target, configuration, export => export.CompilationAssemblies),
                GetLibraries(dependencies, target, configuration, export => export.RuntimeAssemblies));
        }

        private static CompilationOptions GetCompilationOptions(CommonCompilerOptions compilerOptions)
        {
            return new CompilationOptions(compilerOptions.Defines,
                compilerOptions.LanguageVersion,
                compilerOptions.Platform,
                compilerOptions.AllowUnsafe,
                compilerOptions.WarningsAsErrors,
                compilerOptions.Optimize,
                compilerOptions.KeyFile,
                compilerOptions.DelaySign,
                compilerOptions.PublicSign,
                compilerOptions.EmitEntryPoint);
        }

        private static Library[] GetLibraries(IEnumerable<LibraryExport> dependencies, NuGetFramework target, string configuration, Func<LibraryExport, IEnumerable<LibraryAsset>> assemblySelector)
        {
            return dependencies.Select(export => GetLibrary(export, target, configuration, assemblySelector(export), dependencies)).ToArray();
        }

        private static Library GetLibrary(LibraryExport export, NuGetFramework target, string configuration, IEnumerable<LibraryAsset> libraryAssets, IEnumerable<LibraryExport> dependencies)
        {
            var type = export.Library.Identity.Type.ToString().ToLowerInvariant();

            var serviceable = (export.Library as PackageDescription)?.Library.IsServiceable ?? false;
            var version = dependencies.Where(dependency => dependency.Library.Identity == export.Library.Identity);

            var libraryDependencies = export.Library.Dependencies.Select(libraryRange => GetDependency(libraryRange, dependencies)).ToArray();

            string[] assemblies;
            if (type == "project")
            {
                var isExe = ((ProjectDescription) export.Library)
                    .Project
                    .GetCompilerOptions(target, configuration)
                    .EmitEntryPoint
                    .GetValueOrDefault(false);

                assemblies = new[] { export.Library.Identity.Name + (isExe ? ".exe": ".dll") };
            }
            else
            {
                assemblies = libraryAssets.Select(libraryAsset => libraryAsset.RelativePath).ToArray();
            }

            return new Library(
                type,
                export.Library.Identity.Name,
                export.Library.Identity.Version.ToString(),
                export.Library.Hash,
                assemblies,
                libraryDependencies,
                serviceable
                );
        }

        private static Dependency GetDependency(LibraryRange libraryRange, IEnumerable<LibraryExport> dependencies)
        {
            var version =
                dependencies.First(d => d.Library.Identity.Name == libraryRange.Name)
                    .Library.Identity.Version.ToString();
            return new Dependency(libraryRange.Name, version);
        }
    }
}
