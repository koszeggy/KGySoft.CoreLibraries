#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MutableString.cs
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security; 

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Similar to Span{char} but can be used in any platform.
    /// </summary>
    [SecurityCritical]
    internal readonly unsafe struct MutableString
    {
        #region Fields

        #region Internal Fields

        internal readonly int Length;

        #endregion

        #region Private Fields

        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2151:Fields with critical types should be security critical", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        private readonly char* head;
        
        #endregion

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
        public override string ToString() => new String(head, 0, Length).Replace('\0', '□');

        #endregion

        #region Internal Methods

        [SecurityCritical]
        internal MutableString Substring(int start, int count) => new MutableString(head + start, count);

        [SecurityCritical]
        internal MutableString Substring(int start) => new MutableString(head + start, Length - start);

        [SecurityCritical]
        internal void ToUpper()
        {
            for (int i = 0; i < Length; i++)
                head[i] = Char.ToUpperInvariant(head[i]);
        }

        #endregion

        #endregion
    }
}
