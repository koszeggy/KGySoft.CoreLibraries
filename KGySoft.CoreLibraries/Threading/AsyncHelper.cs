#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AsyncHelper.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

#if !NET35
using System.Threading.Tasks;
#endif

#endregion

namespace KGySoft.Threading
{
    internal static class AsyncHelper
    {
        #region Nested classes

        #region NullContext class

        private sealed class NullContext : IAsyncContext
        {
            #region Properties

            public int MaxDegreeOfParallelism => 0;
            public bool IsCancellationRequested => false;
            public bool CanBeCanceled => false;
            public IAsyncProgress? Progress => null;

            #endregion

            #region Methods

            public void ThrowIfCancellationRequested() { }

            #endregion
        }

        #endregion

        #region TaskContext class
#if !NET35

        private sealed class TaskContext : IAsyncContext
        {
            #region Fields

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            [SuppressMessage("Style", "IDE0044:Add readonly modifier",
                Justification = "CancellationToken is not a readonly struct in every targeted platform so not making it readonly to prevent the creation of a defensive copy.")]
            // ReSharper disable once FieldCanBeMadeReadOnly.Local
#endif
            private CancellationToken token;

            #endregion

            #region Properties

            #region Public Properties

            public int MaxDegreeOfParallelism { get; }
            public bool IsCancellationRequested => token.IsCancellationRequested;
            public bool CanBeCanceled => token.CanBeCanceled;
            public IAsyncProgress? Progress { get; }

            #endregion

            #region Internal Properties

            internal bool ThrowIfCanceled { get; } = true;
            internal object? State { get; }

            #endregion

            #endregion

            #region Constructors

            internal TaskContext(TaskConfig? asyncConfig)
            {
                if (asyncConfig == null)
                    return;
                token = asyncConfig.CancellationToken;
                MaxDegreeOfParallelism = asyncConfig.MaxDegreeOfParallelism;
                Progress = asyncConfig.Progress;
                ThrowIfCanceled = asyncConfig.ThrowIfCanceled;
                State = asyncConfig.State;
            }

            #endregion

            #region Methods

            public void ThrowIfCancellationRequested() => token.ThrowIfCancellationRequested();

            #endregion
        }

#endif
        #endregion

        #region AsyncResultContext class

        private class AsyncResultContext : IAsyncResult, IAsyncContext
        {
            #region Fields

            private volatile bool isCancellationRequested;
            private volatile bool isCompleted;
            private Func<bool>? isCancelRequestedCallback;
            private AsyncCallback? callback;
            private ManualResetEventSlim? waitHandle;
            private volatile Exception? error;

            #endregion

            #region Properties

            #region Public Properties

            public int MaxDegreeOfParallelism { get; }
            public bool IsCancellationRequested => isCancelRequestedCallback != null && (isCancellationRequested || (isCancellationRequested = isCancelRequestedCallback.Invoke()));
            public bool CanBeCanceled => isCancelRequestedCallback != null;
            public IAsyncProgress? Progress { get; }
            public bool IsCompleted => isCompleted;
            public string BeginMethodName { get; }
            public bool IsDisposed { get; private set; }

            public WaitHandle AsyncWaitHandle => InternalWaitHandle.WaitHandle;

            public object? AsyncState { get; }
            public bool CompletedSynchronously { get; internal set; }

            #endregion

            #region Internal Properties

            internal bool ThrowIfCanceled { get; } = true;
            internal Action<IAsyncContext>? Operation { get; private set; }

            #endregion

            #region Private Properties

            private ManualResetEventSlim InternalWaitHandle
            {
                get
                {
                    if (IsDisposed)
                        throw new ObjectDisposedException(PublicResources.ObjectDisposed);
                    if (waitHandle == null)
                    {
                        var newHandle = new ManualResetEventSlim();
                        if (Interlocked.CompareExchange(ref waitHandle, newHandle, null) != null)
                            newHandle.Dispose();
                        else if (isCompleted)
                            waitHandle.Set();
                    }

                    return waitHandle;
                }
            }

            #endregion

            #endregion

            #region Constructors

            internal AsyncResultContext(string beginMethod, Action<IAsyncContext>? operation, AsyncConfig? asyncConfig)
            {
                Operation = operation;
                BeginMethodName = beginMethod;
                if (asyncConfig == null)
                    return;
                MaxDegreeOfParallelism = asyncConfig.MaxDegreeOfParallelism;
                callback = asyncConfig.CompletedCallback;
                AsyncState = asyncConfig.State;
                ThrowIfCanceled = asyncConfig.ThrowIfCanceled;
                isCancelRequestedCallback = asyncConfig.IsCancelRequestedCallback;
                Progress = asyncConfig.Progress;
            }

            #endregion

            #region Methods

            #region Static Methods

            private static void ThrowOperationCanceled() => throw new OperationCanceledException(Res.OperationCanceled);

            #endregion

            #region Instance Methods

            #region Public Methods

            public void ThrowIfCancellationRequested()
            {
                if (IsCancellationRequested)
                    ThrowOperationCanceled();
            }

            public void WaitForCompletion()
            {
                if (!isCompleted)
                    InternalWaitHandle.Wait();
                if (isCancellationRequested && ThrowIfCanceled)
                    ThrowOperationCanceled();
                if (error != null)
                    ExceptionDispatchInfo.Capture(error).Throw();
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }

            #endregion

            #region Internal Methods

            internal void SetCompleted()
            {
                Debug.Assert(!isCompleted);
                isCompleted = true;
                callback?.Invoke(this);
                waitHandle?.Set();
            }

            internal void SetError(Exception e)
            {
                error = e;
                SetCompleted();
            }

            internal void SetCanceled()
            {
                Debug.Assert(isCancellationRequested);
                SetCompleted();
            }

            #endregion

            #region Protected Methods

            protected virtual void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;
                IsDisposed = true;
                Operation = null;
                isCancelRequestedCallback = null;
                callback = null;
                if (waitHandle == null)
                    return;

                if (!waitHandle.IsSet)
                    waitHandle.Set();

                if (disposing)
                    waitHandle.Dispose();
            }

            #endregion

            #endregion

            #endregion
        }

        #endregion

        #region AsyncResultContext<TResult> class

        private sealed class AsyncResultContext<TResult> : AsyncResultContext
            where TResult : class?
        {
            #region Fields

            [AllowNull] private volatile TResult result;

            #endregion

            #region Properties

            #region Public Properties

            public TResult? Result
            {
                get
                {
                    WaitForCompletion();
                    return result;
                }
            }

            #endregion

            #region Internal Properties

            internal new Func<IAsyncContext, TResult>? Operation { get; private set; }

            #endregion

            #endregion

            #region Constructors

            internal AsyncResultContext(string beginMethod, Func<IAsyncContext, TResult>? operation, AsyncConfig? asyncConfig)
                : base(beginMethod, null, asyncConfig)
            {
                Operation = operation;
            }

            #endregion

            #region Methods

            #region Internal Methods

            internal void SetResult(TResult value)
            {
                result = value;
                SetCompleted();
            }

            #endregion

            #region Protected Methods

            protected override void Dispose(bool disposing)
            {
                if (IsDisposed)
                    return;
                Operation = null;
                base.Dispose(disposing);
            }

            #endregion

            #endregion
        }

        #endregion

        #region ManualResetEventSlim class
#if NET35

        private sealed class ManualResetEventSlim : IDisposable
        {
        #region Fields

            private readonly object lockObject = new object();

            private bool isDisposed;
            private ManualResetEvent? nativeHandle;

        #endregion

        #region Properties

            internal bool IsSet { get; private set; }

            internal WaitHandle WaitHandle
            {
                get
                {
                    if (isDisposed)
                        throw new ObjectDisposedException(PublicResources.ObjectDisposed);
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

#endif
        #endregion

        #endregion

        #region Fields

        private static IAsyncContext? nullContext;

        #endregion

        #region Properties

        internal static IAsyncContext Null => nullContext ??= new  NullContext();

        #endregion

        #region Methods


        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when calling the EndOperation method.")]
        internal static IAsyncResult BeginOperation<TProgress>(Action<IAsyncContext> operation, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethod = null!)
        {
            #region Local Methods

            // this method is executed on a pool thread
            static void DoWork(object state)
            {
                var context = (AsyncResultContext)state;
                if (context.IsCancellationRequested)
                {
                    context.SetCanceled();
                    return;
                }

                try
                {
                    context.Operation!.Invoke(context);
                    if (context.IsCancellationRequested)
                        context.SetCanceled();
                    else
                        context.SetCompleted();
                }
                catch (OperationCanceledException)
                {
                    context.SetCanceled();
                }
                catch (Exception e)
                {
                    context.SetError(e);
                }
            }

            #endregion

            var asyncResult = new AsyncResultContext(beginMethod, operation, asyncConfig);
            if (asyncResult.IsCancellationRequested)
            {
                asyncResult.SetCanceled();
                asyncResult.CompletedSynchronously = true;
            }
            else
                ThreadPool.QueueUserWorkItem(DoWork!, asyncResult);
            return asyncResult;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
                Justification = "Pool thread exceptions are not suppressed, they will be thrown when calling the EndOperation method.")]
        internal static IAsyncResult BeginOperation<TResult>(Func<IAsyncContext, TResult> operation, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethod = null!)
            where TResult : class?
        {
            #region Local Methods

            // this method is executed on a pool thread
            static void DoWork(object state)
            {
                var context = (AsyncResultContext<TResult>)state;
                if (context.IsCancellationRequested)
                {
                    context.SetCanceled();
                    return;
                }

                try
                {
                    TResult result = context.Operation!.Invoke(context);
                    if (context.IsCancellationRequested)
                        context.SetCanceled();
                    else
                        // a non-nullable TResult will not be null if the operation was not canceled
                        context.SetResult(result!);
                }
                catch (OperationCanceledException)
                {
                    context.SetCanceled();
                }
                catch (Exception e)
                {
                    context.SetError(e);
                }
            }

            #endregion

            var asyncResult = new AsyncResultContext<TResult>(beginMethod, operation, asyncConfig);
            if (asyncResult.IsCancellationRequested)
            {
                asyncResult.SetCanceled();
                asyncResult.CompletedSynchronously = true;
            }
            else
                ThreadPool.QueueUserWorkItem(DoWork!, asyncResult);
            return asyncResult;
        }

        internal static IAsyncResult FromCompleted(AsyncConfig? asyncConfig, [CallerMemberName]string beginMethod = null!)
        {
            var asyncResult = new AsyncResultContext(beginMethod, null, asyncConfig);
            asyncResult.SetCompleted();
            asyncResult.CompletedSynchronously = true;
            return asyncResult;
        }

        internal static IAsyncResult FromResult<TResult>(TResult result, TResult canceledValue, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethod = null!)
            where TResult : class
        {
            var asyncResult = new AsyncResultContext<TResult>(beginMethod, null, asyncConfig);
            if (asyncResult.IsCancellationRequested)
            {
                if (asyncResult.ThrowIfCanceled)
                    asyncResult.SetCanceled();
                else
                    asyncResult.SetResult(canceledValue);
            }
            else
                asyncResult.SetResult(result);

            asyncResult.CompletedSynchronously = true;
            return asyncResult;
        }

        internal static void EndOperation(IAsyncResult asyncResult, string beginMethodName)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult), PublicResources.ArgumentNull);
            if (asyncResult is not AsyncResultContext result || result.GetType() != typeof(AsyncResultContext) || result.BeginMethodName != beginMethodName || result.IsDisposed)
                throw new InvalidOperationException(Res.InvalidAsyncResult(beginMethodName));
            try
            {
                result.WaitForCompletion();
            }
            finally
            {
                result.Dispose();
            }
        }

        internal static TResult? EndOperation<TResult>(IAsyncResult asyncResult, string beginMethodName)
            where TResult : class
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult), PublicResources.ArgumentNull);
            if (asyncResult is not AsyncResultContext<TResult> result || result.BeginMethodName != beginMethodName || result.IsDisposed)
                throw new InvalidOperationException(Res.InvalidAsyncResult(beginMethodName));
            try
            {
                return result.Result;
            }
            finally
            {
                result.Dispose();
            }
        }

#if !NET35
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when task is awaited or Result is accessed.")]
        internal static Task<TResult?> DoOperationAsync<TResult>(Func<IAsyncContext, TResult?> operation, TaskConfig? asyncConfig)
        {
            #region Local Methods

            // this method is executed on a pool thread
            static void DoWork(object state)
            {
                var (context, completion, func) = ((TaskContext, TaskCompletionSource<TResult?>, Func<IAsyncContext, TResult>))state;
                try
                {
                    TResult result = func.Invoke(context);
                    if (context.IsCancellationRequested)
                    {
                        if (context.ThrowIfCanceled)
                            completion.SetCanceled();
                        else
                            completion.SetResult(default);
                    }
                    else
                        completion.SetResult(result);
                }
                catch (OperationCanceledException)
                {
                    if (context.ThrowIfCanceled)
                        completion.SetCanceled();
                    else
                        completion.SetResult(default);
                }
                catch (Exception e)
                {
                    completion.SetException(e);
                }
            }

            #endregion

            var taskContext = new TaskContext(asyncConfig);
            var completionSource = new TaskCompletionSource<TResult?>(taskContext.State);
            if (taskContext.IsCancellationRequested)
            {
                if (taskContext.ThrowIfCanceled)
                    completionSource.SetCanceled();
                else
                    completionSource.SetResult(default);
            }
            else
                ThreadPool.QueueUserWorkItem(DoWork!, (taskContext, completionSource, operation));

            return completionSource.Task;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when task is awaited or Result is accessed.")]
        internal static Task DoOperationAsync(Action<IAsyncContext> operation, TaskConfig? asyncConfig)
        {
            #region Local Methods

            // this method is executed on a pool thread
            static void DoWork(object state)
            {
                var (context, completion, op) = ((TaskContext, TaskCompletionSource<object?>, Action<IAsyncContext>))state;
                try
                {
                    op.Invoke(context);
                    if (context.IsCancellationRequested && context.ThrowIfCanceled)
                        completion.SetCanceled();
                    else
                        completion.SetResult(default);
                }
                catch (OperationCanceledException)
                {
                    if (context.ThrowIfCanceled)
                        completion.SetCanceled();
                    else
                        completion.SetResult(default);
                }
                catch (Exception e)
                {
                    completion.SetException(e);
                }
            }

            #endregion

            var taskContext = new TaskContext(asyncConfig);
            var completionSource = new TaskCompletionSource<object?>(taskContext.State);
            if (taskContext.IsCancellationRequested)
            {
                if (taskContext.ThrowIfCanceled)
                    completionSource.SetCanceled();
                else
                    completionSource.SetResult(default);
            }
            else
                ThreadPool.QueueUserWorkItem(DoWork!, (taskContext, completionSource, operation));

            return completionSource.Task;
        }

        internal static Task FromCompleted(TaskConfig? asyncConfig)
        {
            var taskContext = new TaskContext(asyncConfig);
            var completionSource = new TaskCompletionSource<_>(taskContext.State);
            if (taskContext.IsCancellationRequested && taskContext.ThrowIfCanceled)
                completionSource.SetCanceled();
            else
                completionSource.SetResult(default);

            return completionSource.Task;
        }

        internal static Task<TResult> FromResult<TResult>(TResult result, TResult canceledValue, TaskConfig? asyncConfig)
        {
            var taskContext = new TaskContext(asyncConfig);
            var completionSource = new TaskCompletionSource<TResult>(taskContext.State);
            if (taskContext.IsCancellationRequested)
            {
                if (taskContext.ThrowIfCanceled)
                    completionSource.SetCanceled();
                else
                    completionSource.SetResult(canceledValue);
            }
            else
                completionSource.SetResult(result);

            return completionSource.Task;
        }
#endif

        #endregion
    }
}
