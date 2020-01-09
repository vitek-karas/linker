using Mono.Cecil;
using System.Collections.Generic;

namespace Mono.Linker.Analysis
{
	class CallgraphDependencyRecorder : IDependencyRecorder
	{
		public List<(MethodDefinition source, MethodDefinition target)> Dependencies { get; } = 
			new List<(MethodDefinition source, MethodDefinition target)> ();

		public HashSet<(MethodDefinition source, MethodDefinition target)> DirectCalls { get; } =
			new HashSet<(MethodDefinition source, MethodDefinition target)> ();
		public HashSet<(MethodDefinition source, MethodDefinition target)> VirtualCalls { get; } =
			new HashSet<(MethodDefinition source, MethodDefinition target)> ();
		public HashSet<(MethodDefinition source, MethodDefinition target)> Overrides { get; } =
			new HashSet<(MethodDefinition source, MethodDefinition target)> ();

		public void RecordDependency (object source, object target, bool marked)
		{
			if ((source is MethodDefinition sourceMD) && (target is MethodDefinition targetMD)) {
				Dependencies.Add ((sourceMD, targetMD));
			}
		}

		public void RecordDirectCall (MethodDefinition source, MethodDefinition target)
		{
			if (source == null || target == null) 
				return;
			DirectCalls.Add ((source, target));
		}

		public void RecordVirtualCall (MethodDefinition source, MethodDefinition target)
		{
			if (source == null || target == null) 
				return;
			VirtualCalls.Add  ((source, target));
		}

		public void RecordOverride (MethodDefinition source, MethodDefinition target)
		{
			if (source == null || target == null) 
				return;
			Overrides.Add ((source, target));
		}

	}
}
