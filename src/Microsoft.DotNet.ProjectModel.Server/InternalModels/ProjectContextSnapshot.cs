// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Server.Helpers;
using Microsoft.DotNet.ProjectModel.Server.Models;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel.Server
{
    internal class ProjectContextSnapshot
    {
        public string RootDependency { get; set; }
        public NuGetFramework TargetFramework { get; set; }
        public IReadOnlyList<string> SourceFiles { get; set; }
        public CommonCompilerOptions CompilerOptions { get; set; }
        public IReadOnlyList<ProjectReferenceDescription> ProjectReferences { get; set; }
        public IReadOnlyList<string> FileReferences { get; set; }
        public IReadOnlyList<DiagnosticMessage> DependencyDiagnostics { get; set; }
        public IDictionary<string, DependencyDescription> Dependencies { get; set; }

        public static ProjectContextSnapshot Create(ProjectContext context, string configuration, IEnumerable<string> currentSearchPaths)
        {
            var snapshot = new ProjectContextSnapshot();

            var allDependencyDiagnostics = new List<DiagnosticMessage>();
            allDependencyDiagnostics.AddRange(context.LibraryManager.GetAllDiagnostics());
            allDependencyDiagnostics.AddRange(DependencyTypeChangeFinder.Diagnose(context, currentSearchPaths));

            var diagnosticsLookup = allDependencyDiagnostics.ToLookup(d => d.Source);

            var allExports = context.CreateExporter(configuration).GetAllExports();
            var allSourceFiles = new List<string>(context.ProjectFile.Files.SourceFiles);
            var allFileReferences = new List<string>();
            var allProjectReferences = new List<ProjectReferenceDescription>();
            var allDependencies = new Dictionary<string, DependencyDescription>();
            var allLibrariesDictionary = BuildLibrariesDictionary(allExports);

            foreach (var export in allExports)
            {
                allSourceFiles.AddRange(export.SourceReferences);
                allFileReferences.AddRange(export.CompilationAssemblies.Select(asset => asset.ResolvedPath));

                var diagnostics = diagnosticsLookup[export.Library].ToList();
                var description = DependencyDescription.Create(export.Library, diagnostics, allLibrariesDictionary);
                allDependencies[description.Name] = description;

                var projectDescription = export.Library as ProjectDescription;
                if (projectDescription != null && projectDescription.Identity.Name != context.ProjectFile.Name)
                {
                    allProjectReferences.Add(ProjectReferenceDescription.Create(projectDescription));
                }
            }

            snapshot.RootDependency = context.ProjectFile.Name;
            snapshot.TargetFramework = context.TargetFramework;
            snapshot.SourceFiles = allSourceFiles.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path).ToList();
            snapshot.CompilerOptions = context.ProjectFile.GetCompilerOptions(context.TargetFramework, configuration);
            snapshot.ProjectReferences = allProjectReferences.OrderBy(reference => reference.Name).ToList();
            snapshot.FileReferences = allFileReferences.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path).ToList();
            snapshot.DependencyDiagnostics = allDependencyDiagnostics;
            snapshot.Dependencies = allDependencies;

            return snapshot;
        }

        /// <summary>
        /// Build a dictionary of LibraryDescription from a list of LibraryExports.
        /// 
        /// When one library name can be mapped to multiple LibraryExports the reference assembly library always win.
        /// </summary>
        private static IDictionary<string, LibraryDescription> BuildLibrariesDictionary(IEnumerable<LibraryExport> exports)
        {
            return exports
                .ToLookup(export => export.Library.Identity.Name)
                .Select(group =>
                {
                    if (group.Count() == 1)
                    {
                        return new KeyValuePair<string, LibraryDescription>(group.Key, group.First().Library);
                    }
                    else
                    {
                        var referenceAssemblyExport = group.FirstOrDefault(exp => exp.Library.Identity.Type == LibraryType.ReferenceAssembly);
                        if (referenceAssemblyExport != null)
                        {
                            return new KeyValuePair<string, LibraryDescription>(group.Key, referenceAssemblyExport.Library);
                        }
                        else
                        {
                            return new KeyValuePair<string, LibraryDescription>(group.Key, group.First().Library);
                        }
                    }
                })
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}
