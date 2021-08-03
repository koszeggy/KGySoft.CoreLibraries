#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SettingFieldEventArgs.cs
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
using System.Reflection;
using System.Runtime.Serialization;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides arguments for the <see cref="CustomSerializerSurrogateSelector.SettingField">CustomSerializerSurrogateSelector.SettingField</see> event.
    /// </summary>
    public class SettingFieldEventArgs : HandledEventArgs
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
        /// Gets the <see cref="System.Runtime.Serialization.SerializationInfo"/> of the of the <see cref="Object"/> being deserialized.
        /// </summary>
        public SerializationInfo SerializationInfo { get; }

        /// <summary>
        /// Gets the current <see cref="SerializationEntry"/> of the of the <see cref="Object"/> being deserialized.
        /// </summary>
        public SerializationEntry Entry { get; }

        /// <summary>
        /// Gets or sets the field to be set. If <see langword="null"/>&#160;and <see cref="CustomSerializerSurrogateSelector.IgnoreNonExistingFields"/>
        /// is <see langword="false"/>, then a <see cref="SerializationException"/> will be thrown.
        /// You may either set this property by the matching field or set the <see cref="HandledEventArgs.Handled"/> to <see langword="true"/>&#160;to
        /// skip the default processing.
        /// <br/>Default value: The field identified as the matching field, or <see langword="null"/>, if such field was not found.
        /// </summary>
        public FieldInfo? Field { get; set; }

        /// <summary>
        /// Gets or sets the value to be set.
        /// <br/>To prevent setting any value make sure you set the <see cref="HandledEventArgs.Handled"/> property to <see langword="true"/>&#160;to
        /// omit the default deserialization logic.
        /// </summary>
        public object? Value { get; set; }

        #endregion

        #region Constructors

        internal SettingFieldEventArgs(object obj, StreamingContext context, SerializationInfo info, SerializationEntry entry)
        {
            Object = obj;
            Context = context;
            SerializationInfo = info;
            Entry = entry;
        }

        #endregion
    }
}
