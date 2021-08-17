#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MutableStringBuilder.cs
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
using System.Runtime.CompilerServices;
using System.Security; 

#endregion

namespace KGySoft.CoreLibraries
{
    [SecurityCritical]
    internal ref struct MutableStringBuilder
    {
        #region Fields

        private readonly MutableString str;

        private int pos;

        #endregion

        #region Properties and Indexers

        #region Properties

        internal int Capacity => str.Length;

        internal int Length => pos;

        #endregion

        #region Indexers

        internal char this[int index]
        {
            get
            {
                Debug.Assert(index < Length, "Invalid index");
                return str[index];
            }
            set
            {
                Debug.Assert(index < Length, "Invalid index");
                str[index] = value;
            }
        }

        #endregion

        #endregion

        #region Constructors

        internal MutableStringBuilder(in MutableString s)
        {
            str = s;
            pos = 0;
        }

        internal unsafe MutableStringBuilder(char* s, int len)
        {
            str = new MutableString(s, len);
            pos = 0;
        }


        #endregion

        #region Methods

        #region Public Methods

        [SecuritySafeCritical]
        public override string ToString() => pos == str.Length ? str.ToString() : str.Substring(0, Length).ToString();

        #endregion

        #region Internal Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal void Append(char c)
        {
            Debug.Assert(Length < Capacity, "Not enough capacity");
            str[pos] = c;
            pos += 1;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal void Append(char c, int count)
        {
            Debug.Assert(Length + count <= Capacity, "Not enough capacity");
            for (int i = 0; i < count; i++)
            {
                str[pos] = c;
                pos += 1;
            }
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal void Append(string s)
        {
            Debug.Assert(Length + s.Length <= Capacity, "Not enough capacity");
            WriteString(pos, s);
            pos += s.Length;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal void Append(StringSegmentInternal s)
        {
            Debug.Assert(Length + s.Length <= Capacity, "Not enough capacity");
            WriteString(pos, s.String, s.Offset, s.Length);
            pos += s.Length;
        }


        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal void Append(string s, int startIndex, int count)
        {
            Debug.Assert(Length + count <= Capacity, "Not enough capacity");
            Debug.Assert(startIndex + count <= s.Length, "Invalid arguments");
            WriteString(pos, s, startIndex, count);
            pos += count;
        }

        internal void Append(ulong value, bool isNegative, int size)
        {
            Debug.Assert(Length + size <= Capacity, "Not enough capacity");
            if (value == 0)
            {
                Append('0');
                return;
            }

            int i = size + pos;
            while (value > 0)
            {
                str[--i] = (char)(value % 10 + '0');
                value /= 10;
            }

            if (isNegative)
                str[--i] = '-';

            Debug.Assert(i == pos, "Invalid size");
            pos += size;
        }

        internal void Append(byte value) => Append(value, false, value >= 100 ? 3 : value >= 10 ? 2 : 1);

        internal void AppendHex(byte value)
        {
            Debug.Assert(Length + 2 <= Capacity, "Not enough capacity");
            const string hexDigits = "0123456789ABCDEF";

            Append(hexDigits[(value >> 4)]);
            Append(hexDigits[(value & 0xF)]);
        }

        internal void AppendLine() => Append(Environment.NewLine);

        internal void Insert(int index, char c)
        {
            Debug.Assert(index <= Length, "Invalid index");
            if (index == pos)
            {
                Append(c);
                return;
            }

            for (int i = pos - 1; i >= index; i--)
                str[i + 1] = str[i];
            str[index] = c;
            pos += 1;
        }

        internal void Insert(int index, string s)
        {
            Debug.Assert(index <= Length, "Invalid index");
            if (index == pos)
            {
                Append(s);
                return;
            }

            int len = s.Length;
            for (int i = pos - 1; i >= index; i--)
                str[i + len] = str[i];
            WriteString(index, s);
            pos += s.Length;
        }

        #endregion

        #region Private Methods

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private unsafe void WriteString(int index, string s)
        {
#if !(NET35 || NET40 || NET45)
            if (s.Length > 8)
            {
                fixed (char* ptr = s)
                    Buffer.MemoryCopy(ptr, str.AddressOf(pos), (Capacity - pos) << 1, s.Length << 1);
                return;
            } 
#endif

            for (int i = 0; i < s.Length; i++)
                str[index + i] = s[i];
        }

        [SecurityCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private unsafe void WriteString(int targetIndex, string s, int sourceIndex, int count)
        {
#if !(NET35 || NET40 || NET45)
            if (count > 8)
            {
                fixed (char* ptr = s)
                    Buffer.MemoryCopy(ptr + sourceIndex, str.AddressOf(pos), (Capacity - pos) << 1, count << 1);
                return;
            } 
#endif

            for (int i = 0; i < count; i++)
                str[targetIndex + i] = s[sourceIndex + i];
        }

        #endregion

        #endregion
    }
}
