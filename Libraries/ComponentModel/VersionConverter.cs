using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Type converter for <see cref="Version"/> class.
    /// </summary>
    public class VersionConverter: TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (destinationType == typeof(string)) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            Version version = value as Version;
            if (destinationType == typeof(string) && version != null)
            {
                return version.ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                return null;

            string str = value as string;
            if (str != null)
            {
                return new Version(str);
            }
            return base.ConvertFrom(context, culture, value);
        }
    }
}
