#if !NETCOREAPP3_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MemoryHelper.cs
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
using System.Security;

#endregion

namespace KGySoft
{
    internal static class MemoryHelper
    {
        #region Fields

        internal static readonly uint PointerSizeMask = (uint)IntPtr.Size - 1u;

        #endregion

        #region Methods

        #region Internal Methods

        [SecurityCritical]
        internal static unsafe void CopyMemory(void* source, void* target, long length)
        {
#if NET35 || NET40 || NET45
            DoCopyMemory((byte*)source, (byte*)target, length);
#else
            Buffer.MemoryCopy(source, target, length, length);
#endif
        }

        #endregion

        #region Private Methods

#if NET35 || NET40 || NET45
        [SecurityCritical]
        private static unsafe void DoCopyMemory(byte* src, byte* dst, long length)
        {
            // NOTE: Unrolling loops could provide a better performance, but as this is for older frameworks only, we don't optimize it heavily.
            // Alignment is maintained though, even if misalignment is not an issue (apart from performance) on targets supported by these old .NET Framework versions.

            // Trying to copy 8 bytes (qword) at a time if both pointers have the same alignment
            // In a 32-bit process 4-byte alignment is enough for qword copy
            if ((((nuint)src ^ (nuint)dst) & PointerSizeMask) == 0u)
            {
                // Advancing with bytes until both pointers become aligned
                while (((nuint)src & PointerSizeMask) != 0u && length > 0)
                {
                    *dst = *src;
                    dst += 1;
                    src += 1;
                    length -= 1;
                }

                long* qwDst = (long*)dst;
                long* qwSrc = (long*)src;

                // copying qwords as long as possible
                for (long len = length >> 3, i = 0; i < len; i++)
                {
                    *qwDst = *qwSrc;
                    qwDst += 1;
                    qwSrc += 1;
                }

                if ((length & 7) == 0)
                    return;

                dst = (byte*)qwDst;
                src = (byte*)qwSrc;

                // copying last dword
                if ((length & 4) != 0)
                {
                    *(int*)dst = *(int*)src;
                    dst += 4;
                    src += 4;
                }

                // copying last word
                if ((length & 2) != 0)
                {
                    *(short*)dst = *(short*)src;
                    dst += 2;
                    src += 2;
                }

                // copying last byte
                if ((length & 1) != 0)
                    *dst = *src;

                return;
            }

            // 4-byte aligned copy (in a 32-bit process this part is redundant)
            if ((((nuint)src ^ (nuint)dst) & 3u) == 0u)
            {
                // Advancing with bytes until both pointers become aligned
                while (((nuint)src & 3u) != 0u && length > 0)
                {
                    *dst = *src;
                    dst += 1;
                    src += 1;
                    length -= 1;
                }

                int* dwDst = (int*)dst;
                int* dwSrc = (int*)src;

                // copying dwords as long as possible
                for (long len = length >> 2, i = 0; i < len; i++)
                {
                    *dwDst = *dwSrc;
                    dwDst += 1;
                    dwSrc += 1;
                }

                if ((length & 3) == 0)
                    return;

                dst = (byte*)dwDst;
                src = (byte*)dwSrc;

                // copying last word
                if ((length & 2) != 0)
                {
                    *(short*)dst = *(short*)src;
                    dst += 2;
                    src += 2;
                }

                // copying last byte
                if ((length & 1) != 0)
                    *dst = *src;

                return;
            }

            // 2-byte aligned copy
            if ((((nuint)src ^ (nuint)dst) & 1u) == 0u)
            {
                // Advancing one byte if both pointers are odd
                if (((nuint)src & 1u) != 0u && length > 0)
                {
                    *dst = *src;
                    dst += 1;
                    src += 1;
                    length -= 1;
                }

                short* wDst = (short*)dst;
                short* wSrc = (short*)src;

                // copying words as long as possible
                for (long len = length >> 1, i = 0; i < len; i++)
                {
                    *wDst = *wSrc;
                    wDst += 1;
                    wSrc += 1;
                }

                // copying last byte
                if ((length & 1) == 1)
                    *(byte*)wDst = *(byte*)wSrc;

                return;
            }

            // Fallback: byte-wise copy if the alignments do not match
            for (long i = 0; i < length; i++)
            {
                *dst = *src;
                dst += 1;
                src += 1;
            }
        }
#endif

        #endregion

        #endregion
    }
}
#endif