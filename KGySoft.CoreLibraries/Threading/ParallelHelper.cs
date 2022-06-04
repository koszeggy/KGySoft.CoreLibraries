#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParallelHelper.cs
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

namespace KGySoft.Threading
{
    public static class ParallelHelper
    {
        #region Fields

        private static int? coreCount;

#if !NET35
        private static readonly ParallelOptions defaultParallelOptions = new ParallelOptions();
#endif

        #endregion

        #region Properties

        private static int CoreCount => coreCount ??= Environment.ProcessorCount;

        #endregion

        #region Methods

        #region Public Methods

        // Works even on .NET Framework 3.5. Unlike Parallel.For, it is void because if there was no exception the loop is guaranteed to be completed.
        public static void For(int fromInclusive, int toExclusive, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return;

            DoFor<object?>(AsyncHelper.DefaultContext, null, fromInclusive, toExclusive, body);
        }

        public static void For(int fromInclusive, int toExclusive, ParallelConfig? configuration, Action<int> body)
            => For<object?>(null, fromInclusive, toExclusive, configuration, body);

        public static void For<T>(T operation, int fromInclusive, int toExclusive, ParallelConfig? configuration, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return;

            AsyncHelper.DoOperationSynchronously(ctx => DoFor(ctx, operation, fromInclusive, toExclusive, body), configuration);
        }

        public static IAsyncResult BeginFor(int fromInclusive, int toExclusive, Action<int> body)
            => BeginFor<object?>(null, fromInclusive, toExclusive, null, body);

        public static IAsyncResult BeginFor(int fromInclusive, int toExclusive, AsyncConfig? asyncConfig, Action<int> body)
            => BeginFor<object?>(null, fromInclusive, toExclusive, asyncConfig, body);

        public static IAsyncResult BeginFor<T>(T operation, int fromInclusive, int toExclusive, AsyncConfig? asyncConfig, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return AsyncHelper.FromCompleted(asyncConfig);

            return AsyncHelper.BeginOperation(ctx => DoFor(ctx, operation, fromInclusive, toExclusive, body), asyncConfig);
        }

        public static void EndFor(IAsyncResult asyncResult) => AsyncHelper.EndOperation(asyncResult, nameof(BeginFor));

#if !NET35
        public static Task ForAsync(int fromInclusive, int toExclusive, Action<int> body)
            => ForAsync<object?>(null, fromInclusive, toExclusive, null, body);
        
        public static Task ForAsync(int fromInclusive, int toExclusive, TaskConfig? asyncConfig, Action<int> body)
            => ForAsync<object?>(null, fromInclusive, toExclusive, asyncConfig, body);

        public static Task ForAsync<T>(T operation, int fromInclusive, int toExclusive, TaskConfig? asyncConfig, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return AsyncHelper.FromCompleted(asyncConfig);

            return AsyncHelper.DoOperationAsync(ctx => DoFor(ctx, operation, fromInclusive, toExclusive, body), asyncConfig);
        }
#endif

        public static void For<T>(IAsyncContext context, T operation, int fromInclusive, int toExclusive, Action<int> body)
        {
            if (body == null!)
                Throw.ArgumentNullException(Argument.body);
            if (fromInclusive >= toExclusive)
                return;

            DoFor(context, operation, fromInclusive, toExclusive, body);
        }

        #endregion

        #region Internal Methods

#if NET35
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Exceptions in pool threads must not be thrown in place but from the caller thread.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502: Avoid excessive complexity",
            Justification = "Special optimization for .NET 3.5 version where there is no Parallel.For")]
#endif
        private static void DoFor<T>(IAsyncContext context, T operation, int fromInclusive, int toExclusive, Action<int> body)
        {
            #region Local Methods
#if !NET35

            void DoWorkWithProgress(int y)
            {
                body.Invoke(y);
                context.Progress.Increment();
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
            context.Progress?.New(operation, count);

            // a single iteration: invoke once
            if (count == 1)
            {
                if (context.IsCancellationRequested)
                    return;

                body.Invoke(fromInclusive);
                context.Progress?.Increment();
                return;
            }

            // single core or no parallelism: sequential invoke
            if (CoreCount == 1 || context.MaxDegreeOfParallelism == 1)
            {
                for (int i = fromInclusive; i < toExclusive; i++)
                {
                    if (context.IsCancellationRequested)
                        return;
                    body.Invoke(i);
                    context.Progress?.Increment();
                }

                return;
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
                            context.Progress?.Increment();
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
                                context.Progress?.Increment();
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
                bodyWithState = context.Progress == null ? DoWorkWithCancellation : DoWorkWithCancellationAndProgress;
            else
                simpleBody = context.Progress == null ? body : DoWorkWithProgress;

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
                return;
            }

            // We merge some iterations to be processed by the same core
            // NOTE: We could use CreateRanges just like for .NET Framework 3.5 but even though it allocates less an uses value tuples,
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

                return;
            }

            Parallel.ForEach(ranges, options, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                    simpleBody!.Invoke(i);
            });
#endif
        }

        #endregion

        #region Private Methods
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
