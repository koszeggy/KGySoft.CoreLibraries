#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReflectionException.cs
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

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Represent a reflection error.
    /// </summary>
    [Serializable]
    public sealed class ReflectionException : Exception
    {
        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionException"/> class.
        /// </summary>
        public ReflectionException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ReflectionException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="inner">The inner exception.</param>
        public ReflectionException(string message, Exception? inner) : base(message, inner) { }

        #endregion

        #region Private Constructors

        private ReflectionException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        #endregion

        #endregion
    }
}
