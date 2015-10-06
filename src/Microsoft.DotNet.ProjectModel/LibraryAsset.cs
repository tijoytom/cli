using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.DotNet.ProjectModel
{
    public class LibraryAsset
    {
        /// <summary>
        /// Gets the relative path for this asset within the library.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets additional metadata that describes this asset, such as locale
        /// </summary>
        public IReadOnlyDictionary<string, string> Metadata { get; }

        public bool IsPlaceholder { get { return Path.EndsWith("_._"); } }

        public LibraryAsset(string path) : this(path, Enumerable.Empty<KeyValuePair<string, string>>()) { }

        public LibraryAsset(string path, IEnumerable<KeyValuePair<string, string>> metadata)
        {
            Path = path;
            Metadata = new ReadOnlyDictionary<string, string>(metadata.ToDictionary(p => p.Key, p => p.Value));
        }
    }
}