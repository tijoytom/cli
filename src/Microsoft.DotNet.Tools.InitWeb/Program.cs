using System;
using System.IO;
using System.Linq;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.DotNet.Cli.Utils;

namespace Microsoft.DotNet.Tools.InitWeb
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            var app = new CommandLineApplication();
            app.Name = "dotnet init-web";
            app.FullName = ".NET Web Project Initializer";
            app.Description = "Web project initializer for the .NET Platform";
            app.HelpOption("-h|--help");

            var projectName = app.Argument("<name>", "The name to use when initializing the web project");

            app.OnExecute(() =>
            {
                if (!CheckProjectName(projectName.Value))
                {
                    Reporter.Error.WriteLine("Please provide a valid project name".Red().Bold());
                    return 1;
                }

                var projectDirPath = Path.Combine(Directory.GetCurrentDirectory(), projectName.Value);

                if (Directory.Exists(projectDirPath))
                {
                    Console.Error.WriteLine($"The directory {projectDirPath} already exists");
                    return 1;
                }

                var projectDir = Directory.CreateDirectory(projectDirPath);

                Init(projectDir);

                return 0;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static bool CheckProjectName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var invalid = Path.GetInvalidFileNameChars();
            if (name.Any(c => invalid.Contains(c)))
            {
                return false;
            }

            return true;
        }

        private static void Init(DirectoryInfo projectDir)
        {
            CreateProjectFile(projectDir);
            CreateStartupFile(projectDir);
            CreateHostingConfigFile(projectDir);
        }

        private static void CreateProjectFile(DirectoryInfo projectDir)
        {
            var filePath = Path.Combine(projectDir.FullName, "project.json");

            // TODO: Determine version from SDK version?
            // TODO: Support passing target frameworks
            var aspnetVersion = "1.0.0-beta8";
            File.WriteAllText(filePath,
$@"{{
  ""webroot"": ""wwwroot"",
  ""version"": ""1.0.0-*"",
  ""dependencies"": {{
    ""Microsoft.AspNet.IISPlatformHandler"": ""{aspnetVersion}"",
    ""Microsoft.AspNet.Server.Kestrel"": ""{aspnetVersion}""
  }},
  ""frameworks"": {{
    ""dnxcore50"": {{ }}
  }},

  ""exclude"": [
    ""wwwroot"",
    ""node_modules""
  ],
  ""publishExclude"": [
    ""**.user"",
    ""**.vspscc""
  ]
}}");
        }

        private static void CreateStartupFile(DirectoryInfo projectDir)
        {
            var filePath = Path.Combine(projectDir.FullName, "Startup.cs");

            File.WriteAllText(filePath,
$@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

namespace {projectDir.Name}
{{
    public class Startup
    {{
        public void ConfigureServices(IServiceCollection services)
        {{

        }}

        public void Configure(IApplicationBuilder app)
        {{
            app.UseIISPlatformHandler();

            app.Run(async (context) =>
            {{
                await context.Response.WriteAsync(""Hello World!"");
            }});
        }}

        public static int Main(string[] args)
        {{
            return Microsoft.AspNet.Hosting.Program.Main(args);
        }}
    }}
}}");
        }

        private static void CreateHostingConfigFile(DirectoryInfo projectDir)
        {
            var filePath = Path.Combine(projectDir.FullName, "Microsoft.AspNet.Hosting.json");
            
            File.WriteAllText(filePath,
$@"{{
  ""server"": ""Microsoft.AspNet.Server.Kestrel"",
  ""port"": 5001
}}");
        }
    }
}
