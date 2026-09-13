#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TestRunConfig.cs
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
#if !NETSTANDARD_TEST
using System.Runtime.CompilerServices;
#endif

using KGySoft.Reflection;

using NUnit.Framework.Internal;

#endregion

#nullable enable

namespace KGySoft.CoreLibraries
{
    internal class TestRunConfig
    {
        #region Fields

        private bool forceFallbackRunner;

        #endregion

        #region Properties

        internal string? TestName { get; set; }

        internal string? ClassName { get; set; }

        internal TestFilter TestFilter => field ??=
            TestName is string testName ? (TestFilter)Reflector.CreateInstance(Reflector.ResolveType("NUnit.Framework.Internal.Filters.TestNameFilter")!, testName)
            : ClassName is string className ? (TestFilter)Reflector.CreateInstance(Reflector.ResolveType("NUnit.Framework.Internal.Filters.ClassNameFilter")!, className)
            : TestFilter.Empty;

        internal bool FallbackRunnerRequired => forceFallbackRunner
#if !NETSTANDARD_TEST
            || !RuntimeFeature.IsDynamicCodeSupported // AOT mode
#endif
            || (TestName != null && ClassName != null) // Both filters are set, but NUnit does not support combining them
            || ClassName?.IndexOf('.') < 0; // ClassName is set without namespace

        #endregion

        #region Constructors

        internal TestRunConfig(string[] args) => ProcessArgs(args);

        #endregion

        #region Methods

        private void ProcessArgs(string[] args)
        {
            if ("-?".In(args) || "-h".ContainsAny(StringComparison.OrdinalIgnoreCase, args) || "--help".ContainsAny(StringComparison.OrdinalIgnoreCase, args))
            {
                Console.WriteLine("Available command line arguments:");
                Console.WriteLine("  -? or -h or --help  Displays this help message.");
#if NETCOREAPP3_0_OR_GREATER
                Console.WriteLine("  -f                  Forces the use of the fallback runner.");
#endif
                Console.WriteLine("  TestName=<name>     Runs only tests with the specified name.");
                Console.WriteLine("  ClassName=<name>    Runs only tests in the specified class.");
                Environment.Exit(-1);
            }

            foreach (string arg in args)
            {
                if (arg.StartsWith("TestName=", StringComparison.OrdinalIgnoreCase))
                {
                    TestName = arg.Substring(arg.IndexOf('=') + 1);
                    Console.WriteLine($"Applying test name filter: {TestName}");
                }
                else if (arg.StartsWith("ClassName=", StringComparison.OrdinalIgnoreCase))
                {
                    ClassName = arg.Substring(arg.IndexOf('=') + 1);
                    Console.WriteLine($"Applying class name filter: {ClassName}");
                }
#if NETCOREAPP3_0_OR_GREATER
                else if (arg == "-f")
                    forceFallbackRunner = true;
#endif
                else
                {
                    Console.WriteLine($"Error: Unknown argument: {arg}");
                    Environment.Exit(-1);
                }
            }
        }

        #endregion
    }
}
