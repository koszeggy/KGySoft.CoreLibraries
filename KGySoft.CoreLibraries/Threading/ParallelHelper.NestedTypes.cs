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
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
#endif
using System.Threading;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Threading
{
    [SuppressMessage("ReSharper", "SwapViaDeconstruction",
        Justification = "Performance. The deconstruction would create additional locals and references.")]
    public static partial class ParallelHelper
    {
        #region Nested Types

        #region Nested Interfaces

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

        #region Nested Classes

        #region SortHelper class

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
                DoSort(context, array.Slice(pivotIndex, array.Length - pivotIndex), comparer, freeDepth);
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

                return pivotIndex - 0;
            }

            #endregion

            #endregion

            #region Instance Methods

            public void Sort(IAsyncContext context, IList<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                //Debug.Assert(list is not (T[] or ArraySegment<T> or List<T> or CircularList<T>) && !list.GetType().IsGenericTypeOf(typeof(CastArray<,>)), "Known ILists are expected to be handled in a special way to avoid slower virtual calls");
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, list, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, list, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
            }

            public void Sort(IAsyncContext context, T[] array, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, array, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, array, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
            }

            public void Sort(IAsyncContext context, List<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                //Debug.Assert(list is not (T[] or ArraySegment<T> or List<T> or CircularList<T>) && !list.GetType().IsGenericTypeOf(typeof(CastArray<,>)), "Known ILists are expected to be handled in a special way to avoid slower virtual calls");
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, list, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, list, startIndex, count, comparer ?? ComparerHelper<T>.Comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
            }

            public void Sort<TFrom, TTo>(IAsyncContext context, CastArray<TFrom, TTo> list, IComparer<TTo>? comparer)
                where TFrom : unmanaged
                where TTo : unmanaged, T
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, list, comparer ?? ComparerHelper<TTo>.Comparer, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, list, comparer ?? ComparerHelper<TTo>.Comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
            }

            #endregion

            #endregion
        }

        #endregion

        #region ComparableSortHelper class

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
                        DoSort(context, array.Slice(pivotIndex, array.Length - pivotIndex), freeDepth - 1);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(_ =>
                        {
                            DoSort(context, array.Slice(pivotIndex, array.Length - pivotIndex), freeDepth - 1);
                            handle.Set();
                        }, null);
                        DoSort(context, array.Slice(0, pivotIndex), freeDepth - 1);
                    }

                    handle.Wait();
                    return;
                }

                // Otherwise, doing the recursions on the current thread
                DoSort(context, array.Slice(0, pivotIndex), freeDepth);
                DoSort(context, array.Slice(pivotIndex, array.Length - pivotIndex), freeDepth);
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

                return pivotIndex - 0;
            }

            #endregion

            #region Instance Methods

            public void Sort(IAsyncContext context, IList<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<T>.Default))
                {
#if NETCOREAPP3_0_OR_GREATER
                    SortHelper<T>.DoSort(context, list, startIndex, count, comparer, BitOperations.Log2((uint)maxTasks));
#else
                    SortHelper<T>.DoSort(context, list, startIndex, count, comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
                    return;
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, list, startIndex, count, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, list, startIndex, count, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
            }

            public void Sort(IAsyncContext context, T[] array, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<T>.Default))
                {
#if NETCOREAPP3_0_OR_GREATER
                    SortHelper<T>.DoSort(context, array, startIndex, count, comparer, BitOperations.Log2((uint)maxTasks));
#else
                    SortHelper<T>.DoSort(context, array, startIndex, count, comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
                    return;
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, array, startIndex, count, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, array, startIndex, count, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
            }

            public void Sort(IAsyncContext context, List<T> list, int startIndex, int count, IComparer<T>? comparer)
            {
                int maxTasks = context.MaxDegreeOfParallelism;
                if (maxTasks <= 0)
                    maxTasks = CoreCount;

                if (comparer != null && !Equals(comparer, Comparer<T>.Default))
                {
#if NETCOREAPP3_0_OR_GREATER
                    SortHelper<T>.DoSort(context, list, startIndex, count, comparer, BitOperations.Log2((uint)maxTasks));
#else
                    SortHelper<T>.DoSort(context, list, startIndex, count, comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
                    return;
                }

                // Due to the recursive binary branching the allowed subtask count is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, list, startIndex, count, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, list, startIndex, count, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
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
#if NETCOREAPP3_0_OR_GREATER
                    SortHelper<T>.DoSort(context, list, comparer, BitOperations.Log2((uint)maxTasks));
#else
                    SortHelper<T>.DoSort(context, list, comparer, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
                    return;
                }

                // Due to the recursive binary branching the allowed subtask array.Length is logarithmic, eg. 3 if there are 8 cores.
#if NETCOREAPP3_0_OR_GREATER
                DoSort(context, list, BitOperations.Log2((uint)maxTasks));
#else
                DoSort(context, list, (int)Math.Ceiling(Math.Log(maxTasks, 2)));
#endif
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #endregion
    }
}