﻿#if NETCOREAPP && !NETCOREAPP3_0_OR_GREATER && WINDOWS
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BitmapConverter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.Drawing;
using System.Globalization;
using System.IO;

using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Needed for <see cref="Bitmap"/> type to be able to be serialized to and from byte[] the same way as in the .NET Framework.
    /// </summary>
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