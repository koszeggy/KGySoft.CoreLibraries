#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: WaitHandleExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#if !NET35 && !NET40

#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using KGySoft.Annotations;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="WaitHandle"/> type.
    /// </summary>
    public static class WaitHandleExtensions
    {
        #region Methods

        /// <summary>
        /// Waits for a signal asynchronously on the provided <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">The handle to wait on.</param>
        /// <param name="timeout">The timeout in milliseconds. This parameter is optional.
        /// <br/>Default value: <see cref="Timeout.Infinite">Timeout.Infinite</see></param>
        /// <param name="cancellationToken">A token for cancellation. This parameter is optional.
        /// <br/>Default value: <see cref="CancellationToken.None">CancellationToken.None</see></param>
        public static async Task<bool> WaitOneAsync([NotNull] this WaitHandle handle, int timeout = Timeout.Infinite, CancellationToken cancellationToken = default)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle), Res.ArgumentNull);
            if (cancellationToken.IsCancellationRequested)
                return false;
            RegisteredWaitHandle registeredHandle = null;
            var tokenRegistration = default(CancellationTokenRegistration);
            try
            {
                var completionSource = new TaskCompletionSource<bool>();
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut), completionSource, timeout, true);
                tokenRegistration = cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetResult(false), completionSource);
                return await completionSource.Task;
            }
            finally
            {
                registeredHandle?.Unregister(null);
                tokenRegistration.Dispose();
            }
        }

        /// <summary>
        /// Waits for a signal asynchronously on the provided <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">The handle to wait on.</param>
        /// <param name="cancellationToken">A token for cancellation. This parameter is optional.
        /// <br/>Default value: <see cref="CancellationToken.None">CancellationToken.None</see></param>
        public static Task<bool> WaitOneAsync(this WaitHandle handle, CancellationToken cancellationToken = default)
            => WaitOneAsync(handle, Timeout.Infinite, cancellationToken);

        #endregion
    }
}
#endif