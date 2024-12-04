#if NET35
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ManualResetEventSlim.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Diagnostics.CodeAnalysis;

using KGySoft;

#endregion

// ReSharper disable once CheckNamespace
namespace System.Threading
{
    /// <summary>
    /// Represents a lightweight alternative to <see cref="ManualResetEvent"/> for .NET 3.5.
    /// </summary>
    internal sealed class ManualResetEventSlim : IDisposable
    {
        #region Fields

        private readonly object lockObject = new object();

        private bool isDisposed;
        private ManualResetEvent? nativeHandle;

        #endregion

        #region Properties

        internal bool IsSet { get; private set; }

        [SuppressMessage("ReSharper", "SuspiciousLockOverSynchronizationPrimitive", Justification = "This is the implementation itself so cannot call WaitHandle from here.")]
        internal WaitHandle WaitHandle
        {
            get
            {
                if (isDisposed)
                    Throw.ObjectDisposedException();
                if (nativeHandle != null)
                    return nativeHandle;

                bool originalState = IsSet;
                var newHandle = new ManualResetEvent(originalState);
                if (Interlocked.CompareExchange(ref nativeHandle, newHandle, null) != null)
                    newHandle.Close();
                else
                {
                    if (IsSet == originalState)
                        return nativeHandle;

                    Debug.Assert(IsSet, "Only set state is expected here");
                    lock (newHandle)
                    {
                        if (nativeHandle == newHandle)
                            newHandle.Set();
                    }
                }

                return nativeHandle;
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        public void Dispose()
        {
            lock (lockObject)
            {
                if (isDisposed)
                    return;
                isDisposed = true;
                DoSignal();
                Monitor.PulseAll(lockObject);
                nativeHandle?.Close();
                nativeHandle = null;
            }
        }

        #endregion

        #region Internal Methods

        internal void Set()
        {
            lock (lockObject)
            {
                if (isDisposed)
                    return;
                DoSignal();
                Monitor.PulseAll(lockObject);
            }
        }

        internal void Wait()
        {
            lock (lockObject)
            {
                if (isDisposed)
                    return;
                while (!IsSet)
                    Monitor.Wait(lockObject);
            }
        }

        #endregion

        #region Private Methods

        private void DoSignal()
        {
            // this must be called in lock
            IsSet = true;
            nativeHandle?.Set();
        }

        #endregion

        #endregion
    }
}
#endif
