using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
	public class BuildTarget
	{
		public string Name { get; }
		public IEnumerable<string> Dependencies { get; }
		public Func<BuildContext, BuildTargetResult> Body { get; }
		
		public BuildTarget(string name, IEnumerable<string> dependencies, Func<BuildContext, BuildTargetResult> body)
		{
			Name = name;
			Dependencies = dependencies;
			Body = body;
		}
	}
}