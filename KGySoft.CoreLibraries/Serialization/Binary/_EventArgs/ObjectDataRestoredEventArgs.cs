#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectDataRestoredEventArgs.cs
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

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides arguments for the <see cref="CustomSerializerSurrogateSelector.ObjectDataRestored">CustomSerializerSurrogateSelector.ObjectDataRestored</see> event.
    /// </summary>
    public class ObjectDataRestoredEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the object that is being deserialized.
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// Gets the context of this deserialization.
        /// </summary>
        public StreamingContext Context { get; }

        /// <summary>
        /// Gets the <see cref="System.Runtime.Serialization.SerializationInfo"/> from which <see cref="Object"/> has been restored.
        /// </summary>
        public SerializationInfo SerializationInfo { get; }

        #endregion

        #region Constructors

        internal ObjectDataRestoredEventArgs(object obj, StreamingContext context, SerializationInfo info)
        {
            Object = obj;
            Context = context;
            SerializationInfo = info;
        }

        #endregion
    }
}