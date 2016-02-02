using System;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var buildModel = BuildModel.Create<Program>();
        
            Reporter.Output.WriteBanner("Starting .NET Core CLI build");
        }
    }
}
