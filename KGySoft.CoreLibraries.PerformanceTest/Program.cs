#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Program.cs
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
using System.Collections.Generic;
#if NETCOREAPP
using System.IO;
#elif !NETFRAMEWORK
using System.Runtime.InteropServices;
#endif

using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class Program
    {
        #region Properties

        private static string FrameworkVersion =>
#if NETFRAMEWORK
            $".NET Framework Runtime {typeof(object).Assembly.ImageRuntimeVersion}";
#elif NETCOREAPP
            $".NET Core {Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location))}";
#else
            $"{RuntimeInformation.FrameworkDescription})";
#endif

        #endregion

        #region Methods

        internal static void Main()
        {
            // This executes all tests. Can be useful for .NET 3.5, which is executed on .NET 4.x runtime otherwise.
            // Filtering can be done by reflecting NUnit.Framework.Internal.Filters.TestNameFilter,
            // or just calling the method to debug directly
            Console.WriteLine(FrameworkVersion);
            var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
            runner.Load(typeof(Program).Assembly, new Dictionary<string, object>());
            Console.WriteLine("Executing tests...");
            ITestResult result = runner.Run(null, TestFilter.Empty);
            Console.WriteLine($"Passed: {result.PassCount}; Failed: {result.FailCount}; Skipped: {result.SkipCount}");
            Console.WriteLine($"Message: {result.Message}");
            ProcessChildren(result.Children);
        }

        private static void ProcessChildren(IEnumerable<ITestResult> children)
        {
            foreach (ITestResult child in children)
            {
                if (child.HasChildren)
                {
                    ProcessChildren(child.Children);
                    continue;
                }

                if (child.FailCount == 0)
                    continue;

                Console.WriteLine("====================================");
                Console.WriteLine($"{child.Name}: {child.Message}");
                Console.WriteLine(child.StackTrace);
                if (!child.Output.IsNullOrEmpty())
                    Console.WriteLine($"Output: {child.Output}");

                for (int i = 0; i < child.AssertionResults.Count; i++)
                    Console.WriteLine($"Assertion #{i}: {child.AssertionResults[i].Message}{Environment.NewLine}{child.AssertionResults[i].StackTrace}{Environment.NewLine}");
            }
        }

        #endregion
    }
}
