#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParallelHelper.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if NET35
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
#else
using System.Collections.Concurrent;
using System.Threading.Tasks;
#endif

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// The <see cref="ParallelHelper"/> class contains similar methods to the <see cref="O:System.Threading.Tasks.Parallel.For">Parallel.For</see> overloads
    /// in .NET Framework 4.0 and later but the ones in this class can be used even on .NET Framework 3.5, support reporting progress and have async overloads.
    /// </summary>
    public static class ParallelHelper
    {
        #region Fields

        private static int? coreCount;

#if !NET35
        private static readonly ParallelOptions defaultParallelOptions = new ParallelOptions();
#endif

        #endregion

        #region Properties

        internal static int CoreCount => coreCount ??= Environment.ProcessorCount;
        internal static bool IsSingleCoreCpu => CoreCount == 1;

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Executes an indexed loop synchronously, in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <remarks>
        /// <para>This method is functionally the same as <see cref="Parallel.For(int, int, Action{int})">Parallel.For(int, int, Action&lt;int>)</see>
        /// but it can be used even in .NET Framework 3.5.</para>
        /// <para>If <paramref name="fromInclusive"/> is greater than or equal to <paramref name="toExclusive"/>, then the method returns without performing any iterations.</para>
        /// <para>This method has no return type because if there was no exception, then the loop is guaranteed to be completed.</para>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation or reporting progress.
        /// Use the <see cref="For{T}(T, int, int, ParallelConfig?, Action{int})"/> overload to adjust parallelization, set up cancellation, report progress;
        /// or the <see cref="BeginFor{T}(T, int, int, AsyncConfig?, Action{int})"/>/<see cref="ForAsync{T}(T, int, int, TaskConfig?, Action{int})"/>
        /// (in .NET Framework 4.0 and above) methods to do these asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        public static void For(int fromInclusive, int toExclusive, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return;

            DoFor<object?>(AsyncHelper.DefaultContext, null, fromInclusive, toExclusive, body);
        }

        /// <summary>
        /// Executes an indexed loop synchronously, in which iterations may run in parallel and the execution can be configured.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="For{T}(T, int, int, ParallelConfig?, Action{int})"/> overload for details.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation.
        /// This method does not report progress even if <see cref="AsyncConfigBase.Progress"/> is set in this parameter.
        /// To report progress use the <see cref="For{T}(T, int, int, ParallelConfig?, Action{int})"/> overload.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool For(int fromInclusive, int toExclusive, ParallelConfig? configuration, Action<int> body)
            => For<object?>(null, fromInclusive, toExclusive, configuration, body);

        /// <summary>
        /// Executes an indexed loop synchronously, in which iterations may run in parallel and the execution can be configured.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="operation"/> parameter.</typeparam>
        /// <param name="operation">The operation to be reported when <see cref="AsyncConfigBase.Progress"/> is set in <paramref name="configuration"/>.
        /// Progress is reported only if this parameter is not <see langword="null"/>.</param>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization, cancellation or reporting progress.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method is similar to <see cref="Parallel.For(int, int, ParallelOptions, Action{int})">Parallel.For(int, int, ParallelOptions, Action&lt;int>)</see>
        /// but it can be used even in .NET Framework 3.5 and supports reporting progress.</para>
        /// <para>If <paramref name="fromInclusive"/> is greater than or equal to <paramref name="toExclusive"/>, then the method returns without performing any iterations.</para>
        /// <para>If <paramref name="operation"/> is not <see langword="null"/>, <see cref="AsyncConfigBase.Progress"/> is set in <paramref name="configuration"/> and there is at least one iteration,
        /// then the <see cref="IAsyncProgress.New{T}">IAsyncProgress.New</see> method will be called before the first iteration passing the specified <paramref name="operation"/> to the <c>operationType</c> parameter.
        /// It will be followed by as many <see cref="IAsyncProgress.Increment">IAsyncProgress.Increment</see> calls as many iterations were completed successfully.</para>
        /// <note>This method blocks the caller until the iterations are completed. To perform the execution asynchronously use
        /// the <see cref="O:KGySoft.Threading.ParallelHelper.BeginFor">BeginFor</see> or <see cref="O:KGySoft.Threading.ParallelHelper.ForAsync">ForAsync</see>
        /// (in .NET Framework 4.0 and above) methods.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool For<T>(T operation, int fromInclusive, int toExclusive, ParallelConfig? configuration, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return AsyncHelper.FromResult(true, configuration);

            return AsyncHelper.DoOperationSynchronously(ctx => DoFor(ctx, operation, fromInclusive, toExclusive, body), configuration);
        }

        /// <summary>
        /// Begins to execute an indexed loop asynchronously, in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="ForAsync(int,int,Action{int})"/> method.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndFor">EndFor</see> method.</para>
        /// <para>If <paramref name="fromInclusive"/> is greater than or equal to <paramref name="toExclusive"/>, then the operation completes synchronously without performing any iterations.</para>
        /// <note>This method adjusts the degree of parallelization automatically and does not support cancellation or reporting progress.
        /// Use the <see cref="BeginFor{T}(T, int, int, AsyncConfig?, Action{int})"/> overload to adjust parallelization, set up cancellation, report progress;
        /// or the <see cref="ForAsync{T}(T, int, int, TaskConfig?, Action{int})"/> method if you target .NET Framework 4.0 or later.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        public static IAsyncResult BeginFor(int fromInclusive, int toExclusive, Action<int> body)
            => BeginFor<object?>(null, fromInclusive, toExclusive, null, body);

        /// <summary>
        /// Begins to execute an indexed loop asynchronously, in which iterations may run in parallel and the execution can be configured.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="BeginFor{T}(T, int, int, AsyncConfig?, Action{int})"/> overload for details.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization, cancellation or completion callback.
        /// This method does not report progress even if <see cref="AsyncConfigBase.Progress"/> is set in this parameter.
        /// To report progress use the <see cref="BeginFor{T}(T, int, int, AsyncConfig?, Action{int})"/> overload.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        public static IAsyncResult BeginFor(int fromInclusive, int toExclusive, AsyncConfig? asyncConfig, Action<int> body)
            => BeginFor<object?>(null, fromInclusive, toExclusive, asyncConfig, body);

        /// <summary>
        /// Begins to execute an indexed loop asynchronously, in which iterations may run in parallel and the execution can be configured.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="operation"/> parameter.</typeparam>
        /// <param name="operation">The operation to be reported when <see cref="AsyncConfigBase.Progress"/> is set in <paramref name="asyncConfig"/>.
        /// Progress is reported only if this parameter is not <see langword="null"/>.</param>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization, cancellation, completion callback or reporting progress.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="ForAsync{T}(T, int, int, TaskConfig?, Action{int})"/> method.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndFor">EndFor</see> method.</para>
        /// <para>If <paramref name="fromInclusive"/> is greater than or equal to <paramref name="toExclusive"/>, then the operation completes synchronously without performing any iterations.</para>
        /// <para>If <paramref name="operation"/> is not <see langword="null"/>, <see cref="AsyncConfigBase.Progress"/> is set in <paramref name="asyncConfig"/> and there is at least one iteration,
        /// then the <see cref="IAsyncProgress.New{T}">IAsyncProgress.New</see> method will be called before the first iteration passing the specified <paramref name="operation"/> to the <c>operationType</c> parameter.
        /// It will be followed by as many <see cref="IAsyncProgress.Increment">IAsyncProgress.Increment</see> calls as many iterations were completed successfully.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        public static IAsyncResult BeginFor<T>(T operation, int fromInclusive, int toExclusive, AsyncConfig? asyncConfig, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.BeginOperation(ctx => DoFor(ctx, operation, fromInclusive, toExclusive, body), asyncConfig);
        }

        /// <summary>
        /// Waits for the pending asynchronous operation started by one of the <see cref="O:KGySoft.Threading.ParallelHelper.BeginFor">BeginFor</see> methods to complete.
        /// In .NET Framework 4.0 and above you can use the <see cref="O:KGySoft.Threading.ParallelHelper.ForAsync">ForAsync</see> methods instead.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in the <c>asyncConfig</c> parameter was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="asyncResult"/> was not returned by a <see cref="O:KGySoft.Threading.ParallelHelper.BeginFor">BeginFor</see> overload or
        /// this method was called with the same instance multiple times.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in the <c>asyncConfig</c> parameter was <see langword="true"/>.</exception>
        public static bool EndFor(IAsyncResult asyncResult) => AsyncHelper.EndOperation<bool>(asyncResult, nameof(BeginFor));

#if !NET35
        /// <summary>
        /// Executes an indexed loop asynchronously, in which iterations may run in parallel.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>If <paramref name="fromInclusive"/> is greater than or equal to <paramref name="toExclusive"/>, then the operation completes synchronously without performing any iterations.</para>
        /// <para>The <see cref="Task"/> returned by this method has no result because if there was no exception, then the loop is guaranteed to be completed.</para>
        /// <note>This method adjusts the degree of parallelization automatically and does not support cancellation or reporting progress.
        /// Use the <see cref="ForAsync{T}(T, int, int, TaskConfig?, Action{int})"/> overload to adjust parallelization, set up cancellation or to report progress.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        public static Task ForAsync(int fromInclusive, int toExclusive, Action<int> body)
            => ForAsync<object?>(null, fromInclusive, toExclusive, null, body);

        /// <summary>
        /// Executes an indexed loop asynchronously, in which iterations may run in parallel and the execution can be configured.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="ForAsync{T}(T, int, int, TaskConfig?, Action{int})"/> overload for details.
        /// </summary>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation.
        /// This method does not report progress even if <see cref="AsyncConfigBase.Progress"/> is set in this parameter.
        /// To report progress use the <see cref="ForAsync{T}(T, int, int, TaskConfig?, Action{int})"/> overload.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> ForAsync(int fromInclusive, int toExclusive, TaskConfig? asyncConfig, Action<int> body)
            => ForAsync<object?>(null, fromInclusive, toExclusive, asyncConfig, body);

        /// <summary>
        /// Executes an indexed loop asynchronously, in which iterations may run in parallel and the execution can be configured.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="operation"/> parameter.</typeparam>
        /// <param name="operation">The operation to be reported when <see cref="AsyncConfigBase.Progress"/> is set in <paramref name="asyncConfig"/>.
        /// Progress is reported only if this parameter is not <see langword="null"/>.</param>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization, cancellation, completion callback or reporting progress.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>If <paramref name="fromInclusive"/> is greater than or equal to <paramref name="toExclusive"/>, then the operation completes synchronously without performing any iterations.</para>
        /// <para>If <paramref name="operation"/> is not <see langword="null"/>, <see cref="AsyncConfigBase.Progress"/> is set in <paramref name="asyncConfig"/> and there is at least one iteration,
        /// then the <see cref="IAsyncProgress.New{T}">IAsyncProgress.New</see> method will be called before the first iteration passing the specified <paramref name="operation"/> to the <c>operationType</c> parameter.
        /// It will be followed by as many <see cref="IAsyncProgress.Increment">IAsyncProgress.Increment</see> calls as many iterations were completed successfully.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> ForAsync<T>(T operation, int fromInclusive, int toExclusive, TaskConfig? asyncConfig, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.DoOperationAsync(ctx => DoFor(ctx, operation, fromInclusive, toExclusive, body), asyncConfig);
        }
#endif

        /// <summary>
        /// Executes an indexed loop inside of an already created, possibly asynchronous <paramref name="context"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="operation"/> parameter.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="operation">The operation to be reported when <see cref="AsyncConfigBase.Progress"/> in <paramref name="context"/> is not <see langword="null"/>.
        /// Progress is reported only if this parameter is not <see langword="null"/>.</param>
        /// <param name="fromInclusive">The start index, inclusive.</param>
        /// <param name="toExclusive">The end index, exclusive.</param>
        /// <param name="body">The delegate that is invoked once per iteration.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism, the ability of cancellation and reporting progress depend on how these were configured at the top level method.</para>
        /// <note type="tip">See the <strong>Examples</strong> section of the <see cref="AsyncHelper"/>
        /// class for details about how to create a context for possibly async top level methods.</note>
        /// <note>See the <see cref="For{T}(T, int, int, ParallelConfig?, Action{int})"/> overload for more details about the other parameters.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
        public static bool For<T>(IAsyncContext context, T operation, int fromInclusive, int toExclusive, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return true;

            return DoFor(context, operation, fromInclusive, toExclusive, body);
        }

        #endregion

        #region Private Methods

#if NET35
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Exceptions in pool threads must not be thrown in place but from the caller thread.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502: Avoid excessive complexity",
            Justification = "Special optimization for .NET 3.5 version where there is no Parallel.For")]
#endif
        private static bool DoFor<T>(IAsyncContext context, T operation, int fromInclusive, int toExclusive, Action<int> body)
        {
            #region Local Methods
#if !NET35

            void DoWorkWithProgress(int y)
            {
                body.Invoke(y);
                context.Progress!.Increment();
            }

            void DoWorkWithCancellation(int y, ParallelLoopState state)
            {
                if (context.IsCancellationRequested)
                {
                    state.Stop();
                    return;
                }

                body.Invoke(y);
            }

            void DoWorkWithCancellationAndProgress(int y, ParallelLoopState state)
            {
                if (context.IsCancellationRequested)
                {
                    state.Stop();
                    return;
                }

                body.Invoke(y);
                context.Progress!.Increment();
            }

#endif
            #endregion

            Debug.Assert(toExclusive > fromInclusive);
            int count = toExclusive - fromInclusive;
            bool reportProgress = context.Progress != null && operation != null;
            if (reportProgress)
                context.Progress!.New(operation, count);

            // a single iteration: invoke once
            if (count == 1)
            {
                if (context.IsCancellationRequested)
                    return false;

                body.Invoke(fromInclusive);
                if (reportProgress)
                    context.Progress!.Increment();
                return true;
            }

            // single core or no parallelism: sequential invoke
            if (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1)
            {
                for (int i = fromInclusive; i < toExclusive; i++)
                {
                    if (context.IsCancellationRequested)
                        return false;
                    body.Invoke(i);
                    if (reportProgress)
                        context.Progress!.Increment();
                }

                return true;
            }

#if NET35
            int busyCount = 0;
            Exception? error = null;
            int maxThreads = context.MaxDegreeOfParallelism <= 0 ? CoreCount : context.MaxDegreeOfParallelism;
            int rangeSize = count / maxThreads;

            // we have enough cores/degree for each iteration
            if (rangeSize <= 1)
            {
                for (int i = fromInclusive; i < toExclusive; i++)
                {
                    // not queuing more tasks than the limit
                    while (busyCount >= maxThreads && error == null)
                        Thread.Sleep(0);

                    if (error != null || context.IsCancellationRequested)
                        break;

                    Interlocked.Increment(ref busyCount);

                    int value = i;
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            body.Invoke(value);
                            if (reportProgress)
                                context.Progress!.Increment();
                        }
                        catch (Exception e)
                        {
                            Interlocked.CompareExchange(ref error, e, null);
                        }
                        finally
                        {
                            // ReSharper disable once AccessToModifiedClosure - intended, both outside and inside changes matter
                            Interlocked.Decrement(ref busyCount);
                        }
                    });
                }
            }
            // we merge some iterations to be processed by the same core
            else
            {
                var ranges = CreateRanges(fromInclusive, toExclusive, rangeSize);
                foreach (var range in ranges)
                {
                    // not queuing more tasks than the number of cores
                    while (busyCount >= maxThreads && error == null)
                        Thread.Sleep(0);

                    if (error != null || context.IsCancellationRequested)
                        break;
                    Interlocked.Increment(ref busyCount);

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            for (int i = range.From; i < range.To; i++)
                            {
                                if (context.IsCancellationRequested)
                                    return;
                                body.Invoke(i);
                                if (reportProgress)
                                    context.Progress!.Increment();
                            }
                        }
                        catch (Exception e)
                        {
                            Interlocked.CompareExchange(ref error, e, null);
                        }
                        finally
                        {
                            // ReSharper disable once AccessToModifiedClosure - intended, both outside and inside changes matter
                            Interlocked.Decrement(ref busyCount);
                        }
                    });
                }
            }

            // waiting until every task is finished
            while (busyCount > 0)
                Thread.Sleep(0);

            if (error != null)
                ExceptionDispatchInfo.Capture(error).Throw();
#else
            Action<int, ParallelLoopState>? bodyWithState = null;
            Action<int>? simpleBody = null;
            if (context.CanBeCanceled)
                bodyWithState = reportProgress ? DoWorkWithCancellationAndProgress : DoWorkWithCancellation;
            else
                simpleBody = reportProgress ? DoWorkWithProgress : body;

            int rangeSize;
            ParallelOptions options;
            if (context.MaxDegreeOfParallelism <= 0)
            {
                rangeSize = count / CoreCount;
                options = defaultParallelOptions;
            }
            else
            {
                rangeSize = count / context.MaxDegreeOfParallelism;
                options = new ParallelOptions { MaxDegreeOfParallelism = context.MaxDegreeOfParallelism };
            }

            // we have enough cores/degree for each iteration
            if (rangeSize <= 1)
            {
                if (bodyWithState != null)
                    Parallel.For(fromInclusive, toExclusive, options, bodyWithState);
                else
                    Parallel.For(fromInclusive, toExclusive, options, simpleBody!);
                return !context.IsCancellationRequested;
            }

            // We merge some iterations to be processed by the same core
            // NOTE: We could use CreateRanges just like for .NET Framework 3.5 but even though it allocates less and uses value tuples,
            //       processing some general IEnumerable<> seems to be slower than processing the returned OrderablePartitioner<> instance.
            OrderablePartitioner<Tuple<int, int>> ranges = Partitioner.Create(fromInclusive, toExclusive, rangeSize);
            if (bodyWithState != null)
            {
                Parallel.ForEach(ranges, options, (range, state) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        bodyWithState.Invoke(i, state);
                        if (state.IsStopped)
                            return;
                    }
                });

                return !context.IsCancellationRequested;
            }

            Parallel.ForEach(ranges, options, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    simpleBody!.Invoke(i);
            });
#endif

            return !context.IsCancellationRequested;
        }

#if NET35
        private static IEnumerable<(int From, int To)> CreateRanges(int fromInclusive, int toExclusive, int rangeSize)
        {
            for (int i = fromInclusive; i < toExclusive; i += rangeSize)
            {
                // overflow check
                if (i + rangeSize < i)
                {
                    yield return (i, toExclusive);
                    yield break;
                }

                yield return (i, Math.Min(toExclusive, i + rangeSize));
            }
        }
#endif

        #endregion

        #endregion
    }
}
