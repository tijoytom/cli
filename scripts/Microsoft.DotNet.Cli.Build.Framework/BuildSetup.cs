using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildSetup
    {
        private Dictionary<string, BuildTarget> _targets = new Dictionary<string, BuildTarget>();

        public IList<TargetOverride> _overrides = new List<TargetOverride>();

        public static BuildSetup Create()
        {
            return new BuildSetup();
        }

        public BuildSetup UseTargets(IEnumerable<BuildTarget> targets)
        {
            foreach (var target in targets)
            {
                BuildTarget previousTarget;
                if (_targets.TryGetValue(target.Name, out previousTarget))
                {
                    _overrides.Add(new TargetOverride(target.Name, previousTarget.Source, target.Source));
                }
                _targets[target.Name] = target;
            }
            return this;
        }

        public BuildSetup UseAllTargetsFromAssembly<T>()
        {
            var asm = typeof(T).GetTypeInfo().Assembly;
            return UseTargets(asm.GetExportedTypes().SelectMany(t => CollectTargets(t)));
        }

        public BuildSetup UseTargetsFrom<T>()
        {
            return UseTargets(CollectTargets(typeof(T)));
        }

        public int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            if (_overrides.Any())
            {
                foreach (var targetOverride in _overrides)
                {
                    Reporter.Verbose.WriteLine($"Target {targetOverride.Name} from {targetOverride.OriginalSource} was overridden in {targetOverride.OverrideSource}");
                }
            }

            var context = new BuildContext(_targets);
            try
            {
                context.RunTarget(BuildContext.DefaultTarget);
            }
            catch (Exception ex)
            {
                Reporter.Error.WriteLine(ex.ToString().Red());
                return 1;
            }
            return 0;
        }

        private static IEnumerable<BuildTarget> CollectTargets(Type typ)
        {
            return from m in typ.GetMethods()
                   let attr = m.GetCustomAttribute<TargetAttribute>()
                   where attr != null
                   select CreateTarget(m, attr);
        }

        private static BuildTarget CreateTarget(MethodInfo m, TargetAttribute attr)
        {
            return new BuildTarget(
                attr.Name ?? m.Name,
                $"{m.DeclaringType.FullName}.{m.Name}",
                attr.Dependencies,
                (Func<BuildTargetContext, BuildTargetResult>)m.CreateDelegate(typeof(Func<BuildTargetContext, BuildTargetResult>)));
        }

        private string GenerateSourceString(string file, int? line, string member)
        {
            if (!string.IsNullOrEmpty(file) && line != null)
            {
                return $"{file}:{line}";
            }
            else if (!string.IsNullOrEmpty(member))
            {
                return member;
            }
            return string.Empty;
        }

        public class TargetOverride
        {
            public string Name { get; }
            public string OriginalSource { get; }
            public string OverrideSource { get; }

            public TargetOverride(string name, string originalSource, string overrideSource)
            {
                Name = name;
                OriginalSource = originalSource;
                OverrideSource = overrideSource;
            }
        }
    }
}