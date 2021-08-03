#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DelegateExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Threading;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for delegates.
    /// </summary>
    public static class DelegateExtensions
    {
        #region Methods

        /// <summary>
        /// Combines <paramref name="value"/> with the referenced <paramref name="location"/> in a thread-safe way.
        /// </summary>
        /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
        /// <param name="value">The value to combine with the referenced <paramref name="location"/>.</param>
        /// <param name="location">The reference of the delegate to combine with <paramref name="value"/>.</param>
        public static void AddSafe<TDelegate>(this TDelegate? value, ref TDelegate? location)
            where TDelegate : Delegate
        {
            while (true)
            {
                TDelegate? current = location;
                if (Interlocked.CompareExchange(ref location, (TDelegate?)Delegate.Combine(current, value), current) == current)
                    return;
            }
        }

        /// <summary>
        /// If the referenced <paramref name="location"/> contains the <paramref name="value"/> delegate, then the last occurrence of it will be removed in a thread-safe way.
        /// </summary>
        /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
        /// <param name="value">The value to remove from the referenced <paramref name="location"/>.</param>
        /// <param name="location">The reference of the delegate from which <paramref name="value"/> should be removed.</param>
        public static void RemoveSafe<TDelegate>(this TDelegate? value, ref TDelegate? location)
            where TDelegate : Delegate
        {
            while (true)
            {
                TDelegate? current = location;
                if (Interlocked.CompareExchange(ref location, (TDelegate?)Delegate.Remove(current, value), current) == current)
                    return;
            }
        }

        #endregion
    }
}