#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MutableString.cs
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
using System.Diagnostics;
using System.Security; 

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Similar to Span{char} but can be used in any platform.
    /// </summary>
    [SecurityCritical]
    [DebuggerDisplay("{" + nameof(ToStringDebugger) + "()}")]
    internal readonly unsafe ref struct MutableString
    {
        #region Fields

        [SecurityCritical]
        private readonly char* head;

        internal readonly int Length;

        #endregion

        #region Indexers

        internal char this[int index]
        {
            [SecurityCritical]
            get => head[index];

            [SecurityCritical]
            set => head[index] = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Should be initialized from a fixed or stack allocated pointer.
        /// </summary>
        [SecurityCritical]
        internal MutableString(char* s, int len)
        {
            head = s;
            Length = len;
        }

        #endregion

        #region Methods

        #region Public Methods

        [SecuritySafeCritical]
        public override string ToString() => new String(head, 0, Length);

        #endregion

        #region Internal Methods

        [SecurityCritical]
        internal MutableString Substring(int start, int count) => new MutableString(head + start, count);

        [SecurityCritical]
        internal MutableString Substring(int start) => new MutableString(head + start, Length - start);

        [SecurityCritical]
        internal char* AddressOf(int index) => head + index;

        [SecurityCritical]
        internal void ToUpper()
        {
            for (int i = 0; i < Length; i++)
                head[i] = Char.ToUpperInvariant(head[i]);
        }

        #endregion

        #region PrivateMethods

        private string ToStringDebugger() => ToString().Replace('\0', '□');

        #endregion

        #endregion
    }
}
