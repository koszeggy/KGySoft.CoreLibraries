#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CustomSerializationBinder.cs
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
    /// Provides a very simple customizable <see cref="SerializationBinder"/> that can convert <see cref="Type"/> to and from <see cref="string">string</see>
    /// by using assignable delegate properties.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>When serializing, you can assign the <see cref="AssemblyNameResolver"/> and <see cref="TypeNameResolver"/> properties to customize
    /// the assembly and type names of a <see cref="Type"/> to be written into the serialization stream.</para>
    /// <para>When deserializing, you can assign the <see cref="TypeResolver"/> properties to return a type from an assembly-type name pair.</para>
    /// <para>If the properties above are not assigned or when they return <see langword="null"/>, then the consumer <see cref="IFormatter"/> instance will use its internal resolve logic.</para>
    /// <note type="security"><para>If <see cref="TypeResolver"/> is not assigned or can return <see langword="null"/>, then the consumer <see cref="IFormatter"/> instance
    /// may load assemblies during the deserialization. If the deserialization stream is not from a trusted source, then you should
    /// never return <see langword="null"/>&#160;from the assigned delegate of the <see cref="TypeResolver"/> property. Instead, throw an
    /// exception if a type could not be resolved.</para>
    /// <para>See the security notes at the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for more details.</para></note>
    /// </remarks>
    /// <example>
    /// <code lang="C#"><![CDATA[
    /// // deserializing a renamed type
    /// var formatter = new BinaryFormatter(); // or a BinarySerializationFormatter
    /// formatter.Binder = new CustomSerializationBinder
    /// {
    ///     TypeResolver = (asmName, typeName) =>
    ///         typeName == "MyNamespace.MyOldClass" ? typeof(MyNewClass) : null
    /// };
    ///
    /// return (MyNewClass)formatter.Deserialize(streamContainingOldData);]]></code>
    /// <note type="tip">If the inner structure of the type has also been changed, then you can use the
    /// <see cref="CustomSerializerSurrogateSelector"/> class.</note>
    /// </example>
    /// <seealso cref="ForwardedTypesSerializationBinder"/>
    /// <seealso cref="WeakAssemblySerializationBinder"/>
    /// <seealso cref="BinarySerializationFormatter"/>
    public sealed class CustomSerializationBinder : SerializationBinder, ISerializationBinder
    {
        #region Properties

        /// <summary>
        /// Gets or sets the custom assembly name resolver logic. It is invoked by the <see cref="BindToName">BindToName</see> method.
        /// If returns a non-<see langword="null"/>&#160;value, then it will be stored as the custom assembly name for the <see cref="Type"/> specified by the delegate argument.
        /// </summary>
        public Func<Type, string?>? AssemblyNameResolver { get; set; }

        /// <summary>
        /// Gets or sets the custom type name resolver logic. It is invoked by the <see cref="BindToName">BindToName</see> method.
        /// If returns a non-<see langword="null"/>&#160;value, then it will be stored as the custom full type name (without the assembly name) for the <see cref="Type"/> specified by the delegate argument.
        /// </summary>
        public Func<Type, string?>? TypeNameResolver { get; set; }

        /// <summary>
        /// Gets or sets the custom <see cref="Type"/> resolver logic. It is invoked by the <see cref="BindToType">BindToType</see> method
        /// passing the stored assembly and type names in the delegate arguments, respectively.
        /// If returns <see langword="null"/>&#160;the formatter will attempt to resolve the names by its default logic.
        /// </summary>
        public Func<string, string, Type?>? TypeResolver { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Binds a <see cref="Type"/> to an <paramref name="assemblyName"/> and <paramref name="typeName"/>.
        /// This implementation sets <paramref name="assemblyName"/> by using the <see cref="AssemblyNameResolver"/> property,
        /// and sets <paramref name="typeName"/> by using the <see cref="TypeNameResolver"/> property.
        /// </summary>
        /// <param name="serializedType">The type of the object the formatter creates a new instance of.</param>
        /// <param name="assemblyName">The <see cref="string">string</see>, which will represent the <see cref="Assembly"/> name in the serialized data.
        /// Can return <see langword="null"/>&#160;to provide a default name.</param>
        /// <param name="typeName">The <see cref="string">string</see>, which will represent the <see cref="Type"/> name in the serialized data.
        /// Can return <see langword="null"/>&#160;to provide a default name.</param>
#if !NET35
        override
#endif
        public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
        {
            assemblyName = AssemblyNameResolver?.Invoke(serializedType);
            typeName = TypeNameResolver?.Invoke(serializedType);
        }

        /// <summary>
        /// Gets a <see cref="Type"/> associated by the provided <paramref name="assemblyName"/> and <paramref name="typeName"/>.
        /// This implementation uses the <see cref="TypeResolver"/> property to determine the result <see cref="Type"/>.
        /// </summary>
        /// <param name="assemblyName">Specifies the <see cref="Assembly"/> name of the serialized object.</param>
        /// <param name="typeName">Specifies the <see cref="Type"/> name of the serialized object.</param>
        /// <returns>The <see cref="Type"/> to be created by the formatter or <see langword="null"/>&#160;to use the default binding logic.</returns>
        public override Type? BindToType(string assemblyName, string typeName)
            => TypeResolver?.Invoke(assemblyName, typeName);

        #endregion
    }
}
