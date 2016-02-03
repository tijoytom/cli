﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class Constants
    {
        //public static readonly string ProjectFileName = "project.json";
        //public static readonly string ExeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        // Priority order of runnable suffixes to look for and run
        public static readonly string[] RunnableSuffixes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                         ? new string[] { ".exe", ".cmd", ".bat" }
                                                         : new string[] { string.Empty };

        //public static readonly string DefaultConfiguration = "Debug";
        //public static readonly string BinDirectoryName = "bin";
        //public static readonly string ObjDirectoryName = "obj";

        //public static readonly string DynamicLibSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll"   : 
        //                                                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX)     ? ".dylib" : ".so";

        //public static readonly string LibCoreClrName = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "coreclr" : "libcoreclr") + DynamicLibSuffix;

        //public static readonly string RuntimeIdentifier = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win7-x64" :
        //                                                  RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx.10.10-x64" : "ubuntu.14.04-x64";

        //public static readonly string StaticLibSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".lib" : ".a" ;

        //public static readonly string ResponseFileSuffix = ".rsp";

        //public static readonly string HostExecutableName = "corehost" + ExeSuffix;
        //public static readonly string[] HostBinaryNames = new string[] {
        //    HostExecutableName,
        //    (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "hostpolicy" : "libhostpolicy") + DynamicLibSuffix 
        //};
    }
}
