#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PerformanceTest.cs
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

#if NETFRAMEWORK
using System; 
#endif
#if NETCOREAPP
using System.IO; 
#endif
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    internal class PerformanceTest : KGySoft.Diagnostics.PerformanceTest
    {
        #region Properties

        #region Static Properties

        internal static string FrameworkVersion =>
#if NETFRAMEWORK
            $".NET Framework Runtime {typeof(object).Assembly.ImageRuntimeVersion}";
#elif NETCOREAPP
            $".NET Core {Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location))}";
#else
            $"{RuntimeInformation.FrameworkDescription})";
#endif

        #endregion

        #region Instance Properties

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({FrameworkVersion})";
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        internal static void CheckTestingFramework()
        {
#if NET35
            if (typeof(object).Assembly.GetName().Version != new Version(2, 0, 0, 0))
                Assert.Inconclusive($"mscorlib version does not match to .NET 3.5: {typeof(object).Assembly.GetName().Version}. Add a global <TargetFrameworkVersion>v3.5</TargetFrameworkVersion> to csproj and try again");
#elif NET40 || NET45 || NET472
            if (typeof(object).Assembly.GetName().Version != new Version(4, 0, 0, 0))
                Assert.Inconclusive($"mscorlib version does not match to .NET 4.x: {typeof(object).Assembly.GetName().Version}. Add a global <TargetFrameworkVersion> to csproj and try again");
#elif NETFRAMEWORK
#error unknown .NET version
#endif
        }

        #endregion

        #region Instance Methods

        protected override void OnInitialize()
        {
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            CheckTestingFramework();
        }

        #endregion

        #endregion
    }

    internal class PerformanceTest<TResult> : KGySoft.Diagnostics.PerformanceTest<TResult>
    {
        #region Properties

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({PerformanceTest.FrameworkVersion})";
        }

        #endregion

        #region Methods

        protected override void OnInitialize()
        {
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
        }

        #endregion
    }
}
