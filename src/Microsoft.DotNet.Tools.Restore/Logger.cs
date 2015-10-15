using System;
using NuGet.Logging;

namespace Microsoft.DotNet.Tools.Restore
{
    internal class Logger : ILogger
    {
        public void LogDebug(string data)
        {
            Console.WriteLine($"debug: {data}");
        }

        public void LogError(string data)
        {
            Console.WriteLine($"error: {data}");
        }

        public void LogInformation(string data)
        {
            Console.WriteLine($"info : {data}");
        }

        public void LogVerbose(string data)
        {
            Console.WriteLine($"trace: {data}");
        }

        public void LogWarning(string data)
        {
            Console.WriteLine($"warn : {data}");
        }
    }
}