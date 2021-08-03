#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EncodingConverter.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref="Encoding"/> instances to and from <see cref="string"/> or <see cref="int"/> representations.
    /// </summary>
    public sealed class EncodingConverter : TypeConverter
    {
        #region Fields

        private static StringKeyedDictionary<Encoding>? encodingByName;
        private static Encoding[]? encodings;

        #endregion

        #region Properties

        private static Encoding[] Encodings
        {
            get
            {
                if (encodings != null)
                    return encodings;

                EncodingInfo[] infos = Encoding.GetEncodings();
                encodings = new Encoding[infos.Length];
                for (int i = 0; i < infos.Length; i++)
                    encodings[i] = infos[i].GetEncoding();

                Array.Sort(encodings, (e1, e2) => e1.CodePage.CompareTo(e2.CodePage));
                return encodings;
            }
        }

        private static StringKeyedDictionary<Encoding> EncodingByName
        {
            get
            {
                if (encodingByName != null)
                    return encodingByName;

                encodingByName = new StringKeyedDictionary<Encoding>();
                foreach (Encoding e in Encodings)
                    encodingByName.Add($"{e.CodePage.ToString(CultureInfo.InvariantCulture)} | {e.EncodingName}", e);

                return encodingByName;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="destinationType">A <see cref="Type" /> that represents the type you want to convert to.
        /// This type converter supports <see cref="string"/> and <see cref="int"/> types.</param>
        /// <returns><see langword="true"/>&#160;if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type destinationType) 
            => destinationType == Reflector.StringType || destinationType == Reflector.IntType || base.CanConvertTo(context, destinationType);

        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="Encoding" /> instance to convert.</param>
        /// <param name="destinationType">The <see cref="Type" /> to convert the <paramref name="value" /> parameter to.
        /// This type converter supports <see cref="string"/> and <see cref="int"/> types.</param>
        /// <returns>An <see cref="object" /> that represents the converted value.</returns>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is not Encoding encoding)
                return base.ConvertTo(context, culture, value, destinationType);

            if (destinationType == Reflector.IntType)
                return encoding.CodePage;

            if (destinationType == Reflector.StringType)
                return $"{encoding.CodePage.ToString(CultureInfo.InvariantCulture)} | {encoding.EncodingName}";

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="sourceType">A <see cref="Type" /> that represents the type you want to convert from.
        /// This type converter supports <see cref="string"/> and <see cref="int"/> types.</param>
        /// <returns><see langword="true"/>&#160;if this converter can perform the conversion; otherwise, <see langword="false" />.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) 
            => sourceType == Reflector.StringType || sourceType == Reflector.IntType || base.CanConvertFrom(context, sourceType);

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <param name="culture">A <see cref="CultureInfo" />. In this converter this parameter is ignored.</param>
        /// <param name="value">The <see cref="object" /> to convert.
        /// This type converter supports <see cref="string"/> and <see cref="int"/> types.</param>
        /// <returns>An <see cref="Encoding" /> instance that represents the converted value.</returns>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            if (value is int codePage)
                return Encoding.GetEncoding(codePage);

            if (value is string name)
            {
                // 1: by full string value representation
                if (EncodingByName.TryGetValue(name, out Encoding? encoding))
                    return encoding;

                // 2: by code
                if (Int32.TryParse(name, out codePage))
                    return Encoding.GetEncoding(codePage);

                // 3: by display name
                if ((encoding = Encodings.FirstOrDefault(e => e.EncodingName == name)) != null)
                    return encoding;

                // 4: by web name (may throw ArgumentException)
                return Encoding.GetEncoding(name);
            }

            return base.ConvertFrom(context!, culture!, value);
        }

        /// <summary>
        /// Returns whether this object supports a standard set of values that can be picked from a list, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <returns>This method always returns <see langword="true" />.</returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        /// <summary>
        /// Returns a collection of standard values for the data type this type converter is designed for when provided with a format context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this converter this parameter is ignored.</param>
        /// <returns>A <see cref="TypeConverter.StandardValuesCollection" /> that holds a standard set of valid values.</returns>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(Encodings);

        #endregion
    }
}
