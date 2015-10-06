using System;
using System.Diagnostics;

namespace dotnet
{
    public class Bootstrapper
    {
        public int Start(string[] args)
        {
            if (args.Length == 0) return 0;
            if (ExecuteCommand(args)) return 0;
            Console.WriteLine("donet: '{0}' is not a dotnet command. See 'dotnet help'.", args[0]);
            return 1;
        }

        private static bool ExecuteCommand(string[] args)
        {
            var commandName = args[0];
            var processName = "dotnet" + "-" + commandName;
            var commandArguments = new string[args.Length - 1];
            Array.Copy(args, 1, commandArguments, 0, commandArguments.Length);

            var processInfo = new ProcessStartInfo
            {
                FileName = processName,
                Arguments = string.Join(" ", commandArguments),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            return ProcessStart(processInfo);
        }

        private static bool ProcessStart(ProcessStartInfo processSettings)
        {
            try
            {
                using (var process = Process.Start(processSettings))
                {
                    if (process == null)
                    {
                        Console.WriteLine("Process {0} could not be started.", processSettings.FileName);
                        return false;
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        Console.WriteLine(output);
                    }
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.WriteLine(error);
                    }

                    process.WaitForExit();
                    var exitCode = process.ExitCode;
                    if (exitCode != 0)
                    {
                        Console.WriteLine("Process exit code {0}.", exitCode);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
    }
}
