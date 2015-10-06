using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel.Impl;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.ProjectModel
{
    /// <summary>
    /// Provides context information for a workspace, defined by the presence of a global.json file.
    /// </summary>
    public class WorkspaceContext
    {
        public static readonly string GlobalFileName = "global.json";

        public WorkspaceContext(IEnumerable<string> projectSearchPaths, string packagesPath, string rootDirectory)
        {
            ProjectSearchPaths = projectSearchPaths.ToList().AsReadOnly();
            PackagesPath = packagesPath;
            RootDirectory = rootDirectory;
        }

        public string RootDirectory { get; }
        public IReadOnlyCollection<string> ProjectSearchPaths { get; }
        public string PackagesPath { get; }

        // TODO(anurse): This can probably be cached per-root-directory?
        internal static async Task<WorkspaceContext> GetAsync(string projectDirectory)
        {
            // Locate the root directory
            var rootDirectory = ProjectRootResolver.ResolveRootDirectory(projectDirectory);

            // Load the global.json if one is present
            var globalJson = Path.Combine(rootDirectory, GlobalFileName);

            if (!File.Exists(globalJson))
            {
                return new WorkspaceContext(
                    new[] { projectDirectory },
                    packagesPath: null,
                    rootDirectory: projectDirectory);
            }

            try
            {
                JObject json;
                using (var fs = File.OpenRead(globalJson))
                using (var reader = new StreamReader(fs))
                {
                    json = JObject.Parse(await reader.ReadToEndAsync());
                }

                if (json == null)
                {
                    throw new InvalidOperationException("The JSON file can't be deserialized to a JSON object.");
                }

                var projectSearchPaths = (json.Value<JArray>("projects") ??
                                         new JArray()).Values<string>();
                var packagesPath = json.Value<string>("packages");
                return new WorkspaceContext(projectSearchPaths, packagesPath, rootDirectory);
            }
            catch (Exception ex)
            {
                throw FileFormatException.Create(ex, globalJson);
            }
        }
    }
}
