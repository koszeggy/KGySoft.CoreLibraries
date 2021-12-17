#if !NETFRAMEWORK
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GlobalInitialization.cs
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

using System.Drawing;
#if NETCOREAPP3_0_OR_GREATER
using System.Text;
#endif

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    [SetUpFixture]
    public class GlobalInitialization
    {
        #region Methods

        [OneTimeSetUp]
        public void Initialize()
        {
#if NETCOREAPP3_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            typeof(Bitmap).RegisterTypeConverter<BitmapConverter>();
            typeof(Icon).RegisterTypeConverter<IconConverter>();
        }

        #endregion
    }
}
#endif
