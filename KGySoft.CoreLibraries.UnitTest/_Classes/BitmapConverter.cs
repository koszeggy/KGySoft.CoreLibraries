#if NETCOREAPP
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BitmapConverter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;

using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    internal class BitmapConverter : TypeConverter
    {
        #region Methods

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == Reflector.ByteArrayType || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is byte[] byteArray)
                return new Bitmap(new MemoryStream(byteArray));
            return base.ConvertFrom(context, culture, value);
        }

        #endregion
    }
} 
#endif