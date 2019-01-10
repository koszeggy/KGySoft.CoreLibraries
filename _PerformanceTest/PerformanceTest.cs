#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PerformanceTest.cs
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

using NUnit.Framework;

#endregion

namespace _PerformanceTest
{
    internal class PerformanceTest : PerformanceTest<Action, object>
    {
        #region Methods

        #region Static Methods

        internal static void CheckTestingFramework()
        {
#if NET35
            if (typeof(object).Assembly.GetName().Version != new Version(2, 0, 0, 0))
                Assert.Inconclusive("mscorlib version does not match to .NET 3.5: {0}. Add a global <TargetFrameworkVersion>v3.5</TargetFrameworkVersion> to csproj and try again", typeof(object).Assembly.GetName().Version);
#elif NET40 || NET45
            if (typeof(object).Assembly.GetName().Version != new Version(4, 0, 0, 0))
                Assert.Inconclusive("mscorlib version does not match to .NET 4.x: {0}. Add a global <TargetFrameworkVersion> to csproj and try again", typeof(object).Assembly.GetName().Version);
#else
#error unknown .NET version
#endif

        }

        #endregion

        #region Instance Methods

        protected override object Invoke(Action del)
        {
            del.Invoke();
            return null;
        }

        #endregion

        #endregion
    }
}
