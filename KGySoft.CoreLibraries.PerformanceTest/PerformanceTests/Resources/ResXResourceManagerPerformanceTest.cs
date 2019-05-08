#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceManagerPerformanceTest.cs
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
using System.Globalization;
using System.Resources;

using KGySoft.Resources;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Resources
{
    [TestFixture]
    public class ResXResourceManagerPerformanceTest
    {
        #region Methods

        [Test]
        public void GetObject()
        {
            var inv = CultureInfo.InvariantCulture;
            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var refManager = new ResourceManager("KGySoft.CoreLibraries.Resources.TestResourceResX", GetType().Assembly);
            var manager = new ResXResourceManager("TestResourceResX", GetType().Assembly);
            new PerformanceTest<object>
            {
                TestName = "GetObject Invariant Test",
                Iterations = 1000000,
                Repeat = 5
            }
                .AddCase(() => refManager.GetObject("TestString", inv), "ResourceManager")
                .AddCase(() => manager.GetObject("TestString", inv), "ResXResourceManager")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<object>
            {
                TestName = "GetObject fallback to invariant",
                Iterations = 1000000,
                Repeat = 5
            }
                .AddCase(() => refManager.GetObject("TestString", hu), "ResourceManager")
                .AddCase(() => manager.GetObject("TestString", hu), "ResXResourceManager")
                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
