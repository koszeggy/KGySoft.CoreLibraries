#if NETCOREAPP3_0_OR_GREATER && !NET8_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinaryPrimitiveExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Buffers.Binary;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class BinaryPrimitiveExtensions
    {
        #region Methods

        extension(BinaryPrimitives)
        {
            internal static void ReverseEndianness(Span<ushort> input, Span<ushort> output)
            {
                Debug.Assert(input.Length == output.Length);
                for (int i = 0; i < input.Length; i++)
                    output[i] = BinaryPrimitives.ReverseEndianness(input[i]);
            }

            internal static void ReverseEndianness(Span<uint> input, Span<uint> output)
            {
                Debug.Assert(input.Length == output.Length);
                for (int i = 0; i < input.Length; i++)
                    output[i] = BinaryPrimitives.ReverseEndianness(input[i]);
            }
            
            internal static void ReverseEndianness(Span<ulong> input, Span<ulong> output)
            {
                Debug.Assert(input.Length == output.Length);
                for (int i = 0; i < input.Length; i++)
                    output[i] = BinaryPrimitives.ReverseEndianness(input[i]);
            }
        }

        #endregion
    }
}
#endif