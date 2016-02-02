namespace Microsoft.DotNet.Cli.Build.Framework
{
	public class BuildModel
	{
		public IEnumerable<BuildTarget> Targets { get; }
	
		public BuildModel(IEnumerable<BuildTarget> targets)
		{
			Targets = targets;
		}
		
		public static BuildModel Create<T>()
		{
			// Scan the assembly for targets
			var targets = new List<BuildTarget>();
			foreach(var type in typeof(T).Assembly.GetExportedTypes())
			{
				targets.AddRange(CollectTargets(type));
			}
			return new BuildModel(targets);
		}
		
		private static IEnumerable<BuildTarget> CollectTargets(Type typ)
		{
		}
	}
}