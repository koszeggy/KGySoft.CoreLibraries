//---------------------------------------------------------------------------------------------------------------------
// <copyright file="VersionConverter.cs" company="QVA Development">
//   Copyright © QVA Development 2018. All rights reserved. Confidential.
// </copyright>
//---------------------------------------------------------------------------------------------------------------------

#region Usings

using System;
using System.ComponentModel;
using System.Globalization;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Type converter for <see cref="Version"/> class.
    /// </summary>
    public class VersionConverter : TypeConverter
    {
        #region Methods

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="destinationType">A <see cref="Type" /> that represents the type you want to convert to.</param>
        /// <returns><see langword="true" /> if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => (destinationType == typeof(string)) || base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object" /> to convert.</param>
        /// <param name="destinationType">The <see cref="Type" /> to convert the <paramref name="value" /> parameter to.</param>
        /// <returns>An <see cref="object" /> that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            Version version = value as Version;
            if (destinationType == typeof(string) && version != null)
                return version.ToString();
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="sourceType">A <see cref="Type" /> that represents the type you want to convert from.</param>
        /// <returns><see langword="true" /> if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object" /> to convert.</param>
        /// <returns>An <see cref="object" /> that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                return null;

            if (value is string str)
                return new Version(str);
            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
}
