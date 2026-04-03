#if !NET9_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Lock.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Suppressions

#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement - false alarm, being the compatible solution for old frameworks, it is very intentional here

#endregion

// ReSharper disable once CheckNamespace
namespace System.Threading
{
    /// <summary>
    /// Provides the functionality of the System.Threading.Lock class of .NET 9+ for earlier frameworks.
    /// </summary>
    internal sealed class Lock
    {
        #region Nested Types

        // NOTE: Even though it's inside an internal class, it must be public to avoid CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
        public readonly ref struct Scope : IDisposable
        {
            #region Fields

            private readonly Lock owner;

            #endregion

            #region Constructors

            internal Scope(Lock owner) => this.owner = owner;

            #endregion

            #region Methods
            
            public void Dispose() => owner.Exit();

            #endregion
        }

        #endregion

        #region Methods

        #region Public Methods

        // NOTE: Must be public to avoid CS0656: Missing compiler required member 'System.Threading.Lock.EnterScope'
        public Scope EnterScope()
        {
            Enter();
            return new Scope(this);
        }

        #endregion

        #region Internal Methods
        
        internal void Enter() => Monitor.Enter(this);
        internal void Exit() => Monitor.Exit(this);

        #endregion

        #endregion
    }
}
#endif