#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GettingFieldEventArgs.cs
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
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides arguments for the <see cref="CustomSerializerSurrogateSelector.GettingField">CustomSerializerSurrogateSelector.GettingField</see> event.
    /// </summary>
    /// <remarks>
    /// <para>The default value of the <see cref="HandledEventArgs.Handled"/> property is <see langword="true"/>, if the field is marked by <see cref="NonSerializedAttribute"/>
    /// and the value of <see cref="CustomSerializerSurrogateSelector.IgnoreNonSerializedAttribute"/> property is <see langword="false"/>;
    /// otherwise, <see langword="false"/>.</para>
    /// <para>You can set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to prevent saving the current field,
    /// or you can set it to <see langword="false"/>&#160;to force saving even non-serialized fields.</para>
    /// </remarks>
    public class GettingFieldEventArgs : HandledEventArgs
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
        /// Gets the <see cref="System.Runtime.Serialization.SerializationInfo"/> of the of the <see cref="Object"/> being serialized.
        /// If you add the data manually make sure you set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to
        /// omit the default serialization logic.
        /// </summary>
        public SerializationInfo SerializationInfo { get; }

        /// <summary>
        /// Gets the field whose value is about to be stored.
        /// </summary>
        public FieldInfo Field { get; }

        /// <summary>
        /// Gets or sets the name of the entry to be stored in the serialization stream.
        /// <br/>Setting it to <see langword="null"/>&#160;will cause an <see cref="ArgumentNullException"/> from <see cref="SerializationInfo"/>.
        /// <br/>To prevent storing any value make sure you set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to
        /// omit the default serialization logic.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the value to be stored in the serialization stream.
        /// <br/>To prevent storing any value make sure you set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to
        /// omit the default serialization logic.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> to be stored in the serialization stream.
        /// <br/>Setting <see langword="null"/>&#160;will cause an <see cref="ArgumentNullException"/> from <see cref="SerializationInfo"/>.
        /// <br/>To prevent storing any value make sure you set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to
        /// omit the default serialization logic.
        /// </summary>
        public Type Type { get; set; } = default!;

        #endregion

        #region Constructors

        internal GettingFieldEventArgs(object obj, StreamingContext context, SerializationInfo info, FieldInfo field)
        {
            Object = obj;
            Context = context;
            SerializationInfo = info;
            Field = field;
        }

        #endregion
    }
}
