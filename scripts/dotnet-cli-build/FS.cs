using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build
{
    public static class FS
    {
        public static void Mkdirp(string dir)
        {
            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void Chmod(string file, string mode)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            Command.Create("chmod", mode, file).Execute().EnsureSuccessful();
        }
    }
}
