using Mono.Cecil;
using System.Collections.Generic;

namespace Mono.Linker.Analysis
{
	class AnalysisReflectionPatternRecorder : IReflectionPatternRecorder
	{
		public List<MethodDefinition> UnanalyzedMethods { get; private set; } = new List<MethodDefinition> ();
		public Dictionary<MethodDefinition, HashSet<MethodDefinition>> ResolvedReflectionCalls { get; private set; } =
			new Dictionary<MethodDefinition, HashSet<MethodDefinition>> ();

		public AnalysisReflectionPatternRecorder()
		{
		}

		public void RecognizedReflectionAccessPattern (MethodDefinition sourceMethod, MethodDefinition reflectionMethod, IMemberDefinition accessedItem)
		{
			AddResolvedReflectionCall (sourceMethod, reflectionMethod);
		}

		public void UnrecognizedReflectionAccessPattern (MethodDefinition sourceMethod, MethodDefinition refletionMethod, string message)
		{
			UnanalyzedMethods.Add (sourceMethod);
		}

		private void AddResolvedReflectionCall(MethodDefinition caller, MethodDefinition callee)
		{
			if (!ResolvedReflectionCalls.TryGetValue(callee, out HashSet<MethodDefinition> callers)) {
				callers = new HashSet<MethodDefinition> ();
				ResolvedReflectionCalls.Add (callee, callers);
			}

			callers.Add (caller);
		}
	}
}
