//---------------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryTypeConverter.cs" company="QVA Development">
//   Copyright © QVA Development 2018. All rights reserved. Confidential.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

#region Usings

using System;
using System.ComponentModel;
using System.Globalization;

using KGySoft.Libraries;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Serialization;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert any kind of objects to and from base64 encoded string or byte array representations.
    /// </summary>
    /// <seealso cref="TypeConverter" />
    public class BinaryTypeConverter : TypeConverter
    {
        #region Methods

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="destinationType">A <see cref="Type" /> that represents the type you want to convert to.</param>
        /// <returns><see langword="true" /> if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType.In(Reflector.StringType, Reflector.ByteArrayType) || base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="sourceType">A <see cref="Type" /> that represents the type you want to convert from.</param>
        /// <returns><see langword="true" /> if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType.In(Reflector.StringType, Reflector.ByteArrayType) || base.CanConvertFrom(context, sourceType);

        /// <summary>
        /// Converts the given value object to the specified type.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object" /> to convert.</param>
        /// <param name="destinationType">The <see cref="Type" /> to convert the <paramref name="value" /> parameter to. The <see cref="BinaryTypeConverter"/> supports string and byte array target types.</param>
        /// <returns>An <see cref="object" /> that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (!destinationType.In(Reflector.StringType, Reflector.ByteArrayType))
                return base.ConvertTo(context, culture, value, destinationType);
            byte[] result = BinarySerializer.Serialize(value);
            return destinationType == Reflector.ByteArrayType ? (object)result : Convert.ToBase64String(result);
        }

        /// <summary>
        /// Converts the given object to its original type.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">The <see cref="CultureInfo" /> to use as the current culture. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object"/> to convert.</param>
        /// <returns>
        /// An <see cref="object" /> that represents the converted value.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            byte[] bytes = null;
            if (value is string s)
                bytes = Convert.FromBase64String(s);
            else if (value?.GetType() == Reflector.ByteArrayType) // cast is dangerous: works also from sbyte[] so type check must be performed
                bytes = (byte[])value;

            return bytes != null ? BinarySerializer.Deserialize(bytes) : base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}
