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
    /// <summary>
    /// A helper class to implement CPU-bound async operations returning either an <see cref="IAsyncResult"/> or a <see cref="Task"/> (.NET Framework 4.0 and above) instance
    /// that can be configured by an <see cref="AsyncConfig"/> or <see cref="TaskConfig"/> parameter, respectively.
    /// <br/>See the <strong>Examples</strong> section for an example.
    /// </summary>
    /// <example>
    /// TODO
    /// </example>
    public static class AsyncHelper
    {
        #region Nested classes

        #region EmptyContext class

        private sealed class EmptyContext : IAsyncContext
        {
            #region Properties

            public int MaxDegreeOfParallelism => 0;
            public bool IsCancellationRequested => false;
            public bool CanBeCanceled => false;
            public IAsyncProgress? Progress => null;
            public object? State => null;

            #endregion

            #region Methods

            public void ThrowIfCancellationRequested() { }

            #endregion
        }

        #endregion

        #region SyncContext class

        private sealed class SyncContext : IAsyncContext
        {
            #region Fields

            private volatile bool isCancellationRequested;
            private Func<bool>? isCancelRequestedCallback;

            #endregion

            #region Properties

            public int MaxDegreeOfParallelism { get; }
            public bool IsCancellationRequested => isCancelRequestedCallback != null && (isCancellationRequested || (isCancellationRequested = isCancelRequestedCallback.Invoke()));
            public bool CanBeCanceled => isCancelRequestedCallback != null;
            public IAsyncProgress? Progress { get; }
            public object? State { get; }

            #endregion

            #region Constructors

            internal SyncContext(ParallelConfig config)
            {
                MaxDegreeOfParallelism = config.MaxDegreeOfParallelism;
                isCancelRequestedCallback = config.IsCancelRequestedCallback;
                Progress = config.Progress;
                State = config.State;
            }

            #endregion

            #region Methods

            public void ThrowIfCancellationRequested()
            {
                if (IsCancellationRequested)
                    Throw.OperationCanceledException();
            }

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
            public object? State { get; }

            #endregion

            #region Internal Properties

            internal bool ThrowIfCanceled { get; } = true;

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

            public WaitHandle AsyncWaitHandle => InternalWaitHandle.WaitHandle;

            public object? AsyncState { get; }
            public bool CompletedSynchronously { get; internal set; }

            #endregion

            #region Internal Properties

            internal bool ThrowIfCanceled { get; } = true;
            internal Action<IAsyncContext>? Operation { get; private set; }
            internal string BeginMethodName { get; }
            internal bool IsDisposed { get; private set; }

            #endregion

            #region Private Properties

            private ManualResetEventSlim InternalWaitHandle
            {
                get
                {
                    if (IsDisposed)
                        Throw.ObjectDisposedException();
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

            #region Explicitly Implemented Interface Properties

            object? IAsyncContext.State => AsyncState;

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

            private static void ThrowOperationCanceled() => Throw.OperationCanceledException();

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
        {
            #region Fields

            private TResult result;

            #endregion

            #region Properties

            #region Public Properties

            internal TResult Result
            {
                get
                {
                    // Though result is not volatile, WaitForCompletion has a volatile read so always a correct value is returned
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

            internal AsyncResultContext(string beginMethod, Func<IAsyncContext, TResult>? operation, TResult canceledResult, AsyncConfig? asyncConfig)
                : base(beginMethod, null, asyncConfig)
            {
                result = canceledResult;
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

#endif
        #endregion

        #endregion

        #region Fields

        private static IAsyncContext? emptyContext;

        #endregion

        #region Properties

        public static IAsyncContext DefaultContext => emptyContext ??= new EmptyContext();

        #endregion

        #region Methods

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when calling the EndOperation method.")]
        public static IAsyncResult BeginOperation(Action<IAsyncContext> operation, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
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

            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);
            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);

            var asyncResult = new AsyncResultContext(beginMethodName, operation, asyncConfig);
            if (asyncResult.IsCancellationRequested)
            {
                asyncResult.SetCanceled();
                asyncResult.CompletedSynchronously = true;
            }
            else
                ThreadPool.QueueUserWorkItem(DoWork!, asyncResult);
            return asyncResult;
        }

        public static IAsyncResult BeginOperation<TResult>(Func<IAsyncContext, TResult> operation, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
            => BeginOperation(operation, default, asyncConfig, beginMethodName);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
                Justification = "Pool thread exceptions are not suppressed, they will be thrown when calling the EndOperation method.")]
        public static IAsyncResult BeginOperation<TResult>(Func<IAsyncContext, TResult> operation, TResult canceledResult, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
        {
            #region Local Methods

            // this method is executed on a pool thread
            static void DoWork(object state)
            {
                var context = (AsyncResultContext<TResult>)state;
                if (context.IsCancellationRequested)
                {
                    if (context.ThrowIfCanceled)
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
                        context.SetResult(result);
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

            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);
            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);

            var asyncResult = new AsyncResultContext<TResult>(beginMethodName, operation, canceledResult, asyncConfig);
            if (asyncResult.IsCancellationRequested)
            {
                asyncResult.SetCanceled();
                asyncResult.CompletedSynchronously = true;
            }
            else
                ThreadPool.QueueUserWorkItem(DoWork!, asyncResult);
            return asyncResult;
        }

        public static IAsyncResult FromCompleted(AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
        {
            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);

            var asyncResult = new AsyncResultContext(beginMethodName, null, asyncConfig);
            asyncResult.SetCompleted();
            asyncResult.CompletedSynchronously = true;
            return asyncResult;
        }

        public static IAsyncResult FromResult<TResult>(TResult result, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
            => FromResult(result, default, asyncConfig, beginMethodName);

        public static IAsyncResult FromResult<TResult>(TResult result, TResult canceledResult, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
        {
            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);

            var asyncResult = new AsyncResultContext<TResult>(beginMethodName, null, canceledResult, asyncConfig);
            asyncResult.SetResult(asyncResult.IsCancellationRequested ? canceledResult : result);
            asyncResult.CompletedSynchronously = true;
            return asyncResult;
        }

        public static void EndOperation(IAsyncResult asyncResult, string beginMethodName)
        {
            if (asyncResult == null!)
                Throw.ArgumentNullException<string>(Argument.asyncResult);
            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);
            if (asyncResult is not AsyncResultContext result || result.GetType() != typeof(AsyncResultContext) || result.BeginMethodName != beginMethodName || result.IsDisposed)
            {
                Throw.InvalidOperationException(Res.InvalidAsyncResult(beginMethodName));
                return;
            }

            try
            {
                result.WaitForCompletion();
            }
            finally
            {
                result.Dispose();
            }
        }

        public static TResult EndOperation<TResult>(IAsyncResult asyncResult, string beginMethodName)
        {
            if (asyncResult == null!)
                Throw.ArgumentNullException<string>(Argument.asyncResult);
            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);
            if (asyncResult is not AsyncResultContext<TResult> result || result.BeginMethodName != beginMethodName || result.IsDisposed)
                return Throw.InvalidOperationException<TResult>(Res.InvalidAsyncResult(beginMethodName));
            try
            {
                return result.Result;
            }
            finally
            {
                result.Dispose();
            }
        }

        public static void DoOperationSynchronously(Action<IAsyncContext> operation, ParallelConfig? configuration)
        {
            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);

            if (configuration?.IsCancelRequestedCallback?.Invoke() == true)
            {
                if (configuration.ThrowIfCanceled)
                    Throw.InvalidOperationException(Res.OperationCanceled);
                return;
            }

            try
            {
                operation.Invoke(configuration == null ? DefaultContext : new SyncContext(configuration));
            }
            catch (OperationCanceledException) when (configuration?.ThrowIfCanceled == false)
            {
            }
        }

        public static TResult? DoOperationSynchronously<TResult>(Func<IAsyncContext, TResult> operation, ParallelConfig? configuration)
            => DoOperationSynchronously(operation, default, configuration);

        public static TResult DoOperationSynchronously<TResult>(Func<IAsyncContext, TResult> operation, TResult canceledResult, ParallelConfig? configuration)
        {
            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);

            if (configuration?.IsCancelRequestedCallback?.Invoke() == true)
            {
                if (configuration.ThrowIfCanceled)
                    Throw.InvalidOperationException(Res.OperationCanceled);
                return canceledResult;
            }

            try
            {
                return operation.Invoke(configuration == null ? DefaultContext : new SyncContext(configuration));
            }
            catch (OperationCanceledException) when (configuration?.ThrowIfCanceled == false)
            {
                return canceledResult;
            }
        }

#if !NET35
        public static Task<TResult?> DoOperationAsync<TResult>(Func<IAsyncContext, TResult?> operation, TaskConfig? asyncConfig)
            => DoOperationAsync(operation, default, asyncConfig);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when task is awaited or Result is accessed.")]
        public static Task<TResult> DoOperationAsync<TResult>(Func<IAsyncContext, TResult> operation, TResult canceledResult, TaskConfig? asyncConfig)
        {
            #region Local Methods

            // this method is executed on a pool thread
            static void DoWork(object state)
            {
                var (context, completion, func, canceledResult) = ((TaskContext, TaskCompletionSource<TResult?>, Func<IAsyncContext, TResult>, TResult))state;
                try
                {
                    TResult result = func.Invoke(context);
                    if (context.IsCancellationRequested)
                    {
                        if (context.ThrowIfCanceled)
                            completion.SetCanceled();
                        else
                            completion.SetResult(canceledResult);
                    }
                    else
                        completion.SetResult(result);
                }
                catch (OperationCanceledException)
                {
                    if (context.ThrowIfCanceled)
                        completion.SetCanceled();
                    else
                        completion.SetResult(canceledResult);
                }
                catch (Exception e)
                {
                    completion.SetException(e);
                }
            }

            #endregion

            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);
            var taskContext = new TaskContext(asyncConfig);
            var completionSource = new TaskCompletionSource<TResult>(taskContext.State);
            if (taskContext.IsCancellationRequested)
            {
                if (taskContext.ThrowIfCanceled)
                    completionSource.SetCanceled();
                else
                    completionSource.SetResult(canceledResult);
            }
            else
                ThreadPool.QueueUserWorkItem(DoWork!, (taskContext, completionSource, operation, canceledResult));

            return completionSource.Task;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when task is awaited or Result is accessed.")]
        public static Task DoOperationAsync(Action<IAsyncContext> operation, TaskConfig? asyncConfig)
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

            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);
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

        public static Task FromCompleted(TaskConfig? asyncConfig)
        {
            var taskContext = new TaskContext(asyncConfig);
            var completionSource = new TaskCompletionSource<_>(taskContext.State);
            if (taskContext.IsCancellationRequested && taskContext.ThrowIfCanceled)
                completionSource.SetCanceled();
            else
                completionSource.SetResult(default);

            return completionSource.Task;
        }

        public static Task<TResult> FromResult<TResult>(TResult result, TaskConfig? asyncConfig)
            => FromResult(result, default!, asyncConfig);

        public static Task<TResult> FromResult<TResult>(TResult result, TResult canceledValue, TaskConfig? asyncConfig)
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
