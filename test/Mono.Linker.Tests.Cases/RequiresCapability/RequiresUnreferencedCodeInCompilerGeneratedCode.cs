// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresUnreferencedCodeInCompilerGeneratedCode
	{
		public static void Main()
		{
			TestSuppressInIterator ();
		}

		[RequiresUnreferencedCode("t")]
		static IEnumerable<int> TestSuppressInIterator ()
		{
			yield return 0;
			RequiresUnreferencedCodeMethod ();
			yield return 1;
		}

		[RequiresUnreferencedCode ("t")]
		static void RequiresUnreferencedCodeMethod ()
		{
		}
	}
}
