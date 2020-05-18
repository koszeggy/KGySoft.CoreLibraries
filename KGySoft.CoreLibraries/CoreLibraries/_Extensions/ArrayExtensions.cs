#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArrayExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Runtime.CompilerServices;

using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class ArrayExtensions
    {
        #region Nested classes

        private static class ElementInfo<T>
        {
            #region Fields

            internal static readonly bool IsPrimitive = typeof(T).IsPrimitive;
            internal static readonly int ElementSizeExponent = IsPrimitive ? (int)Math.Log(Reflector.SizeOf<T>(), 2) : 0;

            #endregion
        }

        #endregion

        #region Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static void CopyElements<T>(this T[] source, int sourceIndex, T[] dest, int destIndex, int count)
        {
            if (ElementInfo<T>.IsPrimitive)
            {
                Buffer.BlockCopy(source, sourceIndex << ElementInfo<T>.ElementSizeExponent, dest, destIndex << ElementInfo<T>.ElementSizeExponent, count << ElementInfo<T>.ElementSizeExponent);
                return;
            }

            Array.Copy(source, sourceIndex, dest, destIndex, count);
        }

        #endregion
    }
}