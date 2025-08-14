﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParallelHelper.cs
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
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
#if !NET6_0_OR_GREATER
using System.Diagnostics;
#endif
#if NET35
using System.Diagnostics.CodeAnalysis;
#endif
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
#endif
#if NET35
using System.Runtime.ExceptionServices;
#endif
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
#if !NET6_0_OR_GREATER
using System.Security;
#endif
#if NET35
using System.Threading;
#else
using System.Threading.Tasks;
#endif

using KGySoft.Collections;
using KGySoft.CoreLibraries;

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
    /// Furthermore, it also contains several sorting methods that can sort <see cref="IList{T}"/> instances in place using multiple threads.
    /// </summary>
    public static partial class ParallelHelper
    {
        #region Constants

        private const int sortParallelThreshold = 16;

        #endregion

        #region Fields

#if !NET35
        private static readonly ParallelOptions defaultParallelOptions = new ParallelOptions();
#endif

        #endregion

        #region Properties

        internal static int CoreCount { get; } = GetCoreCount();
        internal static bool IsSingleCoreCpu => CoreCount == 1;

        #endregion

        #region Methods

        #region Public Methods

        #region For

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
        /// Executes an indexed loop using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
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
        /// on a pool thread. Degree of parallelism, the ability of cancellation and reporting progress depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
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

        #region Sort

        #region Sync

        #region DefaultContext

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> synchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<T>(IList<T> list, IComparer<T>? comparer = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.list);
            
            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);

            int count = list.Count;
            if (count < 2)
                return;

            DoSort(AsyncHelper.DefaultContext, list, 0, count, comparer);
        }

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> synchronously, potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="list"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<T>(IList<T> list, int index, int count, IComparer<T>? comparer = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.list);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > list.Count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return;

            DoSort(AsyncHelper.DefaultContext, list, index, count, comparer);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="ArraySection{T}"/> synchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <see cref="ArraySection{T}"/>.</typeparam>
        /// <param name="array">The <see cref="ArraySection{T}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<T>(ArraySection<T> array, IComparer<T>? comparer = null)
        {
            if (array.Length < 2)
                return;
            DoSort(AsyncHelper.DefaultContext, array, comparer);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="CastArray{TFrom,TTo}"/> synchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance.</typeparam>
        /// <param name="array">The <see cref="CastArray{TFrom,TTo}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<TFrom, TTo>(CastArray<TFrom, TTo> array, IComparer<TTo>? comparer = null)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (array.Length < 2)
                return;
            DoSort(AsyncHelper.DefaultContext, array, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/><paramref name="values"/> is not <see langword="null"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, IComparer<TKey>? comparer = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            Sort(keys, values, 0, keys.Count, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="keys"/> or <paramref name="values"/> list.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, int index, int count, IComparer<TKey>? comparer = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            if (values == null)
            {
                Sort(keys, index, count, comparer);
                return;
            }

            if (keys is IList { IsReadOnly: true } || keys is not IList && keys.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (values is IList { IsReadOnly: true } || values is not IList && values.IsReadOnly)
                Throw.ArgumentException(Argument.values, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (keys.Count - index < count || index > values.Count - count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return;

            DoSort(AsyncHelper.DefaultContext, keys, values, index, count, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="ArraySection{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> collection.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> collection.</typeparam>
        /// <param name="keys">The <see cref="ArraySection{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="ArraySection{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<TKey, TValue>(ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer = null)
        {
            if (values.IsNull)
            {
                Sort(keys, comparer);
                return;
            }

            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return;
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            DoSort(AsyncHelper.DefaultContext, keys, values, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="CastArray{TFrom,TTo}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKeyFrom">The actual element type of the underlying array in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TKeyTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TValueFrom">The actual element type of the underlying array in <paramref name="values"/>.</typeparam>
        /// <typeparam name="TValueTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="values"/>.</typeparam>
        /// <param name="keys">The <see cref="CastArray{TFrom,TTo}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="CastArray{TFrom,TTo}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <note>This method adjusts the degree of parallelization automatically, blocks the caller, and does not support cancellation.
        /// Use the overloads with a <see cref="ParallelConfig"/> parameter to adjust parallelization and to set up cancellation;
        /// or the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see>/<see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see>
        /// (in .NET Framework 4.0 and above) methods to perform the sorting asynchronously.</note>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static void Sort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer = null)
            where TKeyFrom : unmanaged
            where TKeyTo : unmanaged
            where TValueFrom : unmanaged
            where TValueTo : unmanaged
        {
            if (values.IsNull)
            {
                Sort(keys, comparer);
                return;
            }

            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return;
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            DoSort(AsyncHelper.DefaultContext, keys, values, comparer);
        }

        #endregion

        #region ParallelConfig

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> synchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<T>(IList<T> list, IComparer<T>? comparer, ParallelConfig? configuration)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.array);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);

            int count = list.Count;
            if (count < 2)
                return AsyncHelper.FromResult(true, configuration);

            return AsyncHelper.DoOperationSynchronously(ctx => DoSort(ctx, list, 0, count, comparer), configuration);
        }

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> synchronously, potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="list"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<T>(IList<T> list, int index, int count, IComparer<T>? comparer, ParallelConfig? configuration)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.list);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > list.Count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return AsyncHelper.FromResult(true, configuration);

            return AsyncHelper.DoOperationSynchronously(ctx => DoSort(ctx, list, index, count, comparer), configuration);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="ArraySection{T}"/> synchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <see cref="ArraySection{T}"/>.</typeparam>
        /// <param name="array">The <see cref="ArraySection{T}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<T>(ArraySection<T> array, IComparer<T>? comparer, ParallelConfig? configuration)
        {
            if (array.Length < 2)
                return AsyncHelper.FromResult(true, configuration);
            return AsyncHelper.DoOperationSynchronously(ctx => DoSort(ctx, array, comparer), configuration);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="CastArray{TFrom,TTo}"/> synchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance.</typeparam>
        /// <param name="array">The <see cref="CastArray{TFrom,TTo}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<TFrom, TTo>(CastArray<TFrom, TTo> array, IComparer<TTo>? comparer, ParallelConfig? configuration)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (array.Length < 2)
                return AsyncHelper.FromResult(true, configuration);
            return AsyncHelper.DoOperationSynchronously(ctx => DoSort(ctx, array, comparer), configuration);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/><paramref name="values"/> is not <see langword="null"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, IComparer<TKey>? comparer, ParallelConfig? configuration)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            return Sort(keys, values, 0, keys.Count, comparer, configuration);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="keys"/> or <paramref name="values"/> list.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, int index, int count, IComparer<TKey>? comparer, ParallelConfig? configuration)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            if (values == null)
                return Sort(keys, index, count, comparer, configuration);

            if (keys is IList { IsReadOnly: true } || keys is not IList && keys.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (values is IList { IsReadOnly: true } || values is not IList && values.IsReadOnly)
                Throw.ArgumentException(Argument.values, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (keys.Count - index < count || index > values.Count - count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return AsyncHelper.FromResult(true, configuration);

            return AsyncHelper.DoOperationSynchronously(ctx => DoSort(ctx, keys, values, index, count, comparer), configuration);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="ArraySection{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> collection.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> collection.</typeparam>
        /// <param name="keys">The <see cref="ArraySection{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="ArraySection{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">The <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<TKey, TValue>(ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer, ParallelConfig? configuration)
        {
            if (values.IsNull)
                return Sort(keys, comparer, configuration);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return AsyncHelper.FromResult(true, configuration);
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return AsyncHelper.DoOperationSynchronously(ctx => DoSort(ctx, keys, values, comparer), configuration);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="CastArray{TFrom,TTo}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKeyFrom">The actual element type of the underlying array in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TKeyTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TValueFrom">The actual element type of the underlying array in <paramref name="values"/>.</typeparam>
        /// <typeparam name="TValueTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="values"/>.</typeparam>
        /// <param name="keys">The <see cref="CastArray{TFrom,TTo}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="CastArray{TFrom,TTo}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer.</param>
        /// <param name="configuration">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">The <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="configuration"/> was <see langword="true"/>.</exception>
        public static bool Sort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer, ParallelConfig? configuration)
            where TKeyFrom : unmanaged
            where TKeyTo : unmanaged
            where TValueFrom : unmanaged
            where TValueTo : unmanaged
        {
            if (values.IsNull)
                return Sort(keys, comparer, configuration);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return AsyncHelper.FromResult(true, configuration);
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return AsyncHelper.DoOperationSynchronously(ctx => DoSort(ctx, keys, values, comparer), configuration);
        }

        #endregion

        #region IAsyncContect

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> synchronously, potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<T>(IAsyncContext? context, IList<T> list, IComparer<T>? comparer = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.array);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);

            int count = list.Count;
            if (count < 2)
                return true;

            return DoSort(context ?? AsyncHelper.DefaultContext, list, 0, count, comparer);
        }

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> synchronously, potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="list">The list to sort.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="list"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<T>(IAsyncContext? context, IList<T> list, int index, int count, IComparer<T>? comparer = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.list);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > list.Count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return true;

            return DoSort(context ?? AsyncHelper.DefaultContext, list, index, count, comparer);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="ArraySection{T}"/> synchronously, potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <see cref="ArraySection{T}"/>.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="array">The <see cref="ArraySection{T}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<T>(IAsyncContext? context, ArraySection<T> array, IComparer<T>? comparer = null)
        {
            if (array.Length < 2)
                return true;
            return DoSort(context ?? AsyncHelper.DefaultContext, array, comparer);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="CastArray{TFrom,TTo}"/> synchronously, potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="array">The <see cref="CastArray{TFrom,TTo}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<TFrom, TTo>(IAsyncContext? context, CastArray<TFrom, TTo> array, IComparer<TTo>? comparer = null)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (array.Length < 2)
                return true;
            return DoSort(context ?? AsyncHelper.DefaultContext, array, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/><paramref name="values"/> is not <see langword="null"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<TKey, TValue>(IAsyncContext? context, IList<TKey> keys, IList<TValue>? values, IComparer<TKey>? comparer = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            return Sort(context, keys, values, 0, keys.Count, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="keys"/> or <paramref name="values"/> list.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<TKey, TValue>(IAsyncContext? context, IList<TKey> keys, IList<TValue>? values, int index, int count, IComparer<TKey>? comparer = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            if (values == null)
                return Sort(context, keys, index, count, comparer);

            if (keys is IList { IsReadOnly: true } || keys is not IList && keys.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (values is IList { IsReadOnly: true } || values is not IList && values.IsReadOnly)
                Throw.ArgumentException(Argument.values, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (keys.Count - index < count || index > values.Count - count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return true;

            return DoSort(context ?? AsyncHelper.DefaultContext, keys, values, index, count, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="ArraySection{T}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> collection.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> collection.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="keys">The <see cref="ArraySection{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="ArraySection{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<TKey, TValue>(IAsyncContext? context, ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer = null)
        {
            if (values.IsNull)
                return Sort(context, keys, comparer);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return true;
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return DoSort(context ?? AsyncHelper.DefaultContext, keys, values, comparer);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="CastArray{TFrom,TTo}"/> instances synchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads,
        /// using a <paramref name="context"/> that may belong to a higher level, possibly asynchronous operation.
        /// </summary>
        /// <typeparam name="TKeyFrom">The actual element type of the underlying array in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TKeyTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TValueFrom">The actual element type of the underlying array in <paramref name="values"/>.</typeparam>
        /// <typeparam name="TValueTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="values"/>.</typeparam>
        /// <param name="context">An <see cref="IAsyncContext"/> instance that contains information for asynchronous processing about the current operation.</param>
        /// <param name="keys">The <see cref="CastArray{TFrom,TTo}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="CastArray{TFrom,TTo}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled.</returns>
        /// <remarks>
        /// <para>This method blocks the caller thread but if <paramref name="context"/> belongs to an async top level method, then the execution may already run
        /// on a pool thread. Degree of parallelism and the ability of cancellation depend on how these were configured at the top level method.
        /// To reconfigure the degree of parallelism of an existing context, you can use the <see cref="AsyncContextWrapper"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool Sort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IAsyncContext? context, CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer = null)
            where TKeyFrom : unmanaged
            where TKeyTo : unmanaged
            where TValueFrom : unmanaged
            where TValueTo : unmanaged
        {
            if (values.IsNull)
                return Sort(context, keys, comparer);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return true;
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return DoSort(context ?? AsyncHelper.DefaultContext, keys, values, comparer);
        }

        #endregion

        #endregion

        #region Async APM

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> asynchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.</exception>
        public static IAsyncResult BeginSort<T>(IList<T> list, IComparer<T>? comparer = null, AsyncConfig? asyncConfig = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.array);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);

            int count = list.Count;
            if (count < 2)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.BeginOperation(ctx => DoSort(ctx, list, 0, count, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> asynchronously, potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="list"/>.</exception>
        public static IAsyncResult BeginSort<T>(IList<T> list, int index, int count, IComparer<T>? comparer = null, AsyncConfig? asyncConfig = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.list);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > list.Count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.BeginOperation(ctx => DoSort(ctx, list, index, count, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="ArraySection{T}"/> asynchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <see cref="ArraySection{T}"/>.</typeparam>
        /// <param name="array">The <see cref="ArraySection{T}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        public static IAsyncResult BeginSort<T>(ArraySection<T> array, IComparer<T>? comparer = null, AsyncConfig? asyncConfig = null)
        {
            if (array.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            return AsyncHelper.BeginOperation(ctx => DoSort(ctx, array, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="CastArray{TFrom,TTo}"/> asynchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance.</typeparam>
        /// <param name="array">The <see cref="CastArray{TFrom,TTo}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        public static IAsyncResult BeginSort<TFrom, TTo>(CastArray<TFrom, TTo> array, IComparer<TTo>? comparer = null, AsyncConfig? asyncConfig = null)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (array.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            return AsyncHelper.BeginOperation(ctx => DoSort(ctx, array, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/><paramref name="values"/> is not <see langword="null"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.</exception>
        public static IAsyncResult BeginSort<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, IComparer<TKey>? comparer = null, AsyncConfig? asyncConfig = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            return BeginSort(keys, values, 0, keys.Count, comparer, asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="keys"/> or <paramref name="values"/> list.</exception>
        public static IAsyncResult BeginSort<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, int index, int count, IComparer<TKey>? comparer = null, AsyncConfig? asyncConfig = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            if (values == null)
                return BeginSort(keys, index, count, comparer, asyncConfig);

            if (keys is IList { IsReadOnly: true } || keys is not IList && keys.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (values is IList { IsReadOnly: true } || values is not IList && values.IsReadOnly)
                Throw.ArgumentException(Argument.values, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (keys.Count - index < count || index > values.Count - count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.BeginOperation(ctx => DoSort(ctx, keys, values, index, count, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="ArraySection{T}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> collection.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> collection.</typeparam>
        /// <param name="keys">The <see cref="ArraySection{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="ArraySection{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.</exception>
        public static IAsyncResult BeginSort<TKey, TValue>(ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer = null, AsyncConfig? asyncConfig = null)
        {
            if (values.IsNull)
                return BeginSort(keys, comparer, asyncConfig);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return AsyncHelper.BeginOperation(ctx => DoSort(ctx, keys, values, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="CastArray{TFrom,TTo}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKeyFrom">The actual element type of the underlying array in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TKeyTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TValueFrom">The actual element type of the underlying array in <paramref name="values"/>.</typeparam>
        /// <typeparam name="TValueTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="values"/>.</typeparam>
        /// <param name="keys">The <see cref="CastArray{TFrom,TTo}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="CastArray{TFrom,TTo}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An <see cref="IAsyncResult"/> that represents the asynchronous operation, which could still be pending.</returns>
        /// <remarks>
        /// <para>In .NET Framework 4.0 and above you can use also the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods.</para>
        /// <para>To get the result or the exception that occurred during the operation you have to call the <see cref="EndSort">EndSort</see> method.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.</exception>
        public static IAsyncResult BeginSort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer = null, AsyncConfig? asyncConfig = null)
            where TKeyFrom : unmanaged
            where TKeyTo : unmanaged
            where TValueFrom : unmanaged
            where TValueTo : unmanaged
        {
            if (values.IsNull)
                return BeginSort(keys, comparer, asyncConfig);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return AsyncHelper.BeginOperation(ctx => DoSort(ctx, keys, values, comparer), asyncConfig);
        }

        /// <summary>
        /// Waits for the pending asynchronous operation started by one of the <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see> methods to complete.
        /// In .NET Framework 4.0 and above you can use the <see cref="O:KGySoft.Threading.ParallelHelper.SortAsync">SortAsync</see> methods instead.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns><see langword="true"/>, if the operation completed successfully.
        /// <br/><see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in the <c>asyncConfig</c> parameter was set to <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncResult"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="asyncResult"/> was not returned by a <see cref="O:KGySoft.Threading.ParallelHelper.BeginSort">BeginSort</see> overload or
        /// this method was called with the same instance multiple times.</exception>
        /// <exception cref="OperationCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in the <c>asyncConfig</c> parameter was <see langword="true"/>.</exception>
        /// <exception cref="ArgumentException">The <see cref="IComparer{T}"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"> The <see cref="IComparer{T}"/> instance was <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        public static bool EndSort(IAsyncResult asyncResult) => AsyncHelper.EndOperation<bool>(asyncResult, nameof(BeginSort));

        #endregion

        #region Async TPL
#if !NET35

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> asynchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<T>(IList<T> list, IComparer<T>? comparer = null, TaskConfig? asyncConfig = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.array);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);

            int count = list.Count;
            if (count < 2)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.DoOperationAsync(ctx => DoSort(ctx, list, 0, count, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements of the specified <paramref name="list"/> asynchronously, potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="list"/>.</typeparam>
        /// <param name="list">The list to sort.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances.
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="list"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<T>(IList<T> list, int index, int count, IComparer<T>? comparer = null, TaskConfig? asyncConfig = null)
        {
            if (list == null!)
                Throw.ArgumentNullException(Argument.list);

            // If implements the non-generic IList, checking its IsReadOnly, which can be false for array-like fixed-size collections
            if (list is IList { IsReadOnly: true } || list is not IList && list.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (index + count > list.Count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.DoOperationAsync(ctx => DoSort(ctx, list, index, count, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="ArraySection{T}"/> asynchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <see cref="ArraySection{T}"/>.</typeparam>
        /// <param name="array">The <see cref="ArraySection{T}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<T>(ArraySection<T> array, IComparer<T>? comparer = null, TaskConfig? asyncConfig = null)
        {
            if (array.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            return AsyncHelper.DoOperationAsync(ctx => DoSort(ctx, array, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements of the specified <see cref="CastArray{TFrom,TTo}"/> asynchronously, potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TFrom">The actual element type of the underlying array.</typeparam>
        /// <typeparam name="TTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance.</typeparam>
        /// <param name="array">The <see cref="CastArray{TFrom,TTo}"/> instance to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<TFrom, TTo>(CastArray<TFrom, TTo> array, IComparer<TTo>? comparer = null, TaskConfig? asyncConfig = null)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (array.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            return AsyncHelper.DoOperationAsync(ctx => DoSort(ctx, array, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/><paramref name="values"/> is not <see langword="null"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, IComparer<TKey>? comparer = null, TaskConfig? asyncConfig = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            return SortAsync(keys, values, 0, keys.Count, comparer, asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="IList{T}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// The range of elements to sort is specified by a starting index and a length.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> list.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> list.</typeparam>
        /// <param name="keys">The <see cref="IList{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="IList{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> list, or <see langword="null"/> to sort only the keys.</param>
        /// <param name="index">The zero-based starting index of the range to sort.</param>
        /// <param name="count">The length of the range to sort.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>In general, accessing <see cref="IList{T}"/> members is like calling virtual methods. For the best performance there is a special handling for <see cref="Array"/>,
        /// <see cref="List{T}"/>, <see cref="ArraySegment{T}"/>, <see cref="ArraySection{T}"/> and <see cref="CircularList{T}"/> instances,
        /// if both <paramref name="keys"/> and <paramref name="values"/> are of the same type (not considering the generic type arguments).
        /// For <see cref="ArraySection{T}"/> and <see cref="CastArray{TFrom,TTo}"/> instances it is recommended to use the dedicated overloads for better performance.</para>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="keys"/> or <paramref name="values"/> is read-only.
        /// <br/>-or-
        /// <br/>The <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <paramref name="keys"/> or <paramref name="values"/> list.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<TKey, TValue>(IList<TKey> keys, IList<TValue>? values, int index, int count, IComparer<TKey>? comparer = null, TaskConfig? asyncConfig = null)
        {
            if (keys == null!)
                Throw.ArgumentNullException(Argument.keys);
            if (values == null)
                return SortAsync(keys, index, count, comparer, asyncConfig);

            if (keys is IList { IsReadOnly: true } || keys is not IList && keys.IsReadOnly)
                Throw.ArgumentException(Argument.list, Res.ICollectionReadOnlyModifyNotSupported);
            if (values is IList { IsReadOnly: true } || values is not IList && values.IsReadOnly)
                Throw.ArgumentException(Argument.values, Res.ICollectionReadOnlyModifyNotSupported);
            if (index < 0)
                Throw.ArgumentOutOfRangeException(Argument.index);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);
            if (keys.Count - index < count || index > values.Count - count)
                Throw.ArgumentException(Res.IListInvalidOffsLen);

            if (count < 2)
                return AsyncHelper.FromResult(true, asyncConfig);

            return AsyncHelper.DoOperationAsync(ctx => DoSort(ctx, keys, values, index, count, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="ArraySection{T}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKey">The type of the elements in the <paramref name="keys"/> collection.</typeparam>
        /// <typeparam name="TValue">The type of the elements in the <paramref name="values"/> collection.</typeparam>
        /// <param name="keys">The <see cref="ArraySection{T}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="ArraySection{T}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="ArraySection{T}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<TKey, TValue>(ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer = null, TaskConfig? asyncConfig = null)
        {
            if (values.IsNull)
                return SortAsync(keys, comparer, asyncConfig);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return AsyncHelper.DoOperationAsync(ctx => DoSort(ctx, keys, values, comparer), asyncConfig);
        }

        /// <summary>
        /// Sorts the elements in a pair of <see cref="CastArray{TFrom,TTo}"/> instances asynchronously (one contains the keys, the other contains the corresponding values), potentially using multiple threads.
        /// </summary>
        /// <typeparam name="TKeyFrom">The actual element type of the underlying array in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TKeyTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="keys"/>.</typeparam>
        /// <typeparam name="TValueFrom">The actual element type of the underlying array in <paramref name="values"/>.</typeparam>
        /// <typeparam name="TValueTo">The reinterpreted element type of the <see cref="CastArray{TFrom,TTo}"/> instance in <paramref name="values"/>.</typeparam>
        /// <param name="keys">The <see cref="CastArray{TFrom,TTo}"/> that contains the keys to sort.</param>
        /// <param name="values">The <see cref="CastArray{TFrom,TTo}"/> that contains the values that correspond to the keys in the <paramref name="keys"/> collection.
        /// If the <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="true"/>, then only the keys are sorted.</param>
        /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use a default comparer. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="asyncConfig">An optional configuration to adjust parallelization or cancellation. Reporting progress is not supported in sorting methods. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A task that represents the asynchronous operation. Its result is <see langword="true"/>, if the operation completed successfully,
        /// or <see langword="false"/>, if the operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/> in <paramref name="asyncConfig"/> parameter was <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method is not a blocking call even if the <see cref="AsyncConfigBase.MaxDegreeOfParallelism"/> property of the <paramref name="asyncConfig"/> parameter is 1.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="CastArray{TFrom,TTo}.IsNull"/> property of <paramref name="values"/> is <see langword="false"/> and <paramref name="values"/> has fewer elements than <paramref name="keys"/>.
        /// <br/>-or-
        /// <br/>The <paramref name="comparer"/> returned inconsistent results.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="comparer"/> is <see langword="null"/>, and an element does not implement the <see cref="IComparable{T}"/> interface.</exception>
        /// <exception cref="TaskCanceledException">The operation has been canceled and <see cref="AsyncConfigBase.ThrowIfCanceled"/>
        /// in <paramref name="asyncConfig"/> was <see langword="true"/>. This exception is thrown when the result is awaited.</exception>
        public static Task<bool> SortAsync<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer = null, TaskConfig? asyncConfig = null)
            where TKeyFrom : unmanaged
            where TKeyTo : unmanaged
            where TValueFrom : unmanaged
            where TValueTo : unmanaged
        {
            if (values.IsNull)
                return SortAsync(keys, comparer, asyncConfig);
            if (keys.Length > values.Length)
                Throw.ArgumentException(Res.IListInvalidOffsLen);
            if (keys.Length < 2)
                return AsyncHelper.FromResult(true, asyncConfig);
            if (keys.Length < values.Length)
                values = values.Slice(0, keys.Length);

            return AsyncHelper.DoOperationAsync(ctx => DoSort(ctx, keys, values, comparer), asyncConfig);
        }

#endif
#endregion

        #endregion

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
                    ThreadPool.UnsafeQueueUserWorkItem(_ =>
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
                    }, null);
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

        private static bool DoSort<T>(IAsyncContext context, IList<T> list, int startIndex, int count, IComparer<T>? comparer)
        {
            Debug.Assert(list.Count > 1);

            // For reference, the result of the performance tests for the IList special handling cases (sorting 10_000_000 items):
            // =[SortPerformanceTest<Int32> (.NET Core 9.0.0) Results]================================================
            // Test Time: 5 000 ms
            // Warming up: Yes
            // Test cases: 10
            // Repeats: 3
            // Calling GC.Collect: Yes
            // Forced CPU Affinity: No
            // Cases are sorted by fulfilled iterations (the most first)
            // --------------------------------------------------
            // 1. ParallelHelper.Sort(CastArray<byte, Int32>) (8 threads): 33 iterations in 15 909,28 ms. Adjusted for 5 000 ms: 10,37
            //   #1  11 iterations in 5 312,41 ms. Adjusted: 10,35
            //   #2  11 iterations in 5 384,39 ms. Adjusted: 10,21	 <---- Worst
            //   #3  11 iterations in 5 212,48 ms. Adjusted: 10,55	 <---- Best
            //   Worst-Best difference: 0,34 (3,30%)
            // 2. ParallelHelper.Sort(CastArray<Int32, Int32>) (8 threads): 33 iterations in 15 921,45 ms. Adjusted for 5 000 ms: 10,36 (-0,01 / 99,91%)
            //   #1  11 iterations in 5 324,17 ms. Adjusted: 10,33
            //   #2  11 iterations in 5 334,38 ms. Adjusted: 10,31	 <---- Worst
            //   #3  11 iterations in 5 262,91 ms. Adjusted: 10,45	 <---- Best
            //   Worst-Best difference: 0,14 (1,36%)
            // 3. ParallelHelper.Sort(ArraySection) (8 threads): 30 iterations in 15 523,24 ms. Adjusted for 5 000 ms: 9,66 (-0,71 / 93,15%)
            //   #1  10 iterations in 5 200,39 ms. Adjusted: 9,61	 <---- Worst
            //   #2  10 iterations in 5 158,40 ms. Adjusted: 9,69	 <---- Best
            //   #3  10 iterations in 5 164,45 ms. Adjusted: 9,68
            //   Worst-Best difference: 0,08 (0,81%)
            // 4. ParallelHelper.Sort(T[]) (8 threads): 30 iterations in 15 709,10 ms. Adjusted for 5 000 ms: 9,55 (-0,82 / 92,07%)
            //   #1  10 iterations in 5 182,84 ms. Adjusted: 9,65
            //   #2  10 iterations in 5 351,91 ms. Adjusted: 9,34	 <---- Worst
            //   #3  10 iterations in 5 174,34 ms. Adjusted: 9,66	 <---- Best
            //   Worst-Best difference: 0,32 (3,43%)
            // 5. ParallelHelper.Sort(List<T>) (8 threads): 21 iterations in 15 367,65 ms. Adjusted for 5 000 ms: 6,83 (-3,54 / 65,87%)
            //   #1  7 iterations in 5 131,20 ms. Adjusted: 6,82
            //   #2  7 iterations in 5 102,99 ms. Adjusted: 6,86	 <---- Best
            //   #3  7 iterations in 5 133,46 ms. Adjusted: 6,82	 <---- Worst
            //   Worst-Best difference: 0,04 (0,60%)
            // 6. ParallelHelper.Sort(T[]) (2 threads): 21 iterations in 15 373,99 ms. Adjusted for 5 000 ms: 6,83 (-3,54 / 65,84%)
            //   #1  7 iterations in 5 104,71 ms. Adjusted: 6,86	 <---- Best
            //   #2  7 iterations in 5 126,63 ms. Adjusted: 6,83
            //   #3  7 iterations in 5 142,66 ms. Adjusted: 6,81	 <---- Worst
            //   Worst-Best difference: 0,05 (0,74%)
            // 7. ParallelHelper.Sort(ArraySection) (2 threads): 21 iterations in 15 414,39 ms. Adjusted for 5 000 ms: 6,81 (-3,56 / 65,67%)
            //   #1  7 iterations in 5 116,86 ms. Adjusted: 6,84	 <---- Best
            //   #2  7 iterations in 5 120,20 ms. Adjusted: 6,84
            //   #3  7 iterations in 5 177,33 ms. Adjusted: 6,76	 <---- Worst
            //   Worst-Best difference: 0,08 (1,18%)
            // 8. ParallelHelper.Sort(CastArray as IList) (8 threads): 21 iterations in 16 227,64 ms. Adjusted for 5 000 ms: 6,47 (-3,90 / 62,38%)
            //   #1  7 iterations in 5 425,92 ms. Adjusted: 6,45	 <---- Worst
            //   #2  7 iterations in 5 415,67 ms. Adjusted: 6,46
            //   #3  7 iterations in 5 386,05 ms. Adjusted: 6,50	 <---- Best
            //   Worst-Best difference: 0,05 (0,74%)
            // 9. Array.Sort: 20 iterations in 16 657,91 ms. Adjusted for 5 000 ms: 6,00 (-4,37 / 57,83%)
            //   #1  7 iterations in 5 809,20 ms. Adjusted: 6,02
            //   #2  6 iterations in 5 102,40 ms. Adjusted: 5,88	 <---- Worst
            //   #3  7 iterations in 5 746,31 ms. Adjusted: 6,09	 <---- Best
            //   Worst-Best difference: 0,21 (3,59%)
            // 10. ParallelHelper.Sort(T[]) (single thread): 16 iterations in 15 245,16 ms. Adjusted for 5 000 ms: 5,25 (-5,12 / 50,62%)
            //   #1  4 iterations in 5 106,41 ms. Adjusted: 3,92	 <---- Worst
            //   #2  6 iterations in 5 077,82 ms. Adjusted: 5,91
            //   #3  6 iterations in 5 060,93 ms. Adjusted: 5,93	 <---- Best
            //   Worst-Best difference: 2,01 (51,35%)
            bool isSingleThreadNotCancellable = !context.CanBeCanceled && (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1);

            switch (list)
            {
                case T[] array:
                    if (isSingleThreadNotCancellable)
                        Array.Sort(array, startIndex, count, comparer);
                    else
                        SortHelper<T>.Instance.Sort(context, array, startIndex, count, comparer);
                    break;

                case List<T> genericList:
                    // List<T>: multithreaded sorting is getting faster only from 4 cores. Unfortunately there is no public API to get
                    // the underlying array, and CollectionMarshal.AsSpan cannot be used because spans cannot be passed to other threads.
                    if (isSingleThreadNotCancellable || context.MaxDegreeOfParallelism is >= 1 and < 4)
                        genericList.Sort(startIndex, count, comparer);
                    else
                        SortHelper<T>.Instance.Sort(context, genericList, startIndex, count, comparer);
                    break;

                case ArraySection<T> arraySection:
                    if (isSingleThreadNotCancellable)
                        Array.Sort(arraySection.UnderlyingArray!, startIndex + arraySection.Offset, count, comparer);
                    else
                        SortHelper<T>.Instance.Sort(context, arraySection.UnderlyingArray!, startIndex + arraySection.Offset, count, comparer);

                    break;

#if !NET35
                case ArraySegment<T> arraySection:
                    if (isSingleThreadNotCancellable)
                        Array.Sort(arraySection.Array!, startIndex + arraySection.Offset, count, comparer);
                    else
                        SortHelper<T>.Instance.Sort(context, arraySection.Array!, startIndex + arraySection.Offset, count, comparer);

                    break;
#endif

                case CircularList<T> circularList:
                    if (isSingleThreadNotCancellable)
                        circularList.Sort(startIndex, count, comparer);
                    else
                    {
                        ArraySection<T> section = circularList.GetSectionToSort(startIndex, count);
                        SortHelper<T>.Instance.Sort(context, section.UnderlyingArray!, section.Offset, count, comparer);
                    }

                    break;

                default:
                    // From here the slow fallback path for IList<T> with virtual calls
#if NET11_0_OR_GREATER // TODO: https://github.com/dotnet/runtime/issues/76375 - only if the fallback is not implemented by copying the elements to a new array, and then back
                    if (isSingleThread)
                    {
                        CollectionExtensions.Sort(list, startIndex, count, comparer);
                        break;
                    }
#endif
                    SortHelper<T>.Instance.Sort(context, list, startIndex, count, comparer);
                    break;
            }

            return !context.IsCancellationRequested;
        }

        private static bool DoSort<T>(IAsyncContext context, ArraySection<T> array, IComparer<T>? comparer)
        {
            Debug.Assert(array.Length > 1);
            if (!context.CanBeCanceled && (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1))
                Array.Sort(array.UnderlyingArray!, array.Offset, array.Length, comparer);
            else
                SortHelper<T>.Instance.Sort(context, array.UnderlyingArray!, array.Offset, array.Length, comparer);
            return !context.IsCancellationRequested;
        }

        private static bool DoSort<TFrom, TTo>(IAsyncContext context, CastArray<TFrom, TTo> array, IComparer<TTo>? comparer)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            Debug.Assert(array.Length > 1);
#if NET5_0_OR_GREATER
            if (!context.CanBeCanceled && (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1))
                array.AsSpan.Sort(comparer);
            else
#endif
            {
                SortHelper<TTo>.Instance.Sort(context, array, comparer);
            }
            return !context.IsCancellationRequested;
        }

        private static bool DoSort<TKey, TValue>(IAsyncContext context, IList<TKey> keys, IList<TValue> values, int startIndex, int count, IComparer<TKey>? comparer)
        {
            Debug.Assert(keys.Count > startIndex + 1 && values.Count >= keys.Count);

            bool isSingleThreadNotCancellable = !context.CanBeCanceled && (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1);

            switch ((keys, values))
            {
                case (TKey[] keysArray, TValue[] valuesArray):
                    if (isSingleThreadNotCancellable)
                        Array.Sort(keysArray, valuesArray, startIndex, count, comparer);
                    else
                        SortHelper<TKey, TValue>.Instance.Sort(context, keysArray, valuesArray, startIndex, count, comparer);
                    break;

                case (List<TKey> keysList, List<TValue> valuesList):
                    // List<T>: multithreaded sorting is getting faster only from 4 cores. Unfortunately there is no public API to get
                    // the underlying array, and CollectionMarshal.AsSpan cannot be used for multithreaded sorting because spans cannot be passed to other threads.
#if NET5_0_OR_GREATER
                    if (isSingleThreadNotCancellable || context.MaxDegreeOfParallelism is >= 1 and < 4)
                        CollectionsMarshal.AsSpan(keysList).Slice(startIndex, count).Sort(CollectionsMarshal.AsSpan(valuesList).Slice(startIndex, count), comparer);
                    else
#endif
                    {
                        SortHelper<TKey, TValue>.Instance.Sort(context, keysList, valuesList, startIndex, count, comparer);
                    }
                    break;

                case (ArraySection<TKey> keysSection, ArraySection<TValue> valuesSection):
#if NET5_0_OR_GREATER
                    if (isSingleThreadNotCancellable)
                        keysSection.AsSpan.Sort(valuesSection.AsSpan, comparer);
#else
                    if (isSingleThreadNotCancellable && keysSection.Offset == valuesSection.Offset)
                        Array.Sort(keysSection.UnderlyingArray!, valuesSection.UnderlyingArray, startIndex + keysSection.Offset, count, comparer);
#endif
                    else
                        SortHelper<TKey, TValue>.Instance.Sort(context, keysSection, valuesSection.Slice(startIndex, count), comparer);

                    break;

#if !NET35
                case (ArraySegment<TKey> keysSection, ArraySegment<TValue> valuesSection):
#if NET5_0_OR_GREATER
                    if (isSingleThreadNotCancellable)
                        keysSection.AsSpan().Sort(valuesSection.AsSpan(), comparer);
#else
                    if (isSingleThreadNotCancellable && keysSection.Offset == valuesSection.Offset)
                        Array.Sort(keysSection.Array!, valuesSection.Array, startIndex + keysSection.Offset, count, comparer);
#endif
                    else
                        SortHelper<TKey, TValue>.Instance.Sort(context, keysSection.AsSection(), valuesSection.AsSection().Slice(startIndex, count), comparer);

                    break;
#endif

                case (CircularList<TKey> keysCircularList, CircularList<TValue> valuesCircularList):
                    ArraySection<TKey> keysCListSection = keysCircularList.GetSectionToSort(startIndex, count);
                    ArraySection<TValue> valuesCListSection = valuesCircularList.GetSectionToSort(startIndex, count);
#if NET5_0_OR_GREATER
                    if (isSingleThreadNotCancellable)
                        keysCListSection.AsSpan.Sort(valuesCListSection.AsSpan, comparer);
#else
                    if (isSingleThreadNotCancellable && keysCListSection.Offset == valuesCListSection.Offset)
                        Array.Sort(keysCListSection.UnderlyingArray!, valuesCListSection.UnderlyingArray, startIndex + keysCListSection.Offset, count, comparer);
#endif
                    else
                        SortHelper<TKey, TValue>.Instance.Sort(context, keysCListSection, valuesCListSection.Slice(startIndex, count), comparer);

                    break;

                default:
                    // From here the slow fallback path for IList<T> with virtual calls
#if NET11_0_OR_GREATER // TODO: https://github.com/dotnet/runtime/issues/76375 - only if the fallback is not implemented by copying the elements to a new array, and then back
                    if (isSingleThread)
                    {
                        CollectionExtensions.Sort(list, keys, arrays, startIndex, count, comparer);
                        break;
                    }
#endif
                    SortHelper<TKey, TValue>.Instance.Sort(context, keys, values, startIndex, count, comparer);
                    break;
            }

            return !context.IsCancellationRequested;
        }

        private static bool DoSort<TKey, TValue>(IAsyncContext context, ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer)
        {
            Debug.Assert(keys.Length > 1 && values.Length >= keys.Length);
#if NET5_0_OR_GREATER
            if (!context.CanBeCanceled && (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1))
                keys.AsSpan.Sort(values.AsSpan, comparer);
#else
            if (!context.CanBeCanceled && (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1) && keys.Offset == values.Offset)
                Array.Sort(keys.UnderlyingArray!, values.UnderlyingArray, keys.Offset, keys.Length, comparer);
#endif
            else
                SortHelper<TKey, TValue>.Instance.Sort(context, keys, values.Slice(0, keys.Length), comparer);
            return !context.IsCancellationRequested;
        }

        private static bool DoSort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IAsyncContext context, CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer)
            where TKeyFrom : unmanaged
            where TKeyTo : unmanaged
            where TValueFrom : unmanaged
            where TValueTo : unmanaged
        {
            Debug.Assert(keys.Length > 1 && values.Length >= keys.Length);
#if NET5_0_OR_GREATER
            if (!context.CanBeCanceled && (IsSingleCoreCpu || context.MaxDegreeOfParallelism == 1))
                keys.AsSpan.Sort(values.AsSpan, comparer);
            else
#endif
            {
                SortHelper<TKeyTo, TValueTo>.Instance.Sort(context, keys, values.Slice(0, keys.Length), comparer);
            }

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

#if NET6_0_OR_GREATER
        private static int GetCoreCount() => Environment.ProcessorCount;
#else
        [SecuritySafeCritical]
        private static int GetCoreCount()
        {
            if (!EnvironmentHelper.IsWindows)
                return Environment.ProcessorCount;

            // Here we are on Windows, targeting .NET 5 or earlier, where Environment.ProcessorCount doesn't respect affinity or CPU limit:
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/environment-processorcount-on-windows

            try
            {
                // We check if DOTNET_PROCESSOR_COUNT is set because it has a priority over any other settings
                string? var = Environment.GetEnvironmentVariable("DOTNET_PROCESSOR_COUNT");
                if (var is not null && Int32.TryParse(var, out int result))
                    return result;

                // Using CPU affinity
                // NOTE: Unlike the latest Environment.ProcessorCount implementations, not checking if multiple CPU groups are available
                // because it's supported on Windows 11+ only, which was released after .NET 5 anyway.
                nint affinity = Process.GetCurrentProcess().ProcessorAffinity;
                return affinity == 0 ? Environment.ProcessorCount : ((ulong)affinity).GetFlagsCount();
            }
            catch (Exception e) when (!e.IsCritical())
            {
                return Environment.ProcessorCount;
            }
        }
#endif

#if NETCOREAPP3_0_OR_GREATER
        private static int Log2(int value) => BitOperations.Log2((uint)value);
#else
        private static int Log2(int value) => (int)Math.Ceiling(Math.Log(value, 2));
#endif

        #endregion

        #endregion
    }
}
