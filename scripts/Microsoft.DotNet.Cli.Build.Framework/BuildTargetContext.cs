using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildTargetContext
    {
        private IDictionary<string, BuildTargetResult> _dependencyResults;

        public BuildContext Context { get; }
        public BuildTarget Target { get; }

        public BuildTargetContext(BuildContext context, BuildTarget target, IDictionary<string, BuildTargetResult> dependencyResults)
        {
            Context = context;
            Target = target;
            _dependencyResults = dependencyResults;
        }

        public BuildTargetResult Success()
        {
            return new BuildTargetResult(Target, success: true);
        }

        public BuildTargetResult Failed() => Failed(errorMessage: string.Empty);

        public BuildTargetResult Failed(string errorMessage)
        {
            return new BuildTargetResult(Target, success: false, errorMessage: errorMessage);
        }
    }
}
