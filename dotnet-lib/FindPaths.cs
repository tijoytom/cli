using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dotnet_lib
{
    public static class FindPaths
    {
        public static List<string> GetPathDirectories()
        {
            var pathValue = Environment.GetEnvironmentVariable("PATH");
            return pathValue.Split(Path.PathSeparator).ToList();
        }
    }
}
