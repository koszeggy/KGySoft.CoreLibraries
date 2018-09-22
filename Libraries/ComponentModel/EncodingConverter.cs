#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EncodingConverter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Supports conversion from string to <see cref="Encoding"/> type.
    /// </summary>
    public sealed class EncodingConverter : TypeConverter
    {
        #region Fields

        private static Dictionary<string, Encoding> encodingByName;

        private static Encoding[] encodings;

        #endregion

        #region Properties

        private static Encoding[] Encodings
        {
            get
            {
                if (encodings == null)
                {
                    EncodingInfo[] infos = Encoding.GetEncodings();
                    encodings = new Encoding[infos.Length];
                    for (int i = 0; i < infos.Length; i++)
                    {
                        encodings[i] = infos[i].GetEncoding();
                    }
                    Array.Sort(encodings, (e1, e2) => e1.CodePage.CompareTo(e2.CodePage));
                }
                return encodings;
            }
        }

        private static Dictionary<string, Encoding> EncodingByName
        {
            get
            {
                if (encodingByName == null)
                {
                    encodingByName = new Dictionary<string, Encoding>();
                    foreach (Encoding e in Encodings)
                    {
                        encodingByName.Add($"{e.CodePage} | {e.EncodingName}", e);
                    }
                }
                return encodingByName;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns whether this converter can convert the object to the specified type, using the specified context.
        /// </summary>
        /// <returns>
        /// true if this converter can perform the conversion; otherwise, false.
        /// </returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context.
        ///                 </param><param name="destinationType">A <see cref="T:System.Type"/> that represents the type you want to convert to.
        ///                 </param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string)) || (destinationType == typeof(int)) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given value object to the specified type, using the specified context and culture information.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Object"/> that represents the converted value.
        /// </returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context.
        ///                 </param><param name="culture">A <see cref="T:System.Globalization.CultureInfo"/>. If null is passed, the current culture is assumed.
        ///                 </param><param name="value">The <see cref="T:System.Object"/> to convert.
        ///                 </param><param name="destinationType">The <see cref="T:System.Type"/> to convert the <paramref name="value"/> parameter to.
        ///                 </param><exception cref="T:System.ArgumentNullException">The <paramref name="destinationType"/> parameter is null.
        ///                 </exception><exception cref="T:System.NotSupportedException">The conversion cannot be performed.
        ///                 </exception>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            Encoding encoding = value as Encoding;
            if (encoding != null)
            {
                if (destinationType == typeof(int))
                {
                    return encoding.CodePage;
                }
                if (destinationType == typeof(string))
                {
                    return String.Format("{0} | {1}", encoding.CodePage, encoding.EncodingName);
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <returns>
        /// true if this converter can perform the conversion; otherwise, false.
        /// </returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context.
        ///                 </param><param name="sourceType">A <see cref="T:System.Type"/> that represents the type you want to convert from.
        ///                 </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string)) || (sourceType == typeof(int)) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Object"/> that represents the converted value.
        /// </returns>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"/> that provides a format context.
        ///                 </param><param name="culture">The <see cref="T:System.Globalization.CultureInfo"/> to use as the current culture.
        ///                 </param><param name="value">The <see cref="T:System.Object"/> to convert.
        ///                 </param><exception cref="T:System.NotSupportedException">The conversion cannot be performed.
        ///                 </exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is int)
            {
                return Encoding.GetEncoding((int)value);
            }
            string str = value as string;
            if (str != null)
            {
                // 1: by full string value representation
                Encoding encoding;
                if (EncodingByName.TryGetValue(str, out encoding))
                    return encoding;

                // 2: by code
                int codepage;
                if (Int32.TryParse(str, out codepage))
                    return Encoding.GetEncoding(codepage);

                // 3: by display name
                if ((encoding = Encodings.FirstOrDefault(e => e.EncodingName == str)) != null)
                    return encoding;

                // 4: by web name (may throw ArgumentException)
                return Encoding.GetEncoding(str);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Returns whether this object supports a standard set of values that can be picked from a list, using the specified context.
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        /// <summary>
        /// Returns a collection of standard values for the data type this type converter is designed for when provided with a format context.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => new StandardValuesCollection(Encodings);

        #endregion
    }
}
