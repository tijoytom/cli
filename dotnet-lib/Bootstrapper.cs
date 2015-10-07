using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace dotnet_lib
{
    public class Bootstrapper : IBootstrapper
    {
        private readonly IProcessStarter _processStarter;

        public Bootstrapper(IProcessStarter processStarter)
        {
            _processStarter = processStarter;
        }

        public int Start(string[] args)
        {
            if (args.Length == 0) return 0;
            var processName = "dotnet-" + args[0];
            var commandPath = FindCommand(processName);

            if (!string.IsNullOrEmpty(commandPath))
            {
                return ExecuteCommand(args, commandPath);
            }

            Console.WriteLine("donet: '{0}' is not a dotnet command. See 'dotnet help'.", args[0]);
            return 1;
        }

        private static string FindCommand(string processName)
        {
            var paths = FindPaths.GetPathDirectories();
            paths.Add(Directory.GetCurrentDirectory());

            foreach (var path in paths.Where(Directory.Exists))
            {
                foreach (var file in Directory.EnumerateFiles(path)
                    .Where(file => Path.GetFileNameWithoutExtension(file).ToLower() == processName))
                {
                    return Path.Combine(path, file);
                }
            }

            return "";
        }

        private int ExecuteCommand(string[] args, string commandPath)
        {
            var isScript = false;
            var commandArguments = new string[args.Length - 1];
            Array.Copy(args, 1, commandArguments, 0, commandArguments.Length);

            var processName = Path.GetFileNameWithoutExtension(commandPath);

            var line = File.ReadLines(commandPath).First();
            var scriptRunner = "";
            var arguments = "";

            if (line.StartsWith("#!"))
            {
                isScript = true;
                var tokens = line.Split(' ');
                scriptRunner = tokens[0].Replace("#!", "");

                var scriptArguments = new string[tokens.Length - 1];
                Array.Copy(tokens, 1, scriptArguments, 0, scriptArguments.Length);
                arguments += string.Join(" ", scriptArguments.Select(s => $"\"{s}\""));
                arguments += processName;
            }

            arguments += string.Join(" ", commandArguments.Select(s => $"\"{s}\""));

            var processInfo = new ProcessStartInfo
            {
                FileName = isScript ? scriptRunner : processName,
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            return _processStarter.Start(processInfo);
        }
    }
}
