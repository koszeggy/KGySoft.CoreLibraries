#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MutableStringBuilder.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Diagnostics;
#if !(NET35 || NET40)
using System.Runtime.CompilerServices;
#endif
using System.Security; 

#endregion

namespace KGySoft.CoreLibraries
{
    [SecurityCritical]
    internal struct MutableStringBuilder
    {
        #region Fields

        private readonly MutableString str;

        private int usedLen;

        #endregion

        #region Properties and Indexers

        #region Properties

        internal int Capacity => str.Length;

        internal int Length => usedLen;

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
            usedLen = 0;
        }

        internal unsafe MutableStringBuilder(char* s, int len)
        {
            str = new MutableString(s, len);
            usedLen = 0;
        }


        #endregion

        #region Methods

        #region Public Methods

        [SecuritySafeCritical]
        public override string ToString() => str.Substring(0, Length).ToString();

        #endregion

        #region Internal Methods

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
#endif
        internal void Append(char c)
        {
            Debug.Assert(Length < Capacity, "Not enough capacity");
            str[usedLen] = c;
            usedLen += 1;
        }

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal void Append(string s)
        {
            Debug.Assert(Length + s.Length <= Capacity, "Not enough capacity");
            CopyTo(usedLen, s);
            usedLen += s.Length;
        }

        internal void Append(ulong value, bool isNegative, int size)
        {
            Debug.Assert(Length + size <= Capacity, "Not enough capacity");
            if (value == 0)
            {
                Append('0');
                return;
            }

            int i = size + usedLen;
            while (value > 0)
            {
                str[--i] = (char)(value % 10 + '0');
                value /= 10;
            }

            if (isNegative)
                str[--i] = '-';

            Debug.Assert(i == usedLen, "Invalid size");
            usedLen += size;
        }

        internal void Insert(int index, char c)
        {
            Debug.Assert(index <= Length, "Invalid index");
            if (index == usedLen)
            {
                Append(c);
                return;
            }

            for (int i = usedLen - 1; i >= index; i--)
                str[i + 1] = str[i];
            str[index] = c;
            usedLen += 1;
        }

        internal void Insert(int index, string s)
        {
            Debug.Assert(index <= Length, "Invalid index");
            if (index == usedLen)
            {
                Append(s);
                return;
            }

            int len = s.Length;
            for (int i = usedLen - 1; i >= index; i--)
                str[i + len] = str[i];
            CopyTo(index, s);
            usedLen += s.Length;
        }

        #endregion

        #region Private Methods

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal void CopyTo(int index, string s)
        {
            int len = s.Length;
            for (int i = 0; i < len; i++)
                str[index + i] = s[i];
        }

        #endregion

        #endregion
    }
}
