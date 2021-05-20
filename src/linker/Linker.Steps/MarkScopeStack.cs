// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	class MarkScopeStack
	{
		readonly Stack<MessageOrigin> _scopeStack;

		readonly struct LocalScope : IDisposable
		{
			readonly MessageOrigin _origin;
			readonly MarkScopeStack _scopeStack;

			public LocalScope(in MessageOrigin origin, MarkScopeStack scopeStack)
			{
				_origin = origin;
				_scopeStack = scopeStack;
				_scopeStack.Push (origin);
			}

			public void Dispose()
			{
				MessageOrigin childOrigin = _scopeStack.Pop ();

				if (_origin.MemberDefinition != childOrigin.MemberDefinition)
					throw new InternalErrorException ($"Scope stack imbalance - expected to pop '{_origin}' but instead popped '{childOrigin}'.");
			}
		}

		readonly struct ParentScope : IDisposable
		{
			readonly MessageOrigin _parentOrigin;
			readonly MessageOrigin _childOrigin;
			readonly MarkScopeStack _scopeStack;

			public ParentScope(MarkScopeStack scopeStack)
			{
				_scopeStack = scopeStack;
				_childOrigin = _scopeStack.Pop ();
				_parentOrigin = _scopeStack.CurrentScope;
			}

			public void Dispose()
			{
				if (_parentOrigin.MemberDefinition != _scopeStack.CurrentScope.MemberDefinition)
					throw new InternalErrorException ($"Scope stack imbalance - expected top of stack to be '{_parentOrigin}' but instead found '{_scopeStack.CurrentScope}'.");

				_scopeStack.Push (_childOrigin);
			}
		}

		public MarkScopeStack ()
		{
			_scopeStack = new Stack<MessageOrigin> ();
		}

		public IDisposable PushScope (in MessageOrigin origin)
		{
			return new LocalScope (origin, this);
		}

		public IDisposable PopToParent ()
		{
			return new ParentScope (this);
		}

		public MessageOrigin CurrentScope {
			get {
				if (!_scopeStack.TryPeek (out var result))
					throw new InternalErrorException ($"Scope stack imbalance - expected scope but instead the stack is empty.");

				return result;
			}
		}

		public void UpdateCurrentScopeInstructionOffset (int offset)
		{
			if (CurrentScope.MemberDefinition is not MethodDefinition)
				throw new InternalErrorException ($"Trying to update instruction offset of scope stack which is not a method. Current stack scope is '{CurrentScope}'.");

			var origin = _scopeStack.Pop ();
			_scopeStack.Push (new MessageOrigin(origin.MemberDefinition, offset));
		}

		void Push (in MessageOrigin origin)
		{
			_scopeStack.Push (origin);
		}

		MessageOrigin Pop ()
		{
			if (!_scopeStack.TryPop (out var result))
				throw new InternalErrorException ($"Scope stack imbalance - trying to pop empty stack.");

			return result;
		}
	}
}
