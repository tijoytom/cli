using Microsoft.DotNet.Cli.Build.Framework;
using System.IO;

namespace Microsoft.DotNet.Cli.Build
{
    public class PrepareTargets
    {
        [Target("LocateStage0,RestorePackages")]
        public static BuildTargetResult Prepare(BuildTargetContext c) => c.Success();

        [Target]
        public static BuildTargetResult LocateStage0(BuildTargetContext c)
        {
            // We should have been run in the repo root, so locate the stage 0 relative to current directory
            var stage0 = Path.Combine(Directory.GetCurrentDirectory(), ".dotnet_stage0", c.BuildContext.Uname, "cli", "bin");

            if(!Directory.Exists(stage0))
            {
                return c.Failed($"Stage 0 directory does not exist: {stage0}");
            }

            c.BuildContext["Stage0Bin"] = stage0;

            // Identify the version
            var version = File.ReadAllLines(Path.Combine(stage0, "..", ".version"));
            c.Info($"Using Stage 0 Version: {version[1]}");

            return c.Success();
        }

        [Target]
        public static BuildTargetResult RestorePackages(BuildTargetContext c)
        {
            var dotnet = new DotNetCli((string)c.BuildContext["Stage0Bin"]);

            dotnet.Restore(Path.Combine(c.BuildContext.BuildDirectory, "src")).Execute().EnsureSuccessful();
            dotnet.Restore(Path.Combine(c.BuildContext.BuildDirectory, "tools")).Execute().EnsureSuccessful();

            return c.Success();
        }
    }
}
