#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AsyncHelper.cs
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading;

#if !NET35
using System.Threading.Tasks;
#endif

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#pragma warning disable CS8620 // nullability of generic delegates is detected incorrectly for .NET Framework 3.5
#endif

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// A helper class to implement CPU-bound operations with adjustable parallelization, cancellation and progress reporting,
    /// allowing a single shared implementation for both sync and async overloads where the latter can be
    /// either <see cref="IAsyncResult"/> or <see cref="Task"/> (.NET Framework 4.0 and later) returning methods.
    /// <div style="display: none;"><br/>See the <a href="https://koszeggy.github.io/docs/corelibraries/html/T_KGySoft_Threading_AsyncHelper.htm">online help</a> for an example.</div>
    /// </summary>
    /// <example>
    /// The following example demonstrates how to use the <see cref="AsyncHelper"/> class to create sync/async versions of a method
    /// sharing the common implementation in a single method.
    /// <code lang="C#"><![CDATA[
    /// #nullable enable
    /// 
    /// using System;
    /// using System.Threading;
    /// using System.Threading.Tasks;
    /// 
    /// using KGySoft;
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Reflection;
    /// using KGySoft.Security.Cryptography;
    /// using KGySoft.Threading;
    /// 
    /// public static class Example
    /// {
    ///     // The sync version. This method is blocking, cannot be canceled, does not report progress and auto adjusts parallelization.
    ///     public static double[] GenerateRandomValues(int count, double min, double max)
    ///     {
    ///         ValidateArguments(count, min, max);
    /// 
    ///         // Just to demonstrate some immediate return (which is quite trivial in this overload).
    ///         if (count == 0)
    ///             return Reflector.EmptyArray<double>(); // same as Array.Empty but available also for older targets
    /// 
    ///         // The actual processing is called directly from this overload with DefaultContext. The result is never null from here.
    ///         return DoGenerateRandomValues(AsyncHelper.DefaultContext, count, min, max)!;
    ///     }
    /// 
    ///     // Another sync overload. This is still blocking but allows cancellation, reporting progress and adjusting parallelization.
    ///     // The result can be null if the operation is canceled and config.ThrowIfCanceled was false.
    ///     public static double[]? GenerateRandomValues(int count, double min, double max, ParallelConfig? config)
    ///     {
    ///         ValidateArguments(count, min, max);
    /// 
    ///         // For immediate return use AsyncHelper.FromResult, which handles throwing possible OperationCanceledException
    ///         // or returning null if config.ThrowIfCanceled was false. For void methods use AsyncHelper.HandleCompleted instead.
    ///         if (count == 0)
    ///             return AsyncHelper.FromResult(Reflector.EmptyArray<double>(), config);
    /// 
    ///         // Even though this is a synchronous call, use AsyncHelper to take care of the context and handle cancellation.
    ///         return AsyncHelper.DoOperationSynchronously(context => DoGenerateRandomValues(context, count, min, max), config);
    ///     }
    /// 
    ///     // The Task-returning version. Requires .NET Framework 4.0 or later and can be awaited in .NET Framework 4.5 or later.
    ///     public static Task<double[]?> GenerateRandomValuesAsync(int count, double min, double max, TaskConfig? asyncConfig = null)
    ///     {
    ///         ValidateArguments(count, min, max);
    /// 
    ///         // Use AsyncHelper.FromResult for immediate return. It handles asyncConfig.ThrowIfCanceled properly.
    ///         // To return a Task without a result use AsyncHelper.FromCompleted instead.
    ///         if (count == 0)
    ///             return AsyncHelper.FromResult(Reflector.EmptyArray<double>(), asyncConfig);
    /// 
    ///         // The actual processing for Task returning async methods.
    ///         return AsyncHelper.DoOperationAsync(context => DoGenerateRandomValues(context, count, min, max), asyncConfig);
    ///     }
    /// 
    ///     // The old-style Begin/End methods that work even in .NET Framework 3.5. Can be omitted if not needed.
    ///     public static IAsyncResult BeginGenerateRandomValues(int count, double min, double max, AsyncConfig? asyncConfig = null)
    ///     {
    ///         ValidateArguments(count, min, max);
    /// 
    ///         // Use AsyncHelper.FromResult for immediate return. It handles asyncConfig.ThrowIfCanceled and
    ///         // sets IAsyncResult.CompletedSynchronously. Use AsyncHelper.FromCompleted if the End method has no return value.
    ///         if (count == 0)
    ///             return AsyncHelper.FromResult(Reflector.EmptyArray<double>(), asyncConfig);
    /// 
    ///         // The actual processing for IAsyncResult returning async methods.
    ///         return AsyncHelper.BeginOperation(context => DoGenerateRandomValues(context, count, min, max), asyncConfig);
    ///     }
    /// 
    ///     // Note that the name of "BeginGenerateRandomValues" is explicitly specified here.
    ///     // Older compilers need it also for AsyncContext.BeginOperation.
    ///     public static double[]? EndGenerateRandomValues(IAsyncResult asyncResult)
    ///         => AsyncHelper.EndOperation<double[]?>(asyncResult, nameof(BeginGenerateRandomValues));
    /// 
    ///     // The method of the actual processing has the same parameters as the sync version after an IAsyncContext parameter.
    ///     // The result can be null if the operation is canceled (but see also the next comment)
    ///     private static double[]? DoGenerateRandomValues(IAsyncContext context, int count, double min, double max)
    ///     {
    ///         // Not throwing OperationCanceledException explicitly: it will be thrown by the caller
    ///         // if the asyncConfig.ThrowIfCanceled was true in the async overloads.
    ///         // Actually we could call context.ThrowIfCancellationRequested() that would be caught conditionally by the caller
    ///         // but use that only if really needed because using exceptions as control flow is really ineffective.
    ///         if (context.IsCancellationRequested)
    ///             return null;
    /// 
    ///         // New progress without max value: in a UI this can be displayed with some indeterminate progress bar/circle
    ///         context.Progress?.New("Initializing");
    ///         Thread.Sleep(100); // imitating some really slow initialization, blocking the current thread
    ///         var result = new double[count];
    ///         using var rnd = new SecureRandom(); // just because it's slow
    /// 
    ///         // We should periodically check after longer steps whether the processing has already been canceled
    ///         if (context.IsCancellationRequested)
    ///             return null;
    /// 
    ///         // Possible shortcut: ParallelHelper has a For overload that can accept an already created context from
    ///         // implementations like this one. It will call IAsyncProgress.New and IAsyncProgress.Increment implicitly.
    ///         ParallelHelper.For(context, "Generating values", 0, count,
    ///             body: i => result[i] = rnd.NextDouble(min, max, FloatScale.ForceLogarithmic)); // some slow number generation
    /// 
    ///         // Actually the previous ParallelHelper.For call returns false if the operation was canceled
    ///         if (context.IsCancellationRequested)
    ///             return null;
    /// 
    ///         // Alternative version with Parallel.For (only in .NET Framework 4.0 and above)
    ///         context.Progress?.New("Generating values (alternative way)", maximumValue: count);
    ///         Parallel.For(0, count,
    ///             // for auto-adjusting parallelism ParallelOptions strictly requires -1, whereas context allows <= 0
    ///             new ParallelOptions { MaxDegreeOfParallelism = context.MaxDegreeOfParallelism <= 0 ? -1 : context.MaxDegreeOfParallelism },
    ///             (i, state) =>
    ///             {
    ///                 // Breaking the loop on cancellation. Note that we did not set a CancellationToken in ParallelOptions
    ///                 // because if context comes from the .NET Framework 3.5-compatible Begin method it cannot even have any.
    ///                 // But if you really need a CancellationToken you can pass one to asyncConfig.State from the caller.
    ///                 if (context.IsCancellationRequested)
    ///                 {
    ///                     state.Stop();
    ///                     return;
    ///                 }
    /// 
    ///                 result[i] = rnd.NextDouble(min, max, FloatScale.ForceLogarithmic);
    ///                 context.Progress?.Increment();
    ///             });
    /// 
    ///         return context.IsCancellationRequested ? null : result;
    ///     }
    /// 
    ///     private static void ValidateArguments(int count, double min, double max)
    ///     {
    ///         if (count < 0)
    ///             throw new ArgumentOutOfRangeException(nameof(count), PublicResources.ArgumentMustBeGreaterThanOrEqualTo(0));
    ///         if (Double.IsNaN(min))
    ///             throw new ArgumentOutOfRangeException(nameof(min), PublicResources.ArgumentOutOfRange);
    ///         if (Double.IsNaN(max))
    ///             throw new ArgumentOutOfRangeException(nameof(max), PublicResources.ArgumentOutOfRange);
    ///         if (max < min)
    ///             throw new ArgumentException(PublicResources.MaxValueLessThanMinValue);
    ///     }
    /// }]]></code>
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

            private readonly Func<bool>? isCancelRequestedCallback;

            private volatile bool isCancellationRequested;

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

        private class AsyncResultContext : IAsyncResult, IAsyncContext, IDisposable
        {
            #region Fields

            private readonly bool throwIfCanceled = true;
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
                throwIfCanceled = asyncConfig.ThrowIfCanceled;
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
                if (isCancellationRequested && throwIfCanceled)
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

        #endregion

        #region Fields

        private static IAsyncContext? emptyContext;
        private static IAsyncContext? singleThreadContext;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a default context for non-async operations that allows to set the number of threads automatically, does not support reporting progress, and is not cancellable.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        public static IAsyncContext DefaultContext => emptyContext ??= new EmptyContext();

        /// <summary>
        /// Gets a predefined context for non-async operations that forces to run a possibly parallel algorithm on a single thread, does not support reporting progress, and is not cancellable.
        /// It actually returns a <see cref="SimpleContext"/> instance.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details about using <see cref="IAsyncContext"/>.
        /// </summary>
        public static IAsyncContext SingleThreadContext => singleThreadContext ??= new SimpleContext(1);

        #endregion

        #region Methods

        #region Sync

        /// <summary>
        /// Executes the specified <paramref name="operation"/> synchronously, in which some sub-operations may run in parallel.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="configuration">The configuration for the operation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static void DoOperationSynchronously(Action<IAsyncContext> operation, ParallelConfig? configuration)
        {
            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);

            IAsyncContext context = configuration == null ? DefaultContext : new SyncContext(configuration);
            if (context.IsCancellationRequested)
            {
                if (configuration?.ThrowIfCanceled != false)
                    Throw.OperationCanceledException();
                return;
            }

            try
            {
                operation.Invoke(context);
            }
            catch (OperationCanceledException) when (configuration?.ThrowIfCanceled == false)
            {
            }

            if (context.IsCancellationRequested && configuration?.ThrowIfCanceled != false)
                Throw.OperationCanceledException();
        }

        /// <summary>
        /// Executes the specified <paramref name="operation"/> synchronously, in which some sub-operations may run in parallel.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the specified <paramref name="operation"/>.</typeparam>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="configuration">The configuration for the operation.</param>
        /// <returns>The result of the <paramref name="operation"/> if the operation has not been canceled,
        /// or the default value of <typeparamref name="TResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static TResult? DoOperationSynchronously<TResult>(Func<IAsyncContext, TResult> operation, ParallelConfig? configuration)
            => DoOperationSynchronously(operation, default, configuration);

        /// <summary>
        /// Executes the specified <paramref name="operation"/> synchronously, in which some sub-operations may run in parallel.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the specified <paramref name="operation"/>.</typeparam>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="canceledResult">The result to be returned if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> of <paramref name="configuration"/> returns <see langword="false"/>.</param>
        /// <param name="configuration">The configuration for the operation.</param>
        /// <returns>The result of the <paramref name="operation"/> if the operation has not been canceled,
        /// or <paramref name="canceledResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static TResult DoOperationSynchronously<TResult>(Func<IAsyncContext, TResult> operation, TResult canceledResult, ParallelConfig? configuration)
        {
            if (operation == null!)
                Throw.ArgumentNullException<string>(Argument.operation);

            IAsyncContext context = configuration == null ? DefaultContext : new SyncContext(configuration);
            if (context.IsCancellationRequested)
            {
                if (configuration?.ThrowIfCanceled != false)
                    Throw.OperationCanceledException();
                return canceledResult;
            }

            try
            {
                TResult result = operation.Invoke(context);
                if (!context.IsCancellationRequested)
                    return result;
            }
            catch (OperationCanceledException) when (configuration?.ThrowIfCanceled == false)
            {
                return canceledResult;
            }

            if (configuration?.ThrowIfCanceled != false)
                Throw.OperationCanceledException();
            return canceledResult;
        }

        /// <summary>
        /// This method can be used to immediately finish a synchronous operation that does not have a return value.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// The example uses the similarly working <see cref="FromResult{TResult}(TResult,ParallelConfig?)">FromResult</see> method.
        /// </summary>
        /// <param name="configuration">The configuration for the operation.</param>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static void HandleCompleted(ParallelConfig? configuration)
        {
            if (configuration?.IsCancelRequestedCallback?.Invoke() == true && configuration.ThrowIfCanceled)
                Throw.OperationCanceledException();
        }

        /// <summary>
        /// This method can be used to immediately return from a synchronous operation that has a return value.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="result">The result to be returned if the operation was not canceled.</param>
        /// <param name="configuration">The configuration for the operation.</param>
        /// <returns><paramref name="result"/> if the operation has not been canceled,
        /// or the default value of <typeparamref name="TResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static TResult? FromResult<TResult>(TResult result, ParallelConfig? configuration)
            => FromResult(result, default, configuration);

        /// <summary>
        /// This method can be used to immediately return from a synchronous operation that has a return value.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="result">The result to be returned if the operation has not been canceled.</param>
        /// <param name="canceledResult">The result to be returned if the operation has been canceled.</param>
        /// <param name="configuration">The configuration for the operation.</param>
        /// <returns><paramref name="result"/> if the operation has not been canceled,
        /// or <paramref name="canceledResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static TResult FromResult<TResult>(TResult result, TResult canceledResult, ParallelConfig? configuration)
        {
            if (configuration?.IsCancelRequestedCallback?.Invoke() != true)
                return result;

            if (configuration.ThrowIfCanceled)
                Throw.OperationCanceledException();
            return canceledResult;
        }

        #endregion

        #region Async APM

        /// <summary>
        /// Exposes the specified <paramref name="operation"/> with no return value as an <see cref="IAsyncResult"/>-returning async operation.
        /// The operation can be completed by calling the <see cref="EndOperation">EndOperation</see> method.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <param name="beginMethodName">The name of the method that represents the <paramref name="operation"/>.
        /// This must be passed also to the <see cref="EndOperation">EndOperation</see> method. This parameter is optional.
        /// <br/>Default value: The name of the caller method when used with a compiler that recognizes <see cref="CallerMemberNameAttribute"/>; otherwise, <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> instance representing the asynchronous operation.
        /// To complete the operation it must be passed to the <see cref="EndOperation">EndOperation</see> method.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="beginMethodName"/> is <see langword="null"/>.</exception>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when calling the EndOperation method.")]
        [SecuritySafeCritical]
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
                ThreadPool.UnsafeQueueUserWorkItem(DoWork!, asyncResult);
            return asyncResult;
        }

        /// <summary>
        /// Exposes the specified <paramref name="operation"/> with a return value as an <see cref="IAsyncResult"/>-returning async operation.
        /// To obtain the result the <see cref="EndOperation{TResult}">EndOperation</see> method must be called.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the specified <paramref name="operation"/>.</typeparam>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <param name="beginMethodName">The name of the method that represents the <paramref name="operation"/>.
        /// This must be passed also to the <see cref="EndOperation{TResult}">EndOperation</see> method. This parameter is optional.
        /// <br/>Default value: The name of the caller method when used with a compiler that recognizes <see cref="CallerMemberNameAttribute"/>; otherwise, <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> instance representing the asynchronous operation.
        /// To complete the operation it must be passed to the <see cref="EndOperation{TResult}">EndOperation</see> method.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="beginMethodName"/> is <see langword="null"/>.</exception>
        public static IAsyncResult BeginOperation<TResult>(Func<IAsyncContext, TResult> operation, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
            => BeginOperation(operation, default, asyncConfig, beginMethodName);

        /// <summary>
        /// Exposes the specified <paramref name="operation"/> with a return value as an <see cref="IAsyncResult"/>-returning async operation.
        /// To obtain the result the <see cref="EndOperation{TResult}">EndOperation</see> method must be called.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the specified <paramref name="operation"/>.</typeparam>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="canceledResult">The result to be returned by <see cref="EndOperation{TResult}">EndOperation</see> if the operation is canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> of <paramref name="asyncConfig"/> returns <see langword="false"/>.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <param name="beginMethodName">The name of the method that represents the <paramref name="operation"/>.
        /// This must be passed also to the <see cref="EndOperation{TResult}">EndOperation</see> method. This parameter is optional.
        /// <br/>Default value: The name of the caller method when used with a compiler that recognizes <see cref="CallerMemberNameAttribute"/>; otherwise, <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> instance representing the asynchronous operation.
        /// To complete the operation it must be passed to the <see cref="EndOperation{TResult}">EndOperation</see> method.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="beginMethodName"/> is <see langword="null"/>.</exception>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
                Justification = "Pool thread exceptions are not suppressed, they will be thrown when calling the EndOperation method.")]
        [SecuritySafeCritical]
        public static IAsyncResult BeginOperation<TResult>(Func<IAsyncContext, TResult> operation, TResult canceledResult, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
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
                ThreadPool.UnsafeQueueUserWorkItem(DoWork!, asyncResult);
            return asyncResult;
        }

        /// <summary>
        /// Returns an <see cref="IAsyncResult"/> instance that represents an already completed operation without a result.
        /// The <see cref="EndOperation">EndOperation</see> method still must be called with the result of this method.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// The example uses the similarly working <see cref="FromResult{TResult}(TResult,AsyncConfig?,string)">FromResult</see> method.
        /// </summary>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <param name="beginMethodName">The name of the method that represents the operation.
        /// This must be passed also to the <see cref="EndOperation">EndOperation</see> method. This parameter is optional.
        /// <br/>Default value: The name of the caller method when used with a compiler that recognizes <see cref="CallerMemberNameAttribute"/>; otherwise, <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> instance representing the asynchronous operation.
        /// To complete the operation it must be passed to the <see cref="EndOperation">EndOperation</see> method.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="beginMethodName"/> is <see langword="null"/>.</exception>
        public static IAsyncResult FromCompleted(AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
        {
            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);

            var asyncResult = new AsyncResultContext(beginMethodName, null, asyncConfig);
            if (asyncResult.IsCancellationRequested)
                asyncResult.SetCanceled();
            else
                asyncResult.SetCompleted();
            asyncResult.CompletedSynchronously = true;
            return asyncResult;
        }

        /// <summary>
        /// Returns an <see cref="IAsyncResult"/> instance that represents an already completed operation with a result.
        /// To obtain the result the <see cref="EndOperation{TResult}">EndOperation</see> method must be called.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="result">The result to be returned by the <see cref="EndOperation{TResult}">EndOperation</see> method if the operation has not been canceled.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <param name="beginMethodName">The name of the method that represents the operation.
        /// This must be passed also to the <see cref="EndOperation">EndOperation</see> method. This parameter is optional.
        /// <br/>Default value: The name of the caller method when used with a compiler that recognizes <see cref="CallerMemberNameAttribute"/>; otherwise, <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> instance representing the asynchronous operation.
        /// To complete the operation it must be passed to the <see cref="EndOperation">EndOperation</see> method.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="beginMethodName"/> is <see langword="null"/>.</exception>
        public static IAsyncResult FromResult<TResult>(TResult result, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
            => FromResult(result, default, asyncConfig, beginMethodName);

        /// <summary>
        /// Returns an <see cref="IAsyncResult"/> instance that represents an already completed operation with a result.
        /// To obtain the result the <see cref="EndOperation{TResult}">EndOperation</see> method must be called.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="result">The result to be returned by the <see cref="EndOperation{TResult}">EndOperation</see> method if the operation has not been canceled.</param>
        /// <param name="canceledResult">The result to be returned by <see cref="EndOperation{TResult}">EndOperation</see> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> of <paramref name="asyncConfig"/> returns <see langword="false"/>.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <param name="beginMethodName">The name of the method that represents the operation.
        /// This must be passed also to the <see cref="EndOperation">EndOperation</see> method. This parameter is optional.
        /// <br/>Default value: The name of the caller method when used with a compiler that recognizes <see cref="CallerMemberNameAttribute"/>; otherwise, <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> instance representing the asynchronous operation.
        /// To complete the operation it must be passed to the <see cref="EndOperation">EndOperation</see> method.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="beginMethodName"/> is <see langword="null"/>.</exception>
        public static IAsyncResult FromResult<TResult>(TResult result, TResult canceledResult, AsyncConfig? asyncConfig, [CallerMemberName]string beginMethodName = null!)
        {
            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);

            var asyncResult = new AsyncResultContext<TResult>(beginMethodName, null, canceledResult, asyncConfig);
            asyncResult.SetResult(asyncResult.IsCancellationRequested ? canceledResult : result);
            asyncResult.CompletedSynchronously = true;
            return asyncResult;
        }

        /// <summary>
        /// Waits for the completion of an operation started by a corresponding <see cref="BeginOperation">BeginOperation</see>,
        /// <see cref="FromResult{TResult}(TResult, TResult, AsyncConfig?, string)">FromResult</see> or <see cref="FromCompleted(AsyncConfig?,string)">FromCompleted</see> call.
        /// If the operation is still running, then this method blocks the caller and waits for the completion.
        /// The possibly occurred exceptions are also thrown then this method is called.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <param name="asyncResult">The result of a corresponding <see cref="BeginOperation">BeginOperation</see> or <see cref="FromCompleted(AsyncConfig?,string)">FromCompleted</see> call.</param>
        /// <param name="beginMethodName">The same name that was passed to the <see cref="BeginOperation">BeginOperation</see>,
        /// <see cref="FromResult{TResult}(TResult, TResult, AsyncConfig?, string)">FromResult</see> or <see cref="FromCompleted(AsyncConfig?,string)">FromCompleted</see> method.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="asyncResult"/> was not returned by the corresponding <see cref="BeginOperation">BeginOperation</see>
        /// or <see cref="FromCompleted(AsyncConfig?,string)">FromCompleted</see> methods with a matching <paramref name="beginMethodName"/>
        /// <br/>-or-
        /// <br/>this method was already called for this <paramref name="asyncResult"/> instance.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in the corresponding <see cref="AsyncConfig"/> was <see langword="true"/>.</exception>
        public static void EndOperation(IAsyncResult asyncResult, string beginMethodName)
        {
            if (asyncResult == null!)
                Throw.ArgumentNullException<string>(Argument.asyncResult);
            if (beginMethodName == null!)
                Throw.ArgumentNullException<string>(Argument.beginMethodName);
            if (asyncResult is not AsyncResultContext result || result.BeginMethodName != beginMethodName || result.IsDisposed)
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

        /// <summary>
        /// Waits for the completion of an operation started by a corresponding <see cref="BeginOperation{TResult}(Func{IAsyncContext, TResult}, TResult, AsyncConfig?, string)">BeginOperation</see>
        /// or <see cref="FromResult{TResult}(TResult, TResult, AsyncConfig?, string)">FromResult</see> call.
        /// If the operation is still running, then this method blocks the caller and waits for the completion.
        /// The possibly occurred exceptions are also thrown then this method is called.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="asyncResult">The result of a corresponding <see cref="BeginOperation{TResult}(Func{IAsyncContext, TResult}, AsyncConfig?, string)">BeginOperation</see>
        /// or <see cref="FromResult{TResult}(TResult, AsyncConfig?, string)">FromResult</see> call.</param>
        /// <param name="beginMethodName">The same name that was passed to the <see cref="BeginOperation{TResult}(Func{IAsyncContext, TResult}, AsyncConfig?, string)">BeginOperation</see>
        /// or <see cref="FromResult{TResult}(TResult, AsyncConfig?, string)">FromResult</see> method.</param>
        /// <returns>The result of the operation, or a default value representing the canceled result if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in the <c>asyncConfig</c> parameter was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="asyncResult"/> was not returned by the corresponding <see cref="BeginOperation{TResult}(Func{IAsyncContext, TResult}, AsyncConfig?, string)">BeginOperation</see>
        /// or <see cref="FromResult{TResult}(TResult, AsyncConfig?, string)">FromResult</see> methods with a matching <paramref name="beginMethodName"/>
        /// <br/>-or-
        /// <br/>this method was already called for this <paramref name="asyncResult"/> instance.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in the corresponding <see cref="AsyncConfig"/> was <see langword="true"/>.</exception>
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

        #endregion

        #region Async TPL
#if !NET35

        /// <summary>
        /// Executes the specified <paramref name="operation"/> asynchronously.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when task is awaited or Result is accessed.")]
        [SecuritySafeCritical]
        public static Task DoOperationAsync(Action<IAsyncContext> operation, TaskConfig? asyncConfig)
        {
            #region Local Methods

            // this method is executed on a pool thread
            static void DoWork(object state)
            {
#if NET
                var (context, completion, op) = ((TaskContext, TaskCompletionSource, Action<IAsyncContext>))state;
#else
                var (context, completion, op) = ((TaskContext, TaskCompletionSource<object?>, Action<IAsyncContext>))state;
#endif
                try
                {
                    op.Invoke(context);
                    if (context.IsCancellationRequested && context.ThrowIfCanceled)
                        completion.SetCanceled();
                    else
                    {
#if NET
                        completion.SetResult();
#else
                        completion.SetResult(default);
#endif
                    }
                }
                catch (OperationCanceledException)
                {
                    if (context.ThrowIfCanceled)
                        completion.SetCanceled();
                    else
                    {
#if NET
                        completion.SetResult();
#else
                        completion.SetResult(default);
#endif
                    }
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
#if NET
            var completionSource = new TaskCompletionSource(taskContext.State);
#else
            var completionSource = new TaskCompletionSource<object?>(taskContext.State);
#endif
            if (taskContext.IsCancellationRequested)
            {
                if (taskContext.ThrowIfCanceled)
                    completionSource.SetCanceled();
                else
                {
#if NET
                    completionSource.SetResult();
#else
                    completionSource.SetResult(default);
#endif
                }

            }
            else
                ThreadPool.UnsafeQueueUserWorkItem(DoWork!, (taskContext, completionSource, operation));

            return completionSource.Task;
        }

        /// <summary>
        /// Executes the specified <paramref name="operation"/> asynchronously.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the specified <paramref name="operation"/>.</typeparam>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation. Its result is the result of the <paramref name="operation"/> if it has not been canceled,
        /// or the default value of <typeparamref name="TResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<TResult?> DoOperationAsync<TResult>(Func<IAsyncContext, TResult?> operation, TaskConfig? asyncConfig)
            => DoOperationAsync(operation, default, asyncConfig);

        /// <summary>
        /// Executes the specified <paramref name="operation"/> asynchronously.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the specified <paramref name="operation"/>.</typeparam>
        /// <param name="operation">The operation to be executed.</param>
        /// <param name="canceledResult">The result to be returned by the returned task if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> of <paramref name="asyncConfig"/> returns <see langword="false"/>.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous operation. Its result is the result of the <paramref name="operation"/> if it has not been canceled,
        /// or <paramref name="canceledResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Pool thread exceptions are not suppressed, they will be thrown when task is awaited or Result is accessed.")]
        [SecuritySafeCritical]
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
                ThreadPool.UnsafeQueueUserWorkItem(DoWork!, (taskContext, completionSource, operation, canceledResult));

            return completionSource.Task;
        }

        /// <summary>
        /// Returns a task that represents an already completed operation without a result.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// The example uses the similarly working <see cref="FromResult{TResult}(TResult,TaskConfig?)">FromResult</see> method.
        /// </summary>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> that represents the completed operation.</returns>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task FromCompleted(TaskConfig? asyncConfig)
        {
            var taskContext = new TaskContext(asyncConfig);
            var completionSource = new TaskCompletionSource<object?>(taskContext.State);
            if (taskContext.IsCancellationRequested && taskContext.ThrowIfCanceled)
                completionSource.SetCanceled();
            else
                completionSource.SetResult(default);

            return completionSource.Task;
        }

        /// <summary>
        /// Returns a task that represents an already completed operation with a result.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="result">The result to be returned by the returned task if the operation has not been canceled.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the completed operation. Its result is <paramref name="result"/> if the operation has not been canceled,
        /// or the default value of <typeparamref name="TResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<TResult?> FromResult<TResult>(TResult result, TaskConfig? asyncConfig)
            => FromResult(result, default, asyncConfig);

        /// <summary>
        /// Returns a task that represents an already completed operation with a result.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/> class for details.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="result">The result to be returned by the returned task if the operation has not been canceled.</param>
        /// <param name="canceledResult">The result to be returned by the returned task if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> of <paramref name="asyncConfig"/> returns <see langword="false"/>.</param>
        /// <param name="asyncConfig">The configuration for the asynchronous operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the completed operation. Its result is <paramref name="result"/> if the operation has not been canceled,
        /// or <paramref name="canceledResult"/> if the operation has been canceled
        /// and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<TResult> FromResult<TResult>(TResult result, TResult canceledResult, TaskConfig? asyncConfig)
        {
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
                completionSource.SetResult(result);

            return completionSource.Task;
        }

#endif
        #endregion

        #endregion
    }
}
