#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceManagerPerformanceTest.cs
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

        [TestCase("TestString")]
        [TestCase("TestBytes")]
        [TestCase("TestPoint")]
        [TestCase("TestImageEmbedded")]
        [TestCase("TestSoundEmbedded")]
        [TestCase("TestObjectEmbedded")]
        public void GetObject(string name)
        {
            var inv = CultureInfo.InvariantCulture;

            var refManager = new ResourceManager("KGySoft.CoreLibraries.Resources.TestResourceResX", GetType().Assembly);
            var managerCloning = new ResXResourceManager("TestResourceResX", GetType().Assembly) { CloneValues = true };
            var managerNonCloning = new ResXResourceManager("TestResourceResX", GetType().Assembly) { CloneValues = false };

            new PerformanceTest<object>
                {
                    TestName = name,
                    TestTime = 500
                }
                .AddCase(() => refManager.GetObject(name, inv), "ResourceManager")
                .AddCase(() => managerCloning.GetObject(name, inv), "ResXResourceManager CloneValues = true")
                .AddCase(() => managerNonCloning.GetObject(name, inv), "ResXResourceManager CloneValues = false")
                .DoTest()
                .DumpResults(Console.Out, false);
        }

        #endregion
    }
}
