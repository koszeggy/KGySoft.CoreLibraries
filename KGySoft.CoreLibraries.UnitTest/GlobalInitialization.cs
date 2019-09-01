#if !NETFRAMEWORK
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GlobalInitialization.cs
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
using System.Drawing;
using System.Text;
using KGySoft.Reflection;
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
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            typeof(Bitmap).RegisterTypeConverter<BitmapConverter>();
            typeof(Icon).RegisterTypeConverter<IconConverter>();
        }

        #endregion
    }
}
#endif
