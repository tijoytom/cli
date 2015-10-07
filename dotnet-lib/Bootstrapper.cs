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
            var commandPath = FindCommandPath(processName);

            if (!string.IsNullOrEmpty(commandPath))
            {
                return IsCommandAScript(commandPath) ? ExecuteScript(args, commandPath) : ExecuteCommand(args, commandPath);
            }

            Console.WriteLine("donet: '{0}' is not a dotnet command. See 'dotnet help'.", args[0]);
            return 1;
        }

        private static string FindCommandPath(string processName)
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

        private static bool IsCommandAScript(string commandPath)
        {
            var firstLine = File.ReadLines(commandPath).FirstOrDefault();
            return firstLine != null && firstLine.StartsWith("#!");
        }

        private int ExecuteScript(string[] args, string commandPath)
        {
            var commandArguments = new string[args.Length - 1];
            Array.Copy(args, 1, commandArguments, 0, commandArguments.Length);

            var tokens = File.ReadLines(commandPath).First().Split(' ');
            var scriptRunner = tokens[0].Replace("#!", "");

            var scriptArguments = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, scriptArguments, 0, scriptArguments.Length);

            var arguments = string.Join(" ", scriptArguments.Select(s => $"\"{s}\""));
            arguments += " " + Path.GetFileNameWithoutExtension(commandPath) + " ";
            arguments += string.Join(" ", commandArguments.Select(s => $"\"{s}\""));

            var processInfo = new ProcessStartInfo
            {
                FileName = scriptRunner,
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            return _processStarter.Start(processInfo);
        }

        private int ExecuteCommand(string[] args, string commandPath)
        {
            var commandArguments = new string[args.Length - 1];
            Array.Copy(args, 1, commandArguments, 0, commandArguments.Length);

            var processInfo = new ProcessStartInfo
            {
                FileName = Path.GetFileNameWithoutExtension(commandPath),
                Arguments = string.Join(" ", commandArguments.Select(s => $"\"{s}\"")),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            return _processStarter.Start(processInfo);
        }
    }
}
