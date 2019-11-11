#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectDataObtainedEventArgs.cs
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
using System.Runtime.Serialization;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides arguments for the <see cref="CustomSerializerSurrogateSelector.ObjectDataObtained">CustomSerializerSurrogateSelector.ObjectDataObtained</see> event.
    /// </summary>
    public class ObjectDataObtainedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the object that is being serialized.
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// Gets the context of this serialization.
        /// </summary>
        public StreamingContext Context { get; }

        /// <summary>
        /// Gets the populated <see cref="System.Runtime.Serialization.SerializationInfo"/> of the <see cref="Object"/> being serialized.
        /// You still can change its content before the actual serialization.
        /// </summary>
        public SerializationInfo SerializationInfo { get; }

        #endregion

        #region Constructors

        internal ObjectDataObtainedEventArgs(object obj, StreamingContext context, SerializationInfo info)
        {
            Object = obj;
            Context = context;
            SerializationInfo = info;
        }

        #endregion
    }
}
