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

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Similar to Span{char} but can be used in any platform.
    /// </summary>
    internal readonly unsafe struct MutableString
    {
        #region Fields

        #region Internal Fields

        internal readonly int Length;

        #endregion

        #region Private Fields

        private readonly char* head;
        
        #endregion

        #endregion

        #region Properties and Indexers

        #region Properties


        #endregion

        #region Indexers

        internal char this[int index]
        {
            get => head[index];
            set => head[index] = value;
        }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Should be initialized from a fixed or stack allocated pointer.
        /// </summary>
        internal MutableString(char* s, int len)
        {
            head = s;
            Length = len;
        }

        #endregion

        #region Methods

        #region Public Methods

        public override string ToString() => new String(head, 0, Length).Replace('\0', '□');

        #endregion

        #region Internal Methods

        internal MutableString Substring(int start, int count) => new MutableString(head + start, count);
        internal MutableString Substring(int start) => new MutableString(head + start, Length - start);

        internal void ToUpper()
        {
            for (int i = 0; i < Length; i++)
                head[i] = Char.ToUpperInvariant(head[i]);
        }

        #endregion

        #endregion
    }
}
