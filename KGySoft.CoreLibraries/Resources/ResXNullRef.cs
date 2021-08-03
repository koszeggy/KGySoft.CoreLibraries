#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXNullRef.cs
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
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

#endregion

namespace KGySoft.Resources
{
    [Serializable]
    internal sealed class ResXNullRef : IObjectReference
    {
        #region Fields

        [NonSerialized]
        private static ResXNullRef? value;

        #endregion

        #region Properties

        /// <summary>
        /// Represents the sole instance of <see cref="ResXNullRef"/> class.
        /// </summary>
        internal static ResXNullRef Value
        {
            get
            {
                if (value == null)
                    Interlocked.CompareExchange(ref value, new ResXNullRef(), null);
                return value;
            }
        }

        #endregion

        #region IObjectReference Members

        [SecurityCritical]
        object IObjectReference.GetRealObject(StreamingContext context) => Value;

        #endregion
    }
}
