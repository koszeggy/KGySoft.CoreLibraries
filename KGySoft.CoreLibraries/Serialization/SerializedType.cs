#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SerializedType.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Diagnostics.CodeAnalysis;

using KGySoft.Serialization.Binary;
using KGySoft.Serialization.Xml;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// Represents a type, whose metadata should be preserved.
    /// Using the <see cref="SerializedType"/> struct can prevent trimming reflected members, making the type compatible with native AOT mode in serialization scenarios.
    /// </summary>
    /// <remarks>
    /// <para>If serialization or deserialization of a type fails in native AOT mode, you can place the following snippet in the initialization section of your code:
    /// <code lang="C#">SerializedType _ = typeof(MySerializedTypeToPeserve);</code></para>
    /// <para>The discarded result is optimized away in a release build, so its only role is to notify the trimmer that the metadata required for serialization has to be preserved.
    /// Multiple types are recommended to be specified as a collection:
    /// <code lang="C#"><![CDATA[ReadOnlySpan<SerializedType> _ = [typeof(MySimpleType), typeof(MyGenericType<,>)];]]></code></para>
    /// <para>If a type has to be preserved along with its nested types, you can use the <see cref="WithNestedTypes">WithNestedTypes</see> method:
    /// <code lang="C#">var _ = SerializedType.WithNestedTypes(typeof(MySerializedTypeToPeserve));</code></para>
    /// <note>The safe mode deserialization methods of this library have overloads with <see cref="SerializedType"/> element type in their <c>expectedTypes</c> parameter.
    /// If you use them, no separate initialization is required in AOT mode. Please note though that unlike the no-op initializations above, the expected types are processed
    /// by the deserialization methods.</note>
    /// </remarks>
    public readonly struct SerializedType
    {
        #region Constants

        private const DynamicallyAccessedMemberTypes neededMembers = BinarySerializer.NeededMembers | XmlSerializer.NeededMembers;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type whose metadata should be preserved.
        /// </summary>
        [DynamicallyAccessedMembers(neededMembers)]
        public Type Type { get; }

        #endregion

        #region Operators

        /// <summary>
        /// Defines an implicit conversion from <see cref="Type"/> to <see cref="SerializedType"/>.
        /// </summary>
        /// <param name="type">The type to convert.</param>
        /// <returns>A <see cref="SerializedType"/> instance that represents the specified <paramref name="type"/>.</returns>
        public static implicit operator SerializedType([DynamicallyAccessedMembers(neededMembers)] Type type) => new SerializedType(type);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedType"/> structure with the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type whose metadata should be preserved.</param>
        public SerializedType([DynamicallyAccessedMembers(neededMembers)] Type type) => Type = type;

        #endregion

        #region Methods

        /// <summary>
        /// Defines a <see cref="SerializedType"/> instance for the specified <paramref name="type"/> and all its nested types.
        /// </summary>
        /// <param name="type">The type whose metadata should be preserved along with its nested types.</param>
        /// <returns>A <see cref="SerializedType"/> instance that represents the specified <paramref name="type"/>.</returns>
        public static SerializedType WithNestedTypes([DynamicallyAccessedMembers(neededMembers | DynamicallyAccessedMembers.AllNestedTypes)] Type type)
            => new SerializedType(type);

        #endregion
    }
}