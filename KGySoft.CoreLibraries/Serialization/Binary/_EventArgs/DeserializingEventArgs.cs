#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DeserializingEventArgs.cs
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

using System.ComponentModel;
using System.Runtime.Serialization;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides arguments for the <see cref="CustomSerializerSurrogateSelector.Deserializing">CustomSerializerSurrogateSelector.Deserializing</see> event.
    /// </summary>
    public class DeserializingEventArgs : HandledEventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the uninitialized object that is being deserialized.
        /// <br/>If you initialize it manually make sure you set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to
        /// omit the default deserialization logic.
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// Gets the context of this deserialization.
        /// </summary>
        public StreamingContext Context { get; }

        /// <summary>
        /// Gets the <see cref="System.Runtime.Serialization.SerializationInfo"/> from which the <see cref="Object"/> is about to be deserialized.
        /// </summary>
        public SerializationInfo SerializationInfo { get; }

        /// <summary>
        /// Gets or sets whether the <see cref="ISerializable"/> implementation of <see cref="Object"/> should be ignored.
        /// <br/>To completely omit the default deserialization logic set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/>&#160;to deserialize the <see cref="Object"/> by fields even if it implements <see cref="ISerializable"/>;
        /// otherwise, <see langword="false"/>.
        /// </value>
        public bool IgnoreISerializable { get; set; }

        #endregion

        #region Constructors

        internal DeserializingEventArgs(object obj, StreamingContext context, SerializationInfo info)
        {
            Object = obj;
            Context = context;
            SerializationInfo = info;
        }

        #endregion
    }
}
