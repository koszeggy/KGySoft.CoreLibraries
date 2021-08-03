#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ISerializationBinder.cs
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
using System.Reflection;
using System.Runtime.Serialization;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Represents a binder that can convert <see cref="Type"/> to and from <see cref="string">string</see> for serialization.
    /// </summary>
    /// <remarks>
    /// <para>Provides the same functionality as the <see cref="SerializationBinder"/> class but makes the
    /// <see cref="BindToName">BindToName</see> method available also in .NET Framework 3.5.</para>
    /// <para>If a binder class is used in <see cref="BinarySerializationFormatter"/> and the class is derived from <see cref="SerializationBinder"/> and implements
    /// this interface, then the <see cref="BinarySerializationFormatter"/> class is able to bind a <see cref="Type"/> in both directions even in .NET 3.5</para>
    /// </remarks>
    public interface ISerializationBinder
    {
        #region Methods

        /// <summary>
        /// Binds a <see cref="Type"/> to an <paramref name="assemblyName"/> and <paramref name="typeName"/>.
        /// </summary>
        /// <param name="serializedType">The type of the object the formatter creates a new instance of.</param>
        /// <param name="assemblyName">The <see cref="string">string</see>, which will represent the <see cref="Assembly"/> name in the serialized data.
        /// Can return <see langword="null"/>&#160;to provide a default name.</param>
        /// <param name="typeName">The <see cref="string">string</see>, which will represent the <see cref="Type"/> name in the serialized data.
        /// Can return <see langword="null"/>&#160;to provide a default name.</param>
        void BindToName(Type serializedType, out string? assemblyName, out string? typeName);

        /// <summary>
        /// Gets a <see cref="Type"/> associated by the provided <paramref name="assemblyName"/> and <paramref name="typeName"/>.
        /// </summary>
        /// <param name="assemblyName">Specifies the <see cref="Assembly"/> name of the serialized object.</param>
        /// <param name="typeName">Specifies the <see cref="Type"/> name of the serialized object.</param>
        /// <returns>The <see cref="Type"/> to be created by the formatter or <see langword="null"/>&#160;to use the default binding logic.</returns>
        Type? BindToType(string assemblyName, string typeName);

        #endregion
    }
}