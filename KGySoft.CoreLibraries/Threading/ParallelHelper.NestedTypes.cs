#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParallelHelper.NestedTypes.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Threading
{
    [SuppressMessage("ReSharper", "SwapViaDeconstruction", Justification = "Performance. The deconstruction would create additional locals and references.")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Dispose happens after leaving the scope")]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass", Justification = "The the outer static Sort method is never meant to be called from the private interface implementations.")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "False alarm, exceptions are re-thrown but the analyzer fails to consider the both the [DoesNotReturn] and [ContractAnnotation] attributes")]
    public static partial class ParallelHelper
    {
        #region Nested Types

        #region Nested Interfaces

        #region ISortHelper<T> interface

        private interface ISortHelper<T>
        {
            #region Methods

            void Sort(IAsyncContext context, IList<T> list, int startIndex, int count, IComparer<T>? comparer);
            void Sort(IAsyncContext context, T[] array, int startIndex, int count, IComparer<T>? comparer);
            void Sort(IAsyncContext context, List<T> list, int startIndex, int count, IComparer<T>? comparer);
            void Sort<TFrom, TTo>(IAsyncContext context, CastArray<TFrom, TTo> array, IComparer<TTo>? comparer) where TFrom : unmanaged where TTo : unmanaged, T;

            #endregion
        }

        #endregion

        #region ISortHelper<TKey, TValue> interface

        private interface ISortHelper<TKey, TValue>
        {
            #region Methods

            void Sort(IAsyncContext context, IList<TKey> keys, IList<TValue> values, int startIndex, int count, IComparer<TKey>? comparer);
            void Sort(IAsyncContext context, TKey[] keys, TValue[] values, int startIndex, int count, IComparer<TKey>? comparer);
            void Sort(IAsyncContext context, List<TKey> keys, List<TValue> values, int startIndex, int count, IComparer<TKey>? comparer);
            void Sort(IAsyncContext context, ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer); // Needed because array version cannot be used if offsets are different
            void Sort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IAsyncContext context, CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer)
                where TKeyFrom : unmanaged where TKeyTo : unmanaged, TKey where TValueFrom : unmanaged where TValueTo : unmanaged, TValue;

            #endregion
        }

        #endregion

        #endregion

        #region Nested Classes

        #region SortHelper<T> class

        private sealed class SortHelper<T> : ISortHelper<T>
        {
            #region Fields

            internal static ISortHelper<T> Instance { get; } = typeof(IComparable<T>).IsAssignableFrom(typeof(T))
                ? (ISortHelper<T>)Activator.CreateInstance(typeof(ComparableSortHelper<>).MakeGenericType(typeof(T)), true)!
                : new SortHelper<T>();

            #endregion

            #region Methods

            #region Static Methods

            #region Internal Methods

            internal static void DoSort(IAsyncContext context, IList<T> list, int startIndex, int count, IComparer<T> comparer, int freeDepth)
            {
                #region Local Methods

                static void SwapIfGreater(IList<T> list, IComparer<T> comparer, int i, int j)
                {
                    if (comparer.Compare(list[i], list[j]) > 0)
                    {
                        T temp = list[i];
                        list[i] = list[j];
                        list[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(list, comparer, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(list, comparer, startIndex, startIndex + 1);
                        SwapIfGreater(list, comparer, startIndex, startIndex + 2);
                        SwapIfGreater(list, comparer, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        T current = list[i];
                        int j = i - 1;
                        while (j >= startIndex && comparer.Compare(current, list[j]) < 0)
                        {
                            list[j + 1] = list[j];
                            j -= 1;
                        }

                        list[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(list, startIndex, count, comparer);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && comparer.Compare(list[startIndex + pivotIndex], list[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(list[startIndex + pivotIndex], list[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex, pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex, pivotIndex, comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, list, startIndex, pivotIndex, comparer, freeDepth);
                DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth);
            }

            internal static void DoSort(IAsyncContext context, T[] array, int startIndex, int count, IComparer<T> comparer, int freeDepth)
            {
                #region Local Methods

                static void SwapIfGreater(T[] array, IComparer<T> comparer, int i, int j)
                {
                    if (comparer.Compare(array[i], array[j]) > 0)
                    {
                        T temp = array[i];
                        array[i] = array[j];
                        array[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(array, comparer, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(array, comparer, startIndex, startIndex + 1);
                        SwapIfGreater(array, comparer, startIndex, startIndex + 2);
                        SwapIfGreater(array, comparer, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        T current = array[i];
                        int j = i - 1;
                        while (j >= startIndex && comparer.Compare(current, array[j]) < 0)
                        {
                            array[j + 1] = array[j];
                            j -= 1;
                        }

                        array[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(array, startIndex, count, comparer);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && comparer.Compare(array[startIndex + pivotIndex], array[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(array[startIndex + pivotIndex], array[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array, startIndex, pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array, startIndex, pivotIndex, comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, array, startIndex, pivotIndex, comparer, freeDepth);
                DoSort(context, array, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth);
            }

            internal static void DoSort(IAsyncContext context, List<T> list, int startIndex, int count, IComparer<T> comparer, int freeDepth)
            {
                #region Local Methods

                static void SwapIfGreater(List<T> list, IComparer<T> comparer, int i, int j)
                {
                    if (comparer.Compare(list[i], list[j]) > 0)
                    {
                        T temp = list[i];
                        list[i] = list[j];
                        list[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(list, comparer, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(list, comparer, startIndex, startIndex + 1);
                        SwapIfGreater(list, comparer, startIndex, startIndex + 2);
                        SwapIfGreater(list, comparer, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        T current = list[i];
                        int j = i - 1;
                        while (j >= startIndex && comparer.Compare(current, list[j]) < 0)
                        {
                            list[j + 1] = list[j];
                            j -= 1;
                        }

                        list[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(list, startIndex, count, comparer);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && comparer.Compare(list[startIndex + pivotIndex], list[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(list[startIndex + pivotIndex], list[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex, pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex, pivotIndex, comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, list, startIndex, pivotIndex, comparer, freeDepth);
                DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth);
            }

            internal static void DoSort<TFrom, TTo>(IAsyncContext context, CastArray<TFrom, TTo> array, IComparer<TTo> comparer, int freeDepth)
                where TFrom : unmanaged
                where TTo : unmanaged, T
            {
                #region Local Methods

                static void SwapIfGreater(CastArray<TFrom, TTo> array, IComparer<TTo> comparer, int i, int j)
                {
                    if (comparer.Compare(array[i], array[j]) > 0)
                    {
                        TTo temp = array[i];
                        array[i] = array[j];
                        array[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(array.Length > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (array.Length < sortParallelThreshold)
                {
                    if (array.Length == 2)
                    {
                        SwapIfGreater(array, comparer, 0, 1);
                        return;
                    }

                    if (array.Length == 3)
                    {
                        SwapIfGreater(array, comparer, 0, 1);
                        SwapIfGreater(array, comparer, 0, 2);
                        SwapIfGreater(array, comparer, 1, 2);
                        return;
                    }

                    // insertion sort
                    for (int i = 1; i < array.Length; i++)
                    {
                        TTo current = array[i];
                        int j = i - 1;
                        while (j >= 0 && comparer.Compare(current, array[j]) < 0)
                        {
                            array[j + 1] = array[j];
                            j -= 1;
                        }

                        array[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(array.Length > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(array, comparer);
                    Debug.Assert(pivotIndex < array.Length);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = array.Length - 1;
                        while (pivotIndex < endIndex && comparer.Compare(array[pivotIndex], array[pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (array.Length - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        array = array.Slice(pivotIndex);
                        continue;
                    }

                    // Right half has <= 1 element
                    if (array.Length - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(array[pivotIndex], array[pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        array = array.Slice(0, pivotIndex);
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, array.Length - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= array.Length >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array.Slice(0, pivotIndex), comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array.Slice(pivotIndex), comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array.Slice(pivotIndex), comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array.Slice(0, pivotIndex), comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, array.Slice(0, pivotIndex), comparer, freeDepth);
                DoSort(context, array.Slice(pivotIndex), comparer, freeDepth);
            }

            #endregion

            #region Private Methods

            private static int Partition(IList<T> list, int startIndex, int count, IComparer<T> comparer)
            {
                #region Local Methods

                static void Swap(IList<T> list, int i, int j)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (comparer.Compare(list[startIndex], list[startIndex + 1]) > 0)
                        Swap(list, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                T pivotValue = list[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && comparer.Compare(list[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, list[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(list, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(list, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(T[] array, int startIndex, int count, IComparer<T> comparer)
            {
                #region Local Methods

                static void Swap(T[] array, int i, int j)
                {
                    var temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (comparer.Compare(array[startIndex], array[startIndex + 1]) > 0)
                        Swap(array, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                T pivotValue = array[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && comparer.Compare(array[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, array[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(array, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(array, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

#if DEBUG
                int endIndex = startIndex + count - 1;
                for (int i = startIndex; i < pivotIndex; i++)
                    Debug.Assert(comparer.Compare(array[i], pivotValue) <= 0);
                for (int i = pivotIndex + 1; i < endIndex + 1; i++)
                    Debug.Assert(comparer.Compare(array[i], pivotValue) > 0);
#endif
                return pivotIndex - startIndex;
            }

            private static int Partition(List<T> list, int startIndex, int count, IComparer<T> comparer)
            {
                #region Local Methods

                static void Swap(List<T> list, int i, int j)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (comparer.Compare(list[startIndex], list[startIndex + 1]) > 0)
                        Swap(list, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                T pivotValue = list[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && comparer.Compare(list[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, list[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(list, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(list, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition<TFrom, TTo>(CastArray<TFrom, TTo> array, IComparer<TTo> comparer)
                where TFrom : unmanaged
                where TTo : unmanaged, T
            {
                #region Local Methods

                static void Swap(CastArray<TFrom, TTo> array, int i, int j)
                {
                    var temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }

                #endregion

                Debug.Assert(array.Length > 1);
                if (array.Length == 2)
                {
                    if (comparer.Compare(array[0], array[1]) > 0)
                        Swap(array, 0, 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = (array.Length >> 1);
                TTo pivotValue = array[pivotIndex];

                int left = 0;
                int right = array.Length - 1;

                do
                {
                    while (left <= right && comparer.Compare(array[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, array[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(array, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(array, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex;
            }

            #endregion

            #endregion

            #region Instance Methods

            public void Sort(IAsyncContext context, IList<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                Debug.Assert(list is not (T[] or ArraySegment<T> or List<T> or CircularList<T>), "Known ILists are expected to be handled in a special way to avoid slower virtual calls");
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, list, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException or ArgumentOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            public void Sort(IAsyncContext context, T[] array, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, array, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            public void Sort(IAsyncContext context, List<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, list, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is ArgumentOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            public void Sort<TFrom, TTo>(IAsyncContext context, CastArray<TFrom, TTo> list, IComparer<TTo>? comparer)
                where TFrom : unmanaged
                where TTo : unmanaged, T
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, list, comparer ?? ComparerHelper<TTo>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            #endregion

            #endregion
        }

        #endregion

        #region SortHelper<TKey, TValue> class

        private sealed class SortHelper<TKey, TValue> : ISortHelper<TKey, TValue>
        {
            #region Fields

            internal static ISortHelper<TKey, TValue> Instance { get; } = typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey))
                ? (ISortHelper<TKey, TValue>)Activator.CreateInstance(typeof(ComparableSortHelper<,>).MakeGenericType(typeof(TKey), typeof(TValue)), true)!
                : new SortHelper<TKey, TValue>();

            #endregion

            #region Methods

            #region Static Methods

            #region Internal Methods

            internal static void DoSort(IAsyncContext context, IList<TKey> keys, IList<TValue> values, int startIndex, int count, IComparer<TKey> comparer, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(IList<TKey> keys, IList<TValue> values, IComparer<TKey> comparer, int i, int j)
                {
                    if (comparer.Compare(keys[i], keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 1);
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 2);
                        SwapIfGreater(keys, values, comparer, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= startIndex && comparer.Compare(currentKey, keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, startIndex, count, comparer);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && comparer.Compare(keys[startIndex + pivotIndex], keys[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(keys[startIndex + pivotIndex], keys[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth);
                DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth);
            }

            internal static void DoSort(IAsyncContext context, TKey[] keys, TValue[] values, int startIndex, int count, IComparer<TKey> comparer, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(TKey[] keys, TValue[] values, IComparer<TKey> comparer, int i, int j)
                {
                    if (comparer.Compare(keys[i], keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 1);
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 2);
                        SwapIfGreater(keys, values, comparer, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= startIndex && comparer.Compare(currentKey, keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, startIndex, count, comparer);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && comparer.Compare(keys[startIndex + pivotIndex], keys[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(keys[startIndex + pivotIndex], keys[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth);
                DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth);
            }

            internal static void DoSort(IAsyncContext context, List<TKey> keys, List<TValue> values, int startIndex, int count, IComparer<TKey> comparer, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(List<TKey> keys, List<TValue> values, IComparer<TKey> comparer, int i, int j)
                {
                    if (comparer.Compare(keys[i], keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 1);
                        SwapIfGreater(keys, values, comparer, startIndex, startIndex + 2);
                        SwapIfGreater(keys, values, comparer, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= startIndex && comparer.Compare(currentKey, keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, startIndex, count, comparer);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && comparer.Compare(keys[startIndex + pivotIndex], keys[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(keys[startIndex + pivotIndex], keys[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys, values, startIndex, pivotIndex, comparer, freeDepth);
                DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, comparer, freeDepth);
            }

            internal static void DoSort(IAsyncContext context, ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey> comparer, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey> comparer, int i, int j)
                {
                    if (comparer.Compare(keys[i], keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(keys.Length > 1 && values.Length == keys.Length);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (keys.Length < sortParallelThreshold)
                {
                    if (keys.Length == 2)
                    {
                        SwapIfGreater(keys, values, comparer, 0, 1);
                        return;
                    }

                    if (keys.Length == 3)
                    {
                        SwapIfGreater(keys, values, comparer, 0, 1);
                        SwapIfGreater(keys, values, comparer, 0, 2);
                        SwapIfGreater(keys, values, comparer, 1, 2);
                        return;
                    }

                    // insertion sort
                    for (int i = 1; i < keys.Length; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= 0 && comparer.Compare(currentKey, keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(keys.Length > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, comparer);
                    Debug.Assert(pivotIndex < keys.Length);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = keys.Length - 1;
                        while (pivotIndex < endIndex && comparer.Compare(keys[pivotIndex], keys[pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (keys.Length - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        keys = keys.Slice(pivotIndex);
                        values = values.Slice(pivotIndex);
                        continue;
                    }

                    // Right half has <= 1 element
                    if (keys.Length - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(keys[pivotIndex], keys[pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        keys = keys.Slice(0, pivotIndex);
                        values = values.Slice(0, pivotIndex);
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, keys.Length - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= keys.Length >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), comparer, freeDepth);
                DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), comparer, freeDepth);
            }

            internal static void DoSort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IAsyncContext context, CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo> comparer, int freeDepth)
                where TKeyFrom : unmanaged
                where TKeyTo : unmanaged, TKey
                where TValueFrom : unmanaged
                where TValueTo : unmanaged, TValue
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo> comparer, int i, int j)
                {
                    if (comparer.Compare(keys[i], keys[j]) > 0)
                    {
                        TKeyTo key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValueTo value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(keys.Length > 1 && values.Length == keys.Length);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (keys.Length < sortParallelThreshold)
                {
                    if (keys.Length == 2)
                    {
                        SwapIfGreater(keys, values, comparer, 0, 1);
                        return;
                    }

                    if (keys.Length == 3)
                    {
                        SwapIfGreater(keys, values, comparer, 0, 1);
                        SwapIfGreater(keys, values, comparer, 0, 2);
                        SwapIfGreater(keys, values, comparer, 1, 2);
                        return;
                    }

                    // insertion sort
                    for (int i = 1; i < keys.Length; i++)
                    {
                        TKeyTo currentKey = keys[i];
                        TValueTo currentValue = values[i];
                        int j = i - 1;
                        while (j >= 0 && comparer.Compare(currentKey, keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(keys.Length > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, comparer);
                    Debug.Assert(pivotIndex < keys.Length);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = keys.Length - 1;
                        while (pivotIndex < endIndex && comparer.Compare(keys[pivotIndex], keys[pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (keys.Length - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        keys = keys.Slice(pivotIndex);
                        values = values.Slice(pivotIndex);
                        continue;
                    }

                    // Right half has <= 1 element
                    if (keys.Length - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && comparer.Compare(keys[pivotIndex], keys[pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        keys = keys.Slice(0, pivotIndex);
                        values = values.Slice(0, pivotIndex);
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, keys.Length - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= keys.Length >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), comparer, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), comparer, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), comparer, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), comparer, freeDepth);
                DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), comparer, freeDepth);
            }

            #endregion

            #region Private Methods

            private static int Partition(IList<TKey> keys, IList<TValue> values, int startIndex, int count, IComparer<TKey> comparer)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void Swap(IList<TKey> keys, IList<TValue> values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (comparer.Compare(keys[startIndex], keys[startIndex + 1]) > 0)
                        Swap(keys, values, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && comparer.Compare(keys[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(TKey[] keys, TValue[] values, int startIndex, int count, IComparer<TKey> comparer)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void Swap(TKey[] keys, TValue[] values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (comparer.Compare(keys[startIndex], keys[startIndex + 1]) > 0)
                        Swap(keys, values, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && comparer.Compare(keys[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(List<TKey> keys, List<TValue> values, int startIndex, int count, IComparer<TKey> comparer)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void Swap(List<TKey> keys, List<TValue> values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (comparer.Compare(keys[startIndex], keys[startIndex + 1]) > 0)
                        Swap(keys, values, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && comparer.Compare(keys[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey> comparer)
            {
                #region Local Methods

                static void Swap(ArraySection<TKey> keys, ArraySection<TValue> values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(keys.Length > 1);
                if (keys.Length == 2)
                {
                    if (comparer.Compare(keys[0], keys[1]) > 0)
                        Swap(keys, values, 0, 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = (keys.Length >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = 0;
                int right = keys.Length - 1;

                do
                {
                    while (left <= right && comparer.Compare(keys[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex;
            }

            private static int Partition<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo> comparer)
                where TKeyFrom : unmanaged
                where TKeyTo : unmanaged, TKey
                where TValueFrom : unmanaged
                where TValueTo : unmanaged, TValue
            {
                #region Local Methods

                static void Swap(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, int i, int j)
                {
                    TKeyTo key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValueTo value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(keys.Length > 1);
                if (keys.Length == 2)
                {
                    if (comparer.Compare(keys[0], keys[1]) > 0)
                        Swap(keys, values, 0, 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = (keys.Length >> 1);
                TKeyTo pivotValue = keys[pivotIndex];

                int left = 0;
                int right = keys.Length - 1;

                do
                {
                    while (left <= right && comparer.Compare(keys[left], pivotValue) <= 0)
                        left += 1;
                    while (left < right && comparer.Compare(pivotValue, keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex;
            }

            #endregion

            #endregion

            #region Instance Methods

            public void Sort(IAsyncContext context, IList<TKey> keys, IList<TValue> values, int startIndex, int count, IComparer<TKey>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, keys, values, startIndex, count, comparer ?? ComparerHelper<TKey>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException or ArgumentOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            public void Sort(IAsyncContext context, TKey[] keys, TValue[] values, int startIndex, int count, IComparer<TKey>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, keys, values, startIndex, count, comparer ?? ComparerHelper<TKey>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            public void Sort(IAsyncContext context, List<TKey> keys, List<TValue> values, int startIndex, int count, IComparer<TKey>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, keys, values, startIndex, count, comparer ?? ComparerHelper<TKey>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is ArgumentOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            public void Sort(IAsyncContext context, ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer)
            {
                Debug.Assert(keys.Length > 1 && keys.Length == values.Length);
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, keys, values, comparer ?? ComparerHelper<TKey>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            public void Sort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IAsyncContext context, CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer)
                where TKeyFrom : unmanaged
                where TKeyTo : unmanaged, TKey
                where TValueFrom : unmanaged
                where TValueTo : unmanaged, TValue
            {
                Debug.Assert(keys.Length > 1 && keys.Length == values.Length);
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                try
                {
                    // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
                    DoSort(context, keys, values, comparer ?? ComparerHelper<TKeyTo>.Comparer, Log2(maxTasks));
                }
                catch (Exception e)
                {
                    if (e is IndexOutOfRangeException)
                        Throw.ArgumentException(Res.IListInconsistentComparer);
                    Throw.InvalidOperationException(Res.IListComparerFail, e);
                }
            }

            #endregion

            #endregion
        }

        #endregion

        #region ComparableSortHelper<T> class

        private sealed class ComparableSortHelper<T> : ISortHelper<T>
            where T : IComparable<T>
        {
            #region Methods

            #region Static Methods

            private static void DoSort(IAsyncContext context, IList<T> list, int startIndex, int count, int freeDepth)
            {
                #region Local Methods

                static void SwapIfGreater(IList<T> list, int i, int j)
                {
                    if (list[i].CompareTo(list[j]) > 0)
                    {
                        T temp = list[i];
                        list[i] = list[j];
                        list[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(list, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(list, startIndex, startIndex + 1);
                        SwapIfGreater(list, startIndex, startIndex + 2);
                        SwapIfGreater(list, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        T current = list[i];
                        int j = i - 1;
                        while (j >= startIndex && current.CompareTo(list[j]) < 0)
                        {
                            list[j + 1] = list[j];
                            j -= 1;
                        }

                        list[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(list, startIndex, count);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && list[startIndex + pivotIndex].CompareTo(list[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && list[startIndex + pivotIndex].CompareTo(list[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex, pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex, pivotIndex, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, list, startIndex, pivotIndex, freeDepth);
                DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, freeDepth);
            }

            private static void DoSort(IAsyncContext context, T[] array, int startIndex, int count, int freeDepth)
            {
                #region Local Methods

                static void SwapIfGreater(T[] array, int i, int j)
                {
                    if (array[i].CompareTo(array[j]) > 0)
                    {
                        T temp = array[i];
                        array[i] = array[j];
                        array[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(array, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(array, startIndex, startIndex + 1);
                        SwapIfGreater(array, startIndex, startIndex + 2);
                        SwapIfGreater(array, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        T current = array[i];
                        int j = i - 1;
                        while (j >= startIndex && current.CompareTo(array[j]) < 0)
                        {
                            array[j + 1] = array[j];
                            j -= 1;
                        }

                        array[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(array, startIndex, count);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && array[startIndex + pivotIndex].CompareTo(array[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && array[startIndex + pivotIndex].CompareTo(array[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array, startIndex, pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array, startIndex, pivotIndex, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, array, startIndex, pivotIndex, freeDepth);
                DoSort(context, array, startIndex + pivotIndex, count - pivotIndex, freeDepth);
            }

            private static void DoSort(IAsyncContext context, List<T> list, int startIndex, int count, int freeDepth)
            {
                #region Local Methods

                static void SwapIfGreater(List<T> list, int i, int j)
                {
                    if (list[i].CompareTo(list[j]) > 0)
                    {
                        T temp = list[i];
                        list[i] = list[j];
                        list[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(list, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(list, startIndex, startIndex + 1);
                        SwapIfGreater(list, startIndex, startIndex + 2);
                        SwapIfGreater(list, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        T current = list[i];
                        int j = i - 1;
                        while (j >= startIndex && current.CompareTo(list[j]) < 0)
                        {
                            list[j + 1] = list[j];
                            j -= 1;
                        }

                        list[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(list, startIndex, count);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && list[startIndex + pivotIndex].CompareTo(list[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && list[startIndex + pivotIndex].CompareTo(list[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex, pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, list, startIndex, pivotIndex, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, list, startIndex, pivotIndex, freeDepth);
                DoSort(context, list, startIndex + pivotIndex, count - pivotIndex, freeDepth);
            }

            private static void DoSort<TFrom, TTo>(IAsyncContext context, CastArray<TFrom, TTo> array, int freeDepth)
                where TFrom : unmanaged
                where TTo : unmanaged, T
            {
                #region Local Methods

                static void SwapIfGreater(CastArray<TFrom, TTo> array, int i, int j)
                {
                    if (array[i].CompareTo(array[j]) > 0)
                    {
                        TTo temp = array[i];
                        array[i] = array[j];
                        array[j] = temp;
                    }
                }

                #endregion

                Debug.Assert(array.Length > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (array.Length < sortParallelThreshold)
                {
                    if (array.Length == 2)
                    {
                        SwapIfGreater(array, 0, 1);
                        return;
                    }

                    if (array.Length == 3)
                    {
                        SwapIfGreater(array, 0, 1);
                        SwapIfGreater(array, 0, 2);
                        SwapIfGreater(array, 1, 2);
                        return;
                    }

                    // insertion sort
                    for (int i = 1; i < array.Length; i++)
                    {
                        TTo current = array[i];
                        int j = i - 1;
                        while (j >= 0 && current.CompareTo(array[j]) < 0)
                        {
                            array[j + 1] = array[j];
                            j -= 1;
                        }

                        array[j + 1] = current;
                    }

                    return;
                }

                // pivot index relative to 0, it's always between 0 and array.Length
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(array.Length > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(array);
                    Debug.Assert(pivotIndex < array.Length);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = array.Length - 1;
                        while (pivotIndex < endIndex && array[pivotIndex].CompareTo(array[pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (array.Length - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        array = array.Slice(pivotIndex);
                        continue;
                    }

                    // Right half has <= 1 element
                    if (array.Length - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && array[pivotIndex].CompareTo(array[pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        array = array.Slice(0, pivotIndex);
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, array.Length - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting sleeping if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= array.Length >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array.Slice(0, pivotIndex), freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array.Slice(pivotIndex), freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array.Slice(pivotIndex), freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array.Slice(0, pivotIndex), freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, array.Slice(0, pivotIndex), freeDepth);
                DoSort(context, array.Slice(pivotIndex), freeDepth);
            }

            private static int Partition(IList<T> list, int startIndex, int count)
            {
                #region Local Methods

                static void Swap(IList<T> list, int i, int j)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (list[startIndex].CompareTo(list[startIndex + 1]) > 0)
                        Swap(list, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                T pivotValue = list[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && list[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(list[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(list, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(list, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(T[] array, int startIndex, int count)
            {
                #region Local Methods

                static void Swap(T[] list, int i, int j)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (array[startIndex].CompareTo(array[startIndex + 1]) > 0)
                        Swap(array, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                T pivotValue = array[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && array[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(array[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(array, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(array, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

#if DEBUG
                int endIndex = startIndex + count - 1;
                for (int i = startIndex; i < pivotIndex; i++)
                    Debug.Assert(array[i].CompareTo(pivotValue) <= 0);
                for (int i = pivotIndex + 1; i < endIndex + 1; i++)
                    Debug.Assert(array[i].CompareTo(pivotValue) > 0);
#endif
                return pivotIndex - startIndex;
            }

            private static int Partition(List<T> list, int startIndex, int count)
            {
                #region Local Methods

                static void Swap(List<T> list, int i, int j)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (list[startIndex].CompareTo(list[startIndex + 1]) > 0)
                        Swap(list, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                T pivotValue = list[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && list[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(list[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(list, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(list, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition<TFrom, TTo>(CastArray<TFrom, TTo> array)
                where TFrom : unmanaged
                where TTo : unmanaged, T
            {
                #region Local Methods

                static void Swap(CastArray<TFrom, TTo> array, int i, int j)
                {
                    var temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }

                #endregion

                Debug.Assert(array.Length > 1);
                if (array.Length == 2)
                {
                    if (array[0].CompareTo(array[1]) > 0)
                        Swap(array, 0, 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = array.Length >> 1;
                T pivotValue = array[pivotIndex];

                int left = 0;
                int right = array.Length - 1;

                do
                {
                    while (left <= right && array[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(array[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(array, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(array, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex;
            }

            #endregion

            #region Instance Methods

            public void Sort(IAsyncContext context, IList<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                Debug.Assert(list is not (T[] or ArraySegment<T> or List<T> or CircularList<T>), "Known ILists are expected to be handled in a special way to avoid slower virtual calls");
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<T>.Default))
                {
                    try
                    {
                        SortHelper<T>.DoSort(context, list, startIndex, count, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is IndexOutOfRangeException or ArgumentOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, list, startIndex, count, Log2(maxTasks));
            }

            public void Sort(IAsyncContext context, T[] array, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<T>.Default))
                {
                    try
                    {
                        SortHelper<T>.DoSort(context, array, startIndex, count, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is IndexOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, array, startIndex, count, Log2(maxTasks));
            }

            public void Sort(IAsyncContext context, List<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<T>.Default))
                {
                    try
                    {
                        SortHelper<T>.DoSort(context, list, startIndex, count, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is ArgumentOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, list, startIndex, count, Log2(maxTasks));
            }

            public void Sort<TFrom, TTo>(IAsyncContext context, CastArray<TFrom, TTo> list, IComparer<TTo>? comparer)
                where TFrom : unmanaged
                where TTo : unmanaged, T
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<T>.Default))
                {
                    try
                    {
                        SortHelper<T>.DoSort(context, list, comparer,Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is IndexOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, list, Log2(maxTasks));
            }

            #endregion

            #endregion
        }

        #endregion

        #region ComparableSortHelper<T> class

        private sealed class ComparableSortHelper<TKey, TValue> : ISortHelper<TKey, TValue>
            where TKey : IComparable<TKey>
        {
            #region Methods

            #region Static Methods

            private static void DoSort(IAsyncContext context, IList<TKey> keys, IList<TValue> values, int startIndex, int count, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(IList<TKey> keys, IList<TValue> values, int i, int j)
                {
                    if (keys[i].CompareTo(keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(keys, values, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(keys, values, startIndex, startIndex + 1);
                        SwapIfGreater(keys, values, startIndex, startIndex + 2);
                        SwapIfGreater(keys, values, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= startIndex && currentKey.CompareTo(keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, startIndex, count);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && keys[startIndex + pivotIndex].CompareTo(keys[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && keys[startIndex + pivotIndex].CompareTo(keys[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex, pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex, pivotIndex, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys, values, startIndex, pivotIndex, freeDepth);
                DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth);
            }

            private static void DoSort(IAsyncContext context, TKey[] keys, TValue[] values, int startIndex, int count, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(TKey[] keys, TValue[] values, int i, int j)
                {
                    if (keys[i].CompareTo(keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(keys, values, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(keys, values, startIndex, startIndex + 1);
                        SwapIfGreater(keys, values, startIndex, startIndex + 2);
                        SwapIfGreater(keys, values, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= startIndex && currentKey.CompareTo(keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, startIndex, count);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && keys[startIndex + pivotIndex].CompareTo(keys[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && keys[startIndex + pivotIndex].CompareTo(keys[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex, pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex, pivotIndex, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys, values, startIndex, pivotIndex, freeDepth);
                DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth);
            }

            private static void DoSort(IAsyncContext context, List<TKey> keys, List<TValue> values, int startIndex, int count, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(List<TKey> keys, List<TValue> values, int i, int j)
                {
                    if (keys[i].CompareTo(keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(count > 1);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (count < sortParallelThreshold)
                {
                    if (count == 2)
                    {
                        SwapIfGreater(keys, values, startIndex, startIndex + 1);
                        return;
                    }

                    if (count == 3)
                    {
                        SwapIfGreater(keys, values, startIndex, startIndex + 1);
                        SwapIfGreater(keys, values, startIndex, startIndex + 2);
                        SwapIfGreater(keys, values, startIndex + 1, startIndex + 2);
                        return;
                    }

                    // insertion sort
                    for (int i = startIndex + 1; i < startIndex + count; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= startIndex && currentKey.CompareTo(keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(count > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values, startIndex, count);
                    Debug.Assert(pivotIndex < count);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = count - 1;
                        while (pivotIndex < endIndex && keys[startIndex + pivotIndex].CompareTo(keys[startIndex + pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (count - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        startIndex += pivotIndex;
                        count -= pivotIndex;
                        continue;
                    }

                    // Right half has <= 1 element
                    if (count - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && keys[startIndex + pivotIndex].CompareTo(keys[startIndex + pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        count = pivotIndex;
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, count - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= count >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex, pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys, values, startIndex, pivotIndex, freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys, values, startIndex, pivotIndex, freeDepth);
                DoSort(context, keys, values, startIndex + pivotIndex, count - pivotIndex, freeDepth);
            }

            private static void DoSort(IAsyncContext context, ArraySection<TKey> keys, ArraySection<TValue> values, int freeDepth)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(ArraySection<TKey> keys, ArraySection<TValue> values, int i, int j)
                {
                    if (keys[i].CompareTo(keys[j]) > 0)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValue value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(keys.Length > 1 && values.Length == keys.Length);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (keys.Length < sortParallelThreshold)
                {
                    if (keys.Length == 2)
                    {
                        SwapIfGreater(keys, values, 0, 1);
                        return;
                    }

                    if (keys.Length == 3)
                    {
                        SwapIfGreater(keys, values, 0, 1);
                        SwapIfGreater(keys, values, 0, 2);
                        SwapIfGreater(keys, values, 1, 2);
                        return;
                    }

                    // insertion sort
                    for (int i = 1; i < keys.Length; i++)
                    {
                        TKey currentKey = keys[i];
                        TValue currentValue = values[i];
                        int j = i - 1;
                        while (j >= 0 && currentKey.CompareTo(keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(keys.Length > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values);
                    Debug.Assert(pivotIndex < keys.Length);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = keys.Length - 1;
                        while (pivotIndex < endIndex && keys[pivotIndex].CompareTo(keys[pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (keys.Length - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        keys = keys.Slice(pivotIndex);
                        values = values.Slice(pivotIndex);
                        continue;
                    }

                    // Right half has <= 1 element
                    if (keys.Length - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && keys[pivotIndex].CompareTo(keys[pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        keys = keys.Slice(0, pivotIndex);
                        values = values.Slice(0, pivotIndex);
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, keys.Length - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= keys.Length >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), freeDepth);
                DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), freeDepth);
            }

            private static void DoSort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IAsyncContext context, CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, int freeDepth)
                where TKeyFrom : unmanaged
                where TKeyTo : unmanaged, TKey
                where TValueFrom : unmanaged
                where TValueTo : unmanaged, TValue
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void SwapIfGreater(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, int i, int j)
                {
                    if (keys[i].CompareTo(keys[j]) > 0)
                    {
                        TKeyTo key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;

                        TValueTo value = values[i];
                        values[i] = values[j];
                        values[j] = value;
                    }
                }

                #endregion

                Debug.Assert(keys.Length > 1 && values.Length == keys.Length);
                if (context.IsCancellationRequested)
                    return;

                // just a few elements: no parallel sorting and no recursion
                if (keys.Length < sortParallelThreshold)
                {
                    if (keys.Length == 2)
                    {
                        SwapIfGreater(keys, values, 0, 1);
                        return;
                    }

                    if (keys.Length == 3)
                    {
                        SwapIfGreater(keys, values, 0, 1);
                        SwapIfGreater(keys, values, 0, 2);
                        SwapIfGreater(keys, values, 1, 2);
                        return;
                    }

                    // insertion sort
                    for (int i = 1; i < keys.Length; i++)
                    {
                        TKeyTo currentKey = keys[i];
                        TValueTo currentValue = values[i];
                        int j = i - 1;
                        while (j >= 0 && currentKey.CompareTo(keys[j]) < 0)
                        {
                            keys[j + 1] = keys[j];
                            values[j + 1] = values[j];
                            j -= 1;
                        }

                        keys[j + 1] = currentKey;
                        values[j + 1] = currentValue;
                    }

                    return;
                }

                // pivot index relative to startIndex, it's always between 0 and count
                int pivotIndex;

                // This is to prevent real recursion in trivial cases. We could use a stack to avoid real recursion, but in practice that is slower.
                while (true)
                {
                    Debug.Assert(keys.Length > 1);

                    // Separating two partitions and then sorting the halves separately.
                    pivotIndex = Partition(keys, values);
                    Debug.Assert(pivotIndex < keys.Length);

                    // Left half has <= 1 element: doing the right half only
                    if (pivotIndex <= 1)
                    {
                        // Narrowing from the left if possible. Can happen if the values are the same according to the comparer.
                        int endIndex = keys.Length - 1;
                        while (pivotIndex < endIndex && keys[pivotIndex].CompareTo(keys[pivotIndex + 1]) == 0)
                            pivotIndex += 1;

                        // there is nothing left to sort
                        if (keys.Length - pivotIndex <= 1)
                            return;

                        // "Recursion" with the right half only so free depth can remain the same
                        keys = keys.Slice(pivotIndex);
                        values = values.Slice(pivotIndex);
                        continue;
                    }

                    // Right half has <= 1 element
                    if (keys.Length - pivotIndex <= 1)
                    {
                        // Narrowing from the right if possible. Can happen if the values are the same according to the comparer.
                        while (pivotIndex > 1 && keys[pivotIndex].CompareTo(keys[pivotIndex - 1]) == 0)
                            pivotIndex -= 1;

                        // there is nothing left to sort
                        if (pivotIndex <= 1)
                            return;

                        // "Recursion" with the left half only so free depth can remain the same
                        keys = keys.Slice(0, pivotIndex);
                        values = values.Slice(0, pivotIndex);
                        continue;
                    }

                    // Only real recursions from here so breaking the loop.
                    break;
                }

                // Here we have two partitions that we can sort independently. If we have free depth and both sides are big enough we can spawn a new thread.
                if (freeDepth > 0 && Math.Min(pivotIndex, keys.Length - pivotIndex) >= sortParallelThreshold)
                {
                    // Only one of them is spawned on a new thread because the current thread can do one of the jobs just fine.
                    // Always the smaller half is assigned to the new thread because of the overhead and to prevent the wait handle
                    // from starting to sleep if possible.
                    using var handle = new ManualResetEventSlim(false);
                    if (pivotIndex <= keys.Length >> 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, keys.Slice(0, pivotIndex), values.Slice(0, pivotIndex), freeDepth);
                DoSort(context, keys.Slice(pivotIndex), values.Slice(pivotIndex), freeDepth);
            }

            private static int Partition(IList<TKey> keys, IList<TValue> values, int startIndex, int count)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void Swap(IList<TKey> keys, IList<TValue> values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (keys[startIndex].CompareTo(keys[startIndex + 1]) > 0)
                        Swap(keys, values, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && keys[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(TKey[] keys, TValue[] values, int startIndex, int count)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void Swap(TKey[] keys, TValue[] values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (keys[startIndex].CompareTo(keys[startIndex + 1]) > 0)
                        Swap(keys, values, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && keys[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(List<TKey> keys, List<TValue> values, int startIndex, int count)
            {
                #region Local Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                static void Swap(List<TKey> keys, List<TValue> values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(count > 1);
                if (count == 2)
                {
                    if (keys[startIndex].CompareTo(keys[startIndex + 1]) > 0)
                        Swap(keys, values, startIndex, startIndex + 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = startIndex + (count >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = startIndex;
                int right = startIndex + count - 1;

                do
                {
                    while (left <= right && keys[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex - startIndex;
            }

            private static int Partition(ArraySection<TKey> keys, ArraySection<TValue> values)
            {
                #region Local Methods

                static void Swap(ArraySection<TKey> keys, ArraySection<TValue> values, int i, int j)
                {
                    TKey key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValue value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(keys.Length > 1);
                if (keys.Length == 2)
                {
                    if (keys[0].CompareTo(keys[1]) > 0)
                        Swap(keys, values, 0, 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = (keys.Length >> 1);
                TKey pivotValue = keys[pivotIndex];

                int left = 0;
                int right = keys.Length - 1;

                do
                {
                    while (left <= right && keys[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex;
            }

            private static int Partition<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values)
                where TKeyFrom : unmanaged
                where TKeyTo : unmanaged, TKey
                where TValueFrom : unmanaged
                where TValueTo : unmanaged, TValue
            {
                #region Local Methods

                static void Swap(CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, int i, int j)
                {
                    TKeyTo key = keys[i];
                    keys[i] = keys[j];
                    keys[j] = key;

                    TValueTo value = values[i];
                    values[i] = values[j];
                    values[j] = value;
                }

                #endregion

                Debug.Assert(keys.Length > 1);
                if (keys.Length == 2)
                {
                    if (keys[0].CompareTo(keys[1]) > 0)
                        Swap(keys, values, 0, 1);

                    return 1;
                }

                // taking the pivot from the middle
                int pivotIndex = (keys.Length >> 1);
                TKeyTo pivotValue = keys[pivotIndex];

                int left = 0;
                int right = keys.Length - 1;

                do
                {
                    while (left <= right && keys[left].CompareTo(pivotValue) <= 0)
                        left += 1;
                    while (left < right && pivotValue.CompareTo(keys[right]) < 0)
                        right -= 1;
                    if (left >= right)
                        break;

                    Swap(keys, values, left, right);
                    if (pivotIndex == right)
                        pivotIndex = left;

                    left++;
                    right--;
                } while (left <= right);

                // left - 1 is now the new place of the pivot
                if (pivotIndex != left - 1)
                {
                    Swap(keys, values, pivotIndex, left - 1);
                    pivotIndex = left - 1;
                }

                return pivotIndex;
            }

            #endregion

            #region Instance Methods

            public void Sort(IAsyncContext context, IList<TKey> keys, IList<TValue> values, int startIndex, int count, IComparer<TKey>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<TKey>.Default))
                {
                    try
                    {
                        SortHelper<TKey, TValue>.DoSort(context, keys, values, startIndex, count, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is IndexOutOfRangeException or ArgumentOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, keys, values, startIndex, count, Log2(maxTasks));
            }

            public void Sort(IAsyncContext context, TKey[] keys, TValue[] values, int startIndex, int count, IComparer<TKey>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<TKey>.Default))
                {
                    try
                    {
                        SortHelper<TKey, TValue>.DoSort(context, keys, values, startIndex, count, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is IndexOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, keys, values, startIndex, count, Log2(maxTasks));
            }

            public void Sort(IAsyncContext context, List<TKey> keys, List<TValue> values, int startIndex, int count, IComparer<TKey>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<TKey>.Default))
                {
                    try
                    {
                        SortHelper<TKey, TValue>.DoSort(context, keys, values, startIndex, count, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is ArgumentOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, keys, values, startIndex, count, Log2(maxTasks));
            }

            public void Sort(IAsyncContext context, ArraySection<TKey> keys, ArraySection<TValue> values, IComparer<TKey>? comparer)
            {
                Debug.Assert(keys.Length > 1 && keys.Length == values.Length);
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<TKey>.Default))
                {
                    try
                    {
                        SortHelper<TKey, TValue>.DoSort(context, keys, values, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is IndexOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, keys, values, Log2(maxTasks));
            }

            public void Sort<TKeyFrom, TKeyTo, TValueFrom, TValueTo>(IAsyncContext context, CastArray<TKeyFrom, TKeyTo> keys, CastArray<TValueFrom, TValueTo> values, IComparer<TKeyTo>? comparer)
                where TKeyFrom : unmanaged
                where TKeyTo : unmanaged, TKey
                where TValueFrom : unmanaged
                where TValueTo : unmanaged, TValue
            {
                Debug.Assert(keys.Length > 1 && keys.Length == values.Length);
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<TKeyTo>.Default))
                {
                    try
                    {
                        SortHelper<TKey, TValue>.DoSort(context, keys, values, comparer, Log2(maxTasks));
                        return;
                    }
                    catch (Exception e)
                    {
                        if (e is IndexOutOfRangeException)
                            Throw.ArgumentException(Res.IListInconsistentComparer);
                        Throw.InvalidOperationException(Res.IListComparerFail, e);
                    }
                }

                // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
                DoSort(context, keys, values, Log2(maxTasks));
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #endregion
    }
}