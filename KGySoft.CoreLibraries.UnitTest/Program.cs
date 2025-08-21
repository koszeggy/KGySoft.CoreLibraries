#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Program.cs
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
using System.Collections.Generic;
using System.IO;

using KGySoft.CoreLibraries.UnitTests.Reflection;

using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

#endregion

namespace KGySoft.CoreLibraries
{
    internal static class Program
    {
        #region Nested Classes

        private class ConsoleTestReporter : ITestListener
        {
            #region Methods

            public void TestStarted(ITest test)
            {
                if (test.HasChildren)
                    return;

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"{test.Name}...");
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            public void TestFinished(ITestResult result)
            {
                if (result.HasChildren)
                    return;

                var state = result.ResultState;
                var status = state.Status;
                if (status == TestStatus.Skipped && state.Site == FailureSite.Parent)
                    return;

                var message = result.Message;
                ConsoleColor origColor = Console.ForegroundColor;
                Console.ForegroundColor = status switch
                {
                    TestStatus.Failed => ConsoleColor.Red,
                    TestStatus.Passed => ConsoleColor.Green,
                    TestStatus.Skipped => ConsoleColor.DarkCyan,
                    _ => ConsoleColor.Yellow
                };

                Console.WriteLine(status);
                if (!String.IsNullOrEmpty(message))
                    Console.WriteLine($"Message: {message}");

                Console.ForegroundColor = origColor;
            }

            public void TestOutput(TestOutput output)
            {
            }

            public void SendMessage(TestMessage message)
            {
            }

            #endregion
        }

        #endregion

        #region Properties

        #region Internal Properties

        internal static TextWriter ConsoleWriter { get; private set; }

        #endregion

        #region Private Properties

        private static string FrameworkVersion =>
#if NETFRAMEWORK
            $".NET Framework Runtime {typeof(object).Assembly.ImageRuntimeVersion}";
#elif NETCOREAPP
            $".NET Core {Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location))}";
#else
            $"{RuntimeInformation.FrameworkDescription}";
#endif

        #endregion

        #endregion

        #region Methods

        internal static void Main()
        {
            // This executes all tests. Can be useful for .NET 3.5, which is executed on .NET 4.x runtime otherwise.
            // Filtering can be done by reflecting NUnit.Framework.Internal.Filters.TestNameFilter,
            // or just calling the method to debug directly
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(FrameworkVersion);

            new ReflectorTest().StructComplexConstructionByCtorInfoUnsafe();
            return;

            var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
            runner.Load(typeof(Program).Assembly, new Dictionary<string, object>());
            Console.WriteLine("Executing tests...");
            ConsoleWriter = Console.Out;
            ITestResult result = runner.Run(new ConsoleTestReporter(), TestFilter.Empty);
            Console.ForegroundColor = result.FailCount > 0 ? ConsoleColor.Red
                : result.InconclusiveCount > 0 ? ConsoleColor.Yellow
                : ConsoleColor.Green;

            Console.WriteLine($"Passed: {result.PassCount}; Failed: {result.FailCount}; Inconclusive: {result.InconclusiveCount}; Skipped: {result.SkipCount}");
            if (!String.IsNullOrEmpty(result.Message))
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

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine();
                Console.WriteLine("====================================");
                Console.ForegroundColor = ConsoleColor.Red;
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
