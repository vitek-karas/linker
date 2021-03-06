﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresAssemblyFilesAnalyzer : RequiresAnalyzerBase
	{
		public const string IL3000 = nameof (IL3000);
		public const string IL3001 = nameof (IL3001);
		public const string IL3002 = nameof (IL3002);
		public const string IL3003 = nameof (IL3003);

		private const string RequiresAssemblyFilesAttribute = nameof (RequiresAssemblyFilesAttribute);
		public const string RequiresAssemblyFilesAttributeFullyQualifiedName = "System.Diagnostics.CodeAnalysis." + RequiresAssemblyFilesAttribute;

		static readonly DiagnosticDescriptor s_locationRule = new DiagnosticDescriptor (
			IL3000,
			new LocalizableResourceString (nameof (SharedStrings.AvoidAssemblyLocationInSingleFileTitle),
				SharedStrings.ResourceManager, typeof (SharedStrings)),
			new LocalizableResourceString (nameof (SharedStrings.AvoidAssemblyLocationInSingleFileMessage),
				SharedStrings.ResourceManager, typeof (SharedStrings)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3000");

		static readonly DiagnosticDescriptor s_getFilesRule = new DiagnosticDescriptor (
			IL3001,
			new LocalizableResourceString (nameof (SharedStrings.AvoidAssemblyGetFilesInSingleFileTitle),
				SharedStrings.ResourceManager, typeof (SharedStrings)),
			new LocalizableResourceString (nameof (SharedStrings.AvoidAssemblyGetFilesInSingleFileMessage),
				SharedStrings.ResourceManager, typeof (SharedStrings)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3001");

		static readonly DiagnosticDescriptor s_requiresAssemblyFilesRule = new DiagnosticDescriptor (
			IL3002,
			new LocalizableResourceString (nameof (SharedStrings.RequiresAssemblyFilesTitle),
				SharedStrings.ResourceManager, typeof (SharedStrings)),
			new LocalizableResourceString (nameof (SharedStrings.RequiresAssemblyFilesMessage),
				SharedStrings.ResourceManager, typeof (SharedStrings)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3002");

		static readonly DiagnosticDescriptor s_requiresAttributeMismatch = new DiagnosticDescriptor (
			IL3003,
			new LocalizableResourceString (nameof (SharedStrings.RequiresAttributeMismatchTitle),
			SharedStrings.ResourceManager, typeof (SharedStrings)),
			new LocalizableResourceString (nameof (SharedStrings.RequiresAttributeMismatchMessage),
			SharedStrings.ResourceManager, typeof (SharedStrings)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_locationRule, s_getFilesRule, s_requiresAssemblyFilesRule, s_requiresAttributeMismatch);

		private protected override string RequiresAttributeName => RequiresAssemblyFilesAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => RequiresAssemblyFilesAttributeFullyQualifiedName;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Property | DiagnosticTargets.Event;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresAssemblyFilesRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresAttributeMismatch;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation)
		{
			var isSingleFileAnalyzerEnabled = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer, compilation);
			if (!string.Equals (isSingleFileAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;
			var includesAllContent = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation);
			if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;
			return true;
		}

		protected override ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (Compilation compilation)
		{
			var dangerousPatternsBuilder = ImmutableArray.CreateBuilder<ISymbol> ();

			var assemblyType = compilation.GetTypeByMetadataName ("System.Reflection.Assembly");
			if (assemblyType != null) {
				// properties
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyType.GetMembers ("Location")));

				// methods
				dangerousPatternsBuilder.AddRange (assemblyType.GetMembers ("GetFile").OfType<IMethodSymbol> ());
				dangerousPatternsBuilder.AddRange (assemblyType.GetMembers ("GetFiles").OfType<IMethodSymbol> ());
			}

			var assemblyNameType = compilation.GetTypeByMetadataName ("System.Reflection.AssemblyName");
			if (assemblyNameType != null) {
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("CodeBase")));
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("EscapedCodeBase")));
			}

			return dangerousPatternsBuilder.ToImmutable ();
		}

		protected override bool ReportSpecialIncompatibleMembersDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> dangerousPatterns, ISymbol member)
		{
			if (member is IMethodSymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
				operationContext.ReportDiagnostic (Diagnostic.Create (s_getFilesRule, operationContext.Operation.Syntax.GetLocation (), member.GetDisplayName ()));
				return true;
			} else if (member is IPropertySymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
				operationContext.ReportDiagnostic (Diagnostic.Create (s_locationRule, operationContext.Operation.Syntax.GetLocation (), member.GetDisplayName ()));
				return true;
			}
			return false;
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) => attribute.ConstructorArguments.Length == 0;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = requiresAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Message").Value.Value?.ToString ();
			if (!string.IsNullOrEmpty (message))
				message = $" {message}{(message!.TrimEnd ().EndsWith (".") ? "" : ".")}";

			return message!;
		}
	}
}
