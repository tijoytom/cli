using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildContext
    {
        private IDictionary<string, BuildTargetResult> _completedTargets = new Dictionary<string, BuildTargetResult>(StringComparer.OrdinalIgnoreCase);

        public static readonly string DefaultTarget = "Default";

        private int _maxTargetLen;


        public IDictionary<string, BuildTarget> Targets { get; }

        public string Platform { get; }
        public string Uname { get; }
        public IDictionary<string, object> Properties = new Dictionary<string, object>();

        public string BuildDirectory { get; }

        public object this[string name]
        {
            get { return Properties[name]; }
            set { Properties[name] = value; }
        }

        public BuildContext(IDictionary<string, BuildTarget> targets, string buildDirectory)
        {
            Targets = targets;
            BuildDirectory = buildDirectory;
            _maxTargetLen = targets.Values.Select(t => t.Name.Length).Max();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Platform = "Windows";
                Uname = "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Platform = "Linux";
                Uname = "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Platform = "OSX";
                Uname = "Darwin";
            }
            else
            {
                Platform = "Unknown";
                Uname = "Unknown";
            }
        }

        public BuildTargetResult RunTarget(string name) => RunTarget(name, force: false);

        public BuildTargetResult RunTarget(string name, bool force)
        {
            BuildTarget target;
            if (!Targets.TryGetValue(name, out target))
            {
                Reporter.Verbose.WriteLine($"Skipping undefined target: {target}");
            }

            // Check if it's been completed
            BuildTargetResult result;
            if (!force && _completedTargets.TryGetValue(name, out result))
            {
                Reporter.Verbose.WriteLine($"Skipping completed target: {target}");
                return result;
            }


            // It hasn't, or we're forcing, so run it
            result = ExecTarget(target);
            _completedTargets[target.Name] = result;
            return result;
        }

        public void Info(string message)
        {
            Reporter.Output.WriteLine("info".Green() + $" : {message}");
        }

        private BuildTargetResult ExecTarget(BuildTarget target)
        {
            // Run the dependencies
            var dependencyResults = new Dictionary<string, BuildTargetResult>();
            foreach (var dependency in target.Dependencies)
            {
                var result = RunTarget(dependency);
                dependencyResults[dependency] = result;
                result.EnsureSuccessful();
            }

            Reporter.Output.WriteLine("TARGET ".Green() + $"{target.Name.PadRight(_maxTargetLen + 2).Yellow()} ({target.Source.White()})");
            if (target.Body != null)
            {
                try
                {
                    return target.Body(new BuildTargetContext(this, target, dependencyResults));
                }
                catch (Exception ex)
                {
                    return new BuildTargetResult(target, success: false, exception: ex);
                }
            }
            else
            {
                return new BuildTargetResult(target, success: true);
            }
        }
    }
}
