using Microsoft.DotNet.Cli.Build.Framework;
using System.IO;
using System.Linq;
using System;

namespace Microsoft.DotNet.Cli.Build
{
    internal class DotNetCli
    {
        public string BinPath { get; }

        public DotNetCli(string binPath)
        {
            BinPath = binPath;
        }

        public Command Exec(string command, params string[] args)
        {
            return Command.Create(Path.Combine(BinPath, "dotnet.exe"), Enumerable.Concat(new[] { command }, args))
                .OnErrorLine(e =>
                {
                    Reprettify(Reporter.Error, e);
                })
                .OnOutputLine(e =>
                {
                    Reprettify(Reporter.Output, e);
                });
        }

        public Command Restore(params string[] args) => Exec("restore", args);
        public Command Build(params string[] args) => Exec("build", args);
        public Command Test(params string[] args) => Exec("test", args);

        private void Reprettify(Reporter output, string str)
        {
            var newVal = str;
            if(str.StartsWith("info"))
            {
                newVal = "info".Green() + str.Substring(4);
            }
            else if(str.StartsWith("warn"))
            {
                newVal = "warn".Yellow() + str.Substring(4);
            }
            else if(str.StartsWith("error"))
            {
                newVal = "error".Red() + str.Substring(5);
            }

            output.WriteLine(newVal);
        }

    }
}