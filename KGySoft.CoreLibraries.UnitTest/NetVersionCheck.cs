#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: NetVersionCheck.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    [TestFixture]
    public class NetVersionCheck : TestBase
    {
        #region Methods

        [Test]
        public void CheckFrameworkVersion() => CheckTestingFramework();

        #endregion
    }
}
