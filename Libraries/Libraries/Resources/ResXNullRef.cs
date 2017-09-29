#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXNullRef.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2017 - All Rights Reserved
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
using System.Runtime.Serialization;
using System.Threading;

#endregion

namespace KGySoft.Libraries.Resources
{
    [Serializable]
    internal sealed class ResXNullRef : IObjectReference
    {
        #region Fields

        [NonSerialized]
        private static ResXNullRef value;

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

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return Value;
        }

        #endregion
    }
}
