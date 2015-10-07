using System;
using System.Diagnostics;

namespace dotnet_lib
{
    public class ProcessStarter : IProcessStarter
    {
        public int Start(ProcessStartInfo startInfo)
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new NullReferenceException("Process " + startInfo.FileName + " could not be started.");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    Console.Write(output);
                }
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.Write(error);
                }

                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}
