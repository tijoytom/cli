using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildContext
    {
        private IDictionary<string, BuildTargetResult> _completedTargets = new Dictionary<string, BuildTargetResult>(StringComparer.OrdinalIgnoreCase);

        public static readonly string DefaultTarget = "Default";

        private int _maxTargetLen;

        public IDictionary<string, BuildTarget> Targets { get; }

        public BuildContext(IDictionary<string, BuildTarget> targets)
        {
            Targets = targets;
            _maxTargetLen = targets.Values.Select(t => t.Name.Length).Max();
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
                return target.Body(new BuildTargetContext(this, target, dependencyResults));
            }
            else
            {
                return new BuildTargetResult(target, success: true);
            }
        }
    }
}
