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
using System.Diagnostics.CodeAnalysis;
using System.Threading; 
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
#else
using System.Collections.Concurrent;
using System.Threading.Tasks;
#endif

#endregion

namespace KGySoft.Threading
{
#if NET35
#pragma warning disable CS1574 // XML comment has references that cannot not be resolved on all platforms
#endif

    internal static class ParallelHelper
    {
        #region Fields

        private static int? coreCount;

#if !NET35
        private static readonly ParallelOptions defaultParallelOptions = new ParallelOptions();
#endif

        #endregion

        #region Properties

        internal static int CoreCount => coreCount ??= Environment.ProcessorCount;

        #endregion

        #region Methods

        #region Internal Methods

        /// <summary>
        /// Similar to <see cref="Parallel.For(int,int,Action{int})"/> but tries to balance resources and works also in .NET 3.5.
        /// </summary>
#if NET35
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Exceptions in pool threads must not be thrown in place but from the caller thread.")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502: Avoid excessive complexity",
            Justification = "Special optimization for .NET 3.5 version where there is no Parallel.For")]
#endif
        internal static void For<T>(IAsyncContext context, T operation, int fromInclusive, int toExclusive, Action<int> body)
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

            int count = toExclusive - fromInclusive;
            context.Progress?.New(operation, Math.Max(count, 0));

            // a single iteration: invoke once
            if (count <= 1)
            {
                if (count < 1 || context.IsCancellationRequested)
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
                // we allow a bit more fine resolution than the actual core counts
                rangeSize = (count / CoreCount) >> 2;
                options = defaultParallelOptions;
            }
            else
            {
                // we allow a bit more fine resolution than the specified degree
                rangeSize = (count / context.MaxDegreeOfParallelism) >> 2;
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

            // we merge some iterations to be processed by the same core
            var partitions = Partitioner.Create(fromInclusive, toExclusive, rangeSize);
            if (bodyWithState != null)
            {
                Parallel.ForEach(partitions, options, (range, state) =>
                {
                    (int from, int to) = range;
                    for (int i = from; i < to; i++)
                    {
                        bodyWithState.Invoke(i, state);
                        if (state.IsStopped)
                            return;
                    }
                });

                return;
            }

            Parallel.ForEach(partitions, options, range =>
            {
                (int from, int to) = range;
                for (int i = from; i < to; i++)
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
                yield return (i, Math.Min(toExclusive, i + rangeSize));
        }

#endif
        #endregion

        #endregion
    }
}
