using Microsoft.Extensions.PlatformAbstractions;
using System.IO;

namespace Microsoft.DotNet.Cli.Build
{
    public static class OutputDir
    {
        public static readonly string Base = Path.Combine(
            Directory.GetCurrentDirectory(),
            "artifacts",
            PlatformServices.Default.Runtime.GetRuntimeIdentifier());
        public static readonly string Stage1 = Path.Combine(Base, "stage1");
        public static readonly string Stage2 = Path.Combine(Base, "stage2");
        public static readonly string Corehost = Path.Combine(Base, "corehost");
    }
}
