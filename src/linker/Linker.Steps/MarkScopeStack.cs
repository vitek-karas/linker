// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class MarkScopeStack
	{
		readonly Stack<Scope> _scopeStack;

		public readonly struct Scope
		{
			public readonly MessageOrigin Origin;
			public readonly IMemberDefinition UserCodeLocation;

			public Scope(in MessageOrigin origin, IMemberDefinition userCodeLocation)
			{
				Origin = origin;
				UserCodeLocation = userCodeLocation;
			}
		}

		readonly struct LocalScope : IDisposable
		{
			readonly MessageOrigin _origin;
			readonly MarkScopeStack _scopeStack;

			public LocalScope(in MessageOrigin origin, MarkScopeStack scopeStack)
			{
				_origin = origin;
				_scopeStack = scopeStack;

				_scopeStack.Push (new Scope(origin, origin.MemberDefinition));
			}

			public LocalScope(in Scope scope, MarkScopeStack scopeStack)
			{
				_origin = scope.Origin;
				_scopeStack = scopeStack;

				_scopeStack.Push (scope);
			}

			public void Dispose()
			{
				Scope scope = _scopeStack.Pop ();

				if (_origin.MemberDefinition != scope.Origin.MemberDefinition)
					throw new InternalErrorException ($"Scope stack imbalance - expected to pop '{_origin}' but instead popped '{scope.Origin}'.");
			}
		}

		readonly struct ParentScope : IDisposable
		{
			readonly Scope _parentScope;
			readonly Scope _childScope;
			readonly MarkScopeStack _scopeStack;

			public ParentScope(MarkScopeStack scopeStack)
			{
				_scopeStack = scopeStack;
				_childScope = _scopeStack.Pop ();
				_parentScope = _scopeStack.CurrentScope;
			}

			public void Dispose()
			{
				if (_parentScope.Origin.MemberDefinition != _scopeStack.CurrentScope.Origin.MemberDefinition)
					throw new InternalErrorException ($"Scope stack imbalance - expected top of stack to be '{_parentScope.Origin}' but instead found '{_scopeStack.CurrentScope.Origin}'.");

				_scopeStack.Push (_childScope);
			}
		}

		public MarkScopeStack ()
		{
			_scopeStack = new Stack<Scope> ();
		}

		public IDisposable PushScope (in MessageOrigin origin)
		{
			return new LocalScope (origin, this);
		}

		public IDisposable PushScope (in Scope scope)
		{
			return new LocalScope (scope, this);
		}

		public IDisposable PopToParent ()
		{
			return new ParentScope (this);
		}

		public Scope CurrentScope {
			get {
				if (!_scopeStack.TryPeek (out var result))
					throw new InternalErrorException ($"Scope stack imbalance - expected scope but instead the stack is empty.");

				return result;
			}
		}

		public void UpdateCurrentScopeInstructionOffset (int offset)
		{
			if (CurrentScope.Origin.MemberDefinition is not MethodDefinition)
				throw new InternalErrorException ($"Trying to update instruction offset of scope stack which is not a method. Current stack scope is '{CurrentScope}'.");

			var scope = _scopeStack.Pop ();
			_scopeStack.Push (new Scope(new MessageOrigin(scope.Origin.MemberDefinition, offset), scope.UserCodeLocation));
		}

		void Push (in Scope scope)
		{
			_scopeStack.Push (scope);
		}

		Scope Pop ()
		{
			if (!_scopeStack.TryPop (out var result))
				throw new InternalErrorException ($"Scope stack imbalance - trying to pop empty stack.");

			return result;
		}
	}
}
