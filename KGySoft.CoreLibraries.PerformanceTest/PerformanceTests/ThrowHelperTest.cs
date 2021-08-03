#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ThrowHelperTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests
{
    [TestFixture]
    public class ThrowHelperTest
    {
        #region TestClass class

        private class TestClass
        {
            #region Methods

            internal int RegularThrow(int value)
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));
                return value + 1;
            }

            internal int ThrowByHelper(int value)
            {
                if (value < 0)
                    Throw.ArgumentOutOfRangeException(Argument.value);
                return value + 1;
            }

            #endregion
        }

        #endregion

        #region Methods

        [Test]
        public void TestThrow()
        {
            var test = new TestClass();
            const int max = 10_000;
            new PerformanceTest { Iterations = 1000, Repeat = 3 }
                .AddCase(() =>
                {
                    int n = 0;
                    for (int i = 0; i < max; i++)
                        n = test.RegularThrow(n);
                }, nameof(test.RegularThrow))
                .AddCase(() =>
                {
                    int n = 0;
                    for (int i = 0; i < max; i++)
                        n = test.ThrowByHelper(n);
                }, nameof(test.ThrowByHelper))
                .AddCase(() => { test.ThrowByHelper(-1); }, $"{nameof(test.ThrowByHelper)} Error")
                .DoTest()
                .DumpResults(Console.Out, forceShowReturnSizes: true);
        }

        #endregion
    }
}
