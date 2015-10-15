using System.IO;
using NuGet.Commands;
using NuGet.Configuration;
using NuGet.ProjectModel;

namespace Microsoft.DotNet.Tools.Restore
{
    public class Program
    {
        public void Main(string[] args)
        {
            var cmd = new RestoreCommand(
                new Logger(),
                new RestoreRequest(
                    JsonPackageSpecReader.GetPackageSpec(
                        File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "project.json")),
                        Path.GetFileName(Directory.GetCurrentDirectory()),
                        Path.Combine(Directory.GetCurrentDirectory(), "project.json")),
                    new[] {
                        new PackageSource("https://www.myget.org/F/aspnetcidev/api/v3/index.json", "AspNetCIDev"),
                        new PackageSource("https://www.myget.org/F/dotnet-core/api/v3/index.json", "dotnet-core"),
                        new PackageSource("https://api.nuget.org/v3/index.json", "api.nuget.org"),
                    },
                    @"C:\Users\anurse\.dnx\packages"));
            var result = cmd.ExecuteAsync().Result;
        }
    }
}
