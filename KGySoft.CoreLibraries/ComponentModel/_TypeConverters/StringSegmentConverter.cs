#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringSegmentConverter.cs
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
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Security;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref="StringSegment"/> instances to and from <see cref="string">string</see>.
    /// </summary>
    public class StringSegmentConverter : TypeConverter
    {
        #region Fields

        private static MethodInfo? asSegmentMethod;

        #endregion

        #region Properties

        private static MethodInfo AsSegmentMethod
            => asSegmentMethod ??= typeof(StringExtensions).GetMethod(nameof(StringExtensions.AsSegment), new[] { Reflector.StringType })!;

        #endregion

        #region Methods

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="destinationType">A <see cref="Type" /> that represents the type you want to convert to.
        /// This type converter supports <see cref="string"/> type only.</param>
        /// <returns><see langword="true"/>&#160;if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            => destinationType == Reflector.StringType || destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="StringSegment" /> instance to convert.</param>
        /// <param name="destinationType">The <see cref="Type" /> to convert the <paramref name="value" /> parameter to.
        /// This type converter supports <see cref="string"/> type only.</param>
        /// <returns>An <see cref="object" /> that represents the converted value.</returns>
        [SecuritySafeCritical]
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
            => value is StringSegment stringSegment
                ? destinationType == Reflector.StringType
                    ? stringSegment.ToString()
                    : new InstanceDescriptor(AsSegmentMethod, new[] { stringSegment.ToString() })
                : base.ConvertTo(context, culture, value, destinationType);

        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="sourceType">A <see cref="Type" /> that represents the type you want to convert from.
        /// This type converter supports <see cref="string"/> type only.</param>
        /// <returns><see langword="true"/>&#160;if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == Reflector.StringType || base.CanConvertFrom(context, sourceType);

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object" /> to convert.
        /// This type converter supports <see cref="string"/> type only.</param>
        /// <returns>A <see cref="StringSegment" /> instance that represents the converted value.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
            => value switch
            {
                string str => str.AsSegment(),
                null => StringSegment.Null,
                _ => base.ConvertFrom(context!, culture!, value)!
            };

        #endregion
    }
}
