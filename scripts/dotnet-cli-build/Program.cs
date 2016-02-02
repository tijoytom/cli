using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class Program
    {
        public static void Main(string[] args) => BuildSetup.Create()
            .UseStandardGoals()
            .Run(args);
    }
}
