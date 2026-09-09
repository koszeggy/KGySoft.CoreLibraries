#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Program.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
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
#if NETCOREAPP
using System.Runtime.InteropServices;
#endif
#if AOT
using System.Runtime.Versioning;
#endif

using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;

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
                    TestStatus.Skipped => ConsoleColor.Cyan,
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
#elif AOT
            $"{((TargetFrameworkAttribute)Attribute.GetCustomAttribute(typeof(Program).Assembly, typeof(TargetFrameworkAttribute)))!.FrameworkDisplayName} ({RuntimeInformation.ProcessArchitecture})";
#elif NETCOREAPP
            $".NET Core {Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location))} ({RuntimeInformation.ProcessArchitecture})";
#else
            $"{RuntimeInformation.FrameworkDescription}";
#endif

        #endregion

        #endregion

        #region Methods

        #region Internal Methods

        internal static void Main(string[] args)
        {
            //args = ["TestName=GenerateDynamicDependencies", /*"ClassName=BinarySerializerTest"*/];

            // This executes all tests. Can be useful for .NET 3.5, which is executed on .NET 4.x runtime otherwise.
            // It is useful also for testing the library in AOT mode after publishing with the PublishAot option.
            // Filtering can be done by arguments (see TestRunConfig.ProcessArgs)
            ConsoleColor origColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(FrameworkVersion);
            var config = new TestRunConfig(args);
            ConsoleWriter = Console.Out;
            ITestResult result;

            if (config.FallbackRunnerRequired)
            {
                Console.WriteLine("Executing tests by the fallback test runner...");
                var runner = new FallbackTestRunner(config);
                result = runner.Run(new ConsoleTestReporter());
            }
            else
            {
                Console.WriteLine("Executing tests by NUnit test runner...");
                var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
                runner.Load(typeof(Program).Assembly, new Dictionary<string, object>());
                result = runner.Run(new ConsoleTestReporter(), config.TestFilter);
            }

            Console.ForegroundColor = result.FailCount > 0 ? ConsoleColor.Red
                : result.InconclusiveCount > 0 || result.WarningCount > 0 ? ConsoleColor.Yellow
                : result.PassCount == 0 ? ConsoleColor.Cyan
                : ConsoleColor.Green;

            Console.WriteLine($"Passed: {result.PassCount}; Failed: {result.FailCount}; Inconclusive: {result.InconclusiveCount}; Skipped: {result.SkipCount}; Warnings: {result.WarningCount}");
            if (!String.IsNullOrEmpty(result.Message))
                Console.WriteLine($"Message: {result.Message}");
            ProcessChildren(result.Children);
            Console.ForegroundColor = origColor;
        }

        #endregion

        #region Private Methods

        private static void ProcessChildren(IEnumerable<ITestResult> children)
        {
            foreach (ITestResult child in children)
            {
                if (child.ResultState == ResultState.Warning)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine();
                    Console.WriteLine("====================================");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{child.Name}: {child.Message}");
                }

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

        #endregion
    }
}
