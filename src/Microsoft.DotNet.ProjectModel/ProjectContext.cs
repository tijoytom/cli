using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel.Impl;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class ProjectContext
    {
        private static readonly IDictionary<string, string> DefaultSourceFileMetadata = new Dictionary<string, string>
        {
            { "language", "csharp" }
        };

        // TODO(anurse): For now, the Project object is an implementation detail. It's not a very clean API right now...
        // It will need to be stabilized somewhat in order to be able to provide data to compilers.
        private Project _project;

        private Dictionary<string, LibraryDescription> _libraryLookup;

        /// <summary>
        /// Gets a list of all libraries resolved for the project
        /// </summary>
        public IEnumerable<LibraryDescription> Libraries
        {
            get { return _libraryLookup.Values; }
        }

        public WorkspaceContext Workspace { get; }
        public string Name { get { return _project.Name; } }
        public string ProjectDirectory { get { return _project.ProjectDirectory; } }

        // TODO(anurse): This should be configurable :)
        public string OutputDirectory { get { return Path.Combine(ProjectDirectory, "bin"); } }

        private ProjectContext(Project project, IEnumerable<LibraryDescription> libraries, WorkspaceContext workspace)
        {
            _project = project;
            _libraryLookup = libraries.ToDictionary(l => l.Name);
            Workspace = workspace;
        }

        /// <summary>
        /// Returns a <see cref="LibraryDescription"/> for the library that matches the provided name,
        /// or returns null if no such library is present in the project.
        /// </summary>
        public LibraryDescription ResolveLibrary(string name)
        {
            LibraryDescription desc;
            if (!_libraryLookup.TryGetValue(name, out desc))
            {
                return null;
            }
            return desc;
        }

        // TODO: See if we can abstract the file system stuff away
        public static Task<ProjectContext> CreateAsync(string projectPath, NuGetFramework targetFramework)
        {
            return CreateAsync(projectPath, targetFramework, targetRuntime: null);
        }

        public static async Task<ProjectContext> CreateAsync(string projectPath, NuGetFramework targetFramework, string targetRuntime)
        {
            string projectDirectory;
            if (projectPath.EndsWith(Project.FileName))
            {
                projectDirectory = Path.GetDirectoryName(projectPath);
            }
            else
            {
                projectDirectory = projectPath;
                projectPath = Path.Combine(projectDirectory, Project.FileName);
            }
            var projectFile = new FileInfo(projectPath);

            // Resolve workspace context
            var workspace = await WorkspaceContext.GetAsync(projectDirectory);

            // Parse the project file
            // TODO(anurse): For now, we let FileNotFoundException throw. We should probably handle it better
            Project project;
            var diagnostics = new List<DiagnosticMessage>();
            using (var stream = new FileStream(projectPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                project = new ProjectReader().ReadProject(stream, projectFile.Directory.Name, projectFile.FullName, diagnostics);
            }
            if (diagnostics.HasErrors())
            {
                throw new InvalidOperationException(
                    "Errors parsing project.json: " + Environment.NewLine +
                    string.Join(Environment.NewLine, diagnostics.Select(d => d.FormattedMessage)));
            }

            // Read the lock file
            LockFile lockFile;
            var lockFilePath = Path.Combine(projectDirectory, LockFile.FileName);
            using (var stream = await FileSystemUtility.OpenFileStreamAsync(lockFilePath))
            {
                lockFile = new LockFileFormat().Read(stream);
            }
            var lookup = new LockFileLookup(lockFile);

            // Select the appropriate target
            var target = SelectTarget(lockFile, targetFramework, targetRuntime);
            if (target == null)
            {
                // TODO: Report diagnostics?
                throw new InvalidOperationException("Unable to locate an appropriate target in the lock file");
            }

            // Load library descriptions
            var libraries = target
                .Libraries
                .Select(t => CreateLibraryDescription(t, lookup, workspace))
                .Where(l => l != null);

            // Create the context from the lock file
            return new ProjectContext(project, libraries, workspace);
        }

        public IEnumerable<string> GetCompilationSource()
        {
            return _project.Files.SourceFiles;
        }

        private static LibraryDescription CreateLibraryDescription(string projectDirectory, LockFileTargetLibrary library, LockFileLookup lookup, WorkspaceContext workspace)
        {
            // Collect source paths
            var sourcePaths = new List<string>();
            var package = lookup.GetPackage(library.Name, library.Version);
            if (package != null)
            {
                sourcePaths.AddRange(package.Files.Where(f => f.StartsWith("shared" + Path.DirectorySeparatorChar)));

                // Calculate metadata about shared sources
                var sourceAssets = sourcePaths.Select(s => new LibraryAsset(s, DefaultSourceFileMetadata));

                return new LibraryDescription(
                    library.Name,
                    library.Version,
                    library.Type,
                    library.TargetFramework,
                    library.Dependencies,
                    library.CompileTimeAssemblies.Select(CreateAsset),
                    library.RuntimeAssemblies.Select(CreateAsset),
                    library.NativeLibraries.Select(CreateAsset),
                    sourceAssets,
                    library.FrameworkAssemblies);
            }
            else
            {
                var project = lookup.GetProject(library.Name);
                if (project != null)
                {
                    // Locate the project
                    // TODO: Temporary code to resolve the path to the project. Needs to be cleaned up a bunch
                    var targetProjectPath = Path.GetFullPath(Path.Combine(projectDirectory, project.Path));
                    var targetProjectDir = Path.GetDirectoryName(targetProjectPath);
                    var targetOutputDir = Path.Combine(projectDirectory, "bin", library.TargetFramework.GetShortFolderName(), library.Name + ".dll");

                    // Synthesize a LibraryDescription
                    // TODO(anurse): We need to actually ensure we build the project on-demand :)
                    // This just assumes it has been built.
                    return new LibraryDescription(
                        library.Name,
                        library.Version,
                        library.Type,
                        library.TargetFramework,
                        library.Dependencies,
                        new LibraryAsset(targetOutputDir),
                        new LibraryAsset(targetOutputDir),
                        Enumerable.Empty<LibraryAsset>(),

                }
            }

            // Unknown library!
            // TODO(anurse): Maybe we need to return an "unresolved" description?
            // TODO(anurse): Project->Project dependencies need to be handled!
            return null;
        }

        private static LibraryAsset CreateAsset(LockFileItem item)
        {
            return new LibraryAsset(item.Path, item.Properties);
        }

        private static LockFileTarget SelectTarget(LockFile lockFile, NuGetFramework targetFramework, string targetRuntime)
        {
            foreach (var scanTarget in lockFile.Targets)
            {
                if (Equals(scanTarget.TargetFramework, targetFramework) &&
                    string.Equals(scanTarget.RuntimeIdentifier, targetRuntime, StringComparison.Ordinal))
                {
                    return scanTarget;
                }
            }

            return null;
        }
    }
}
