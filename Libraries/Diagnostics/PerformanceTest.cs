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

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a class for performance tests of <see cref="Action"/> delegate test cases.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <example>
    /// <para>The following example shows the simplest usage for timed tests.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Diagnostics;
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         new PerformanceTest()
    ///             .AddCase(() => ConsoleColor.Black.ToString(), "Enum.ToString")
    ///             .AddCase(() => Enum<ConsoleColor>.ToString(ConsoleColor.Black), "Enum<TEnum>.ToString")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    ///
    /// // This code example produces an output similar to the following one:
    /// // ==[Performance Test Results]================================================
    /// // Test Time: 2,000 ms
    /// // Warming up: Yes
    /// // Test cases: 2
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: 2
    /// // Cases are sorted by fulfilled iterations (most first)
    /// // --------------------------------------------------
    /// // 1. Enum<TEnum>.ToString: 26,104,501 iterations in 2,000.00 ms. Adjusted for 2,000 ms: 26,104,498.39
    /// // 2. Enum.ToString: 3,956,036 iterations in 2,000.01 ms. Adjusted for 2,000 ms: 3,956,026.31 (-22,148,472.08 / 15.15 %)]]></code>
    /// </para>
    /// <para>Each test case can be repeated multiple times. To see the costs of the first execution the default warming up session can be disabled:
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Diagnostics;
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         new PerformanceTest
    ///             {
    ///                 TestName = "System.Enum vs. KGySoft.CoreLibraries.Enum<TEnum> Results",
    ///                 TestTime = 2000,
    ///                 WarmUp = false,
    ///                 Repeat = 2
    ///             }
    ///             .AddCase(() => ConsoleColor.Black.ToString(), "Enum.ToString")
    ///             .AddCase(() => Enum<ConsoleColor>.ToString(ConsoleColor.Black), "Enum<TEnum>.ToString")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    ///
    /// // This code example produces an output similar to the following one:
    /// // ==[System.Enum vs. KGySoft.CoreLibraries.Enum<TEnum> Results Results]================================================
    /// // Test Time: 2,000 ms
    /// // Warming up: No
    /// // Test cases: 2
    /// // Repeats: 2
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: 2
    /// // Cases are sorted by fulfilled iterations (most first)
    /// // --------------------------------------------------
    /// // 1. Enum<TEnum>.ToString: 57,500,126 iterations in 4,000.00 ms. Adjusted for 2,000 ms: 28,750,060.12
    /// //   #1  28,730,396 iterations in 2,000.00 ms. Adjusted: 28,730,393.13      <---- Worst
    /// //   #2  28,769,730 iterations in 2,000.00 ms. Adjusted: 28,769,727.12      <---- Best
    /// //   Worst-Best difference: 39,334.00 (0.14 %)
    /// // 2. Enum.ToString: 7,618,943 iterations in 4,000.01 ms. Adjusted for 2,000 ms: 3,809,466.01 (-24,940,594.12 / 13.25 %)
    /// //   #1  3,786,163 iterations in 2,000.01 ms. Adjusted: 3,786,152.78        <---- Worst
    /// //   #2  3,832,780 iterations in 2,000.00 ms. Adjusted: 3,832,779.23        <---- Best
    /// //   Worst-Best difference: 46,626.46 (1.23 %)]]></code>
    /// </para>
    /// <para>By specifying <see cref="PerformanceTestBase.Iterations"/> you can specify to execute the test cases for a fix number of times instead of
    /// executing them for the specified time period:
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Diagnostics;
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         new PerformanceTest { Iterations = 10000 }
    ///             .AddCase(() => ConsoleColor.Black.ToString(), "Enum.ToString")
    ///             .AddCase(() => Enum<ConsoleColor>.ToString(ConsoleColor.Black), "Enum<TEnum>.ToString")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    ///
    /// // This code example produces an output similar to the following one:
    /// // ==[Performance Test Results]================================================
    /// // Iterations: 10,000
    /// // Warming up: Yes
    /// // Test cases: 2
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: 2
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Enum<TEnum>.ToString: average time: 0.23 ms
    /// // 2. Enum.ToString: average time: 4.50 ms (+4.27 ms / 1,994.32 %)]]></code>
    /// </para>
    /// <para>Similarly to time-based tests, you can increase number of repetitions:
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Diagnostics;
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         new PerformanceTest
    ///             {
    ///                 WarmUp = false,
    ///                 Iterations = 10000,
    ///                 Repeat = 5
    ///             }
    ///             .AddCase(() => ConsoleColor.Black.ToString(), "Enum.ToString")
    ///             .AddCase(() => Enum<ConsoleColor>.ToString(ConsoleColor.Black), "Enum<TEnum>.ToString")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    ///
    /// // This code example produces an output similar to the following one:
    /// // ==[Performance Test Results]================================================
    /// // Iterations: 10,000
    /// // Warming up: No
    /// // Test cases: 2
    /// // Repeats: 5
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: 2
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Enum<TEnum>.ToString: average time: 1.66 ms
    /// //   #1           7.39 ms   <---- Worst
    /// //   #2           0.22 ms   <---- Best
    /// //   #3           0.22 ms
    /// //   #4           0.23 ms
    /// //   #5           0.22 ms
    /// //   Worst-Best difference: 7.17 ms (3,266.23 %)
    /// // 2. Enum.ToString: average time: 5.00 ms (+3.35 ms / 302.10 %)
    /// //   #1           5.25 ms
    /// //   #2           4.40 ms   <---- Best
    /// //   #3           4.54 ms
    /// //   #4           5.97 ms   <---- Worst
    /// //   #5           4.85 ms
    /// //   Worst-Best difference: 1.58 ms (35.86 %)]]></code>
    /// </para>
    /// </example>
    /// <seealso cref="PerformanceTest{TResult}" />
    public class PerformanceTest : PerformanceTestBase<Action, object>
    {
        #region Methods

        /// <summary>
        /// Invokes the specified delegate.
        /// </summary>
        /// <param name="del">The delegate to invoke.</param>
        /// <returns>In <see cref="PerformanceTest"/> class this method returns always <see langword="null"/>.</returns>
        protected override object Invoke(Action del)
        {
            del.Invoke();
            return null;
        }

        #endregion
    }
}
