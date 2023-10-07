#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinaryTypeConverter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert any <see cref="object"/> to and from base64 encoded <see cref="string"/> or <see cref="Array">byte array</see> representations.
    /// </summary>
    /// <remarks>
    /// <note>This converter uses the <see cref="BinarySerializationFormatter"/> class in safe mode internally. The <see cref="ConvertFrom"/> method may
    /// throw a <see cref="SerializationException"/> if the serialization stream contains any type name to resolve other than the <see cref="Type"/> specified
    /// in the <see cref="BinaryTypeConverter(System.Type)">constructor</see>. So it can be used for types only that do not use any types internally
    /// that are not supported natively by the <see cref="BinarySerializationFormatter"/> class.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for the natively supported types.</note>
    /// </remarks>
    /// <seealso cref="TypeConverter" />
    public class BinaryTypeConverter : TypeConverter
    {
        #region Fields

        private static readonly Type[] supportedTypes =
        {
            Reflector.StringType,
            Reflector.ByteArrayType,
#if !(NETSTANDARD2_0 || NETCOREAPP2_0)
            typeof(InstanceDescriptor)
#endif
        };

        private static MethodInfo? deserializeMethod;

        #endregion

        #region Properties

        #region Protected Properties

        /// <summary>
        /// Gets the type of the member this <see cref="BinaryTypeConverter"/> instance is referring to.
        /// </summary>
        /// <value>This property returns the type that was specified in the <see cref="BinaryTypeConverter(System.Type)">constructor</see>.</value>
        protected Type? Type { get; }

        #endregion

        #region Private Properties

        private static MethodInfo DeserializeMethod => deserializeMethod ??= typeof(BinarySerializer)
            .GetMember(nameof(BinarySerializer.Deserialize), MemberTypes.Method, BindingFlags.Public | BindingFlags.Static)
            .Cast<MethodInfo>()
            .First(mi => !mi.IsGenericMethodDefinition && mi.GetParameters()
                .Select(p => p.ParameterType)
                .SequenceEqual(new[] { Reflector.ByteArrayType, Reflector.IntType, typeof(BinarySerializationOptions), typeof(Type[]) }))!;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref='BinaryTypeConverter'/> class.
        /// </summary>
        /// <param name="type">The type this converter is created for. If <see langword="null"/>, then the <see cref="ConvertFrom">ConvertFrom</see> method
        /// will support only types that are natively supported by <see cref="BinarySerializationFormatter"/>.</param>
        public BinaryTypeConverter(Type? type) => Type = type;

        /// <summary>
        /// Initializes a new instance of the <see cref='BinaryTypeConverter'/> class.
        /// </summary>
        public BinaryTypeConverter() { }

        #endregion

        #region Methods

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <param name="context">In this type converter this parameter is ignored.</param>
        /// <param name="destinationType">A <see cref="Type" /> that represents the type you want to convert to.
        /// This type converter supports <see cref="string"/> and <see cref="Array">byte[]</see> types.</param>
        /// <returns><see langword="true"/> if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            => destinationType.In(supportedTypes) || base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">In this type converter this parameter is ignored.</param>
        /// <param name="sourceType">A <see cref="Type" /> that represents the type you want to convert from.
        /// This type converter supports <see cref="string"/> and <see cref="Array">byte[]</see> types.</param>
        /// <returns><see langword="true"/> if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType.In(supportedTypes) || base.CanConvertFrom(context, sourceType);

        /// <summary>
        /// Converts the given value object to the specified type.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object" /> to convert.</param>
        /// <param name="destinationType">A <see cref="Type" /> that represents the type you want to convert to.
        /// This type converter supports <see cref="string"/> and <see cref="Array">byte[]</see> types.</param>
        /// <returns>An <see cref="object" /> that represents the converted value.</returns>
        [SecuritySafeCritical]
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (!destinationType.In(supportedTypes))
                return base.ConvertTo(context, culture, value, destinationType);
            byte[] result = BinarySerializer.Serialize(value, BinarySerializationOptions.RecursiveSerializationAsFallback);
            return destinationType == Reflector.ByteArrayType ? result
                : destinationType == Reflector.StringType ? Convert.ToBase64String(result)
                : new InstanceDescriptor(DeserializeMethod, new object[] { result, 0, BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, new[] { destinationType } });
        }

        /// <summary>
        /// Converts the given object to its original type.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">The <see cref="CultureInfo" /> to use as the current culture. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object"/> to convert.
        /// This type converter supports <see cref="string"/> and <see cref="Array">byte[]</see> types.</param>
        /// <returns>An <see cref="object" /> that represents the converted value.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            byte[]? bytes = null;
            if (value is string s)
                bytes = Convert.FromBase64String(s);
            else if (value?.GetType() == Reflector.ByteArrayType) // cast is dangerous: works also from sbyte[] so type check must be performed
                bytes = (byte[])value;

            return bytes != null
                ? BinarySerializer.Deserialize(bytes, 0, BinarySerializationOptions.SafeMode, (IEnumerable<Type>?)(Type is null ? null : new[] { Type }))
                : base.ConvertFrom(context!, culture!, value!);
        }

        #endregion
    }
}
