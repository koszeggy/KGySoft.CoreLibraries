#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PerformanceTest`1.cs
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

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a class for performance tests of <see cref="Func{TResult}"/> delegate test cases (tests with a return value).
    /// <br/>See the <strong>Examples</strong> section for an example.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <example>
    /// <para>The following example demonstrates how to use the <see cref="PerformanceTest{TResult}"/> class:
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections.Generic;
    /// using System.IO;
    /// using System.Runtime.Serialization.Formatters.Binary;
    /// using KGySoft.Diagnostics;
    /// using KGySoft.Serialization.Binary;
    ///
    /// class Example
    /// {
    ///     private static object testObj = new List<int> { 1, 2, 3 };
    ///
    ///     static void Main(string[] args)
    ///     {
    ///         new PerformanceTest<byte[]> { Iterations = 10_000, Repeat = 5 }
    ///             .AddCase(() =>
    ///             {
    ///                 using (var ms = new MemoryStream())
    ///                 {
    ///                     new BinaryFormatter().Serialize(ms, testObj);
    ///                     return ms.ToArray();
    ///                 }
    ///             }, "BinaryFormatter")
    ///             .AddCase(() => BinarySerializer.Serialize(testObj), "BinarySerializer")
    ///             .DoTest()
    ///             .DumpResults(Console.Out, forceShowReturnSizes: true);
    ///     }
    /// }
    ///
    /// // This code example produces an output similar to the following one:
    /// // ==[Performance Test Results]================================================
    /// // Iterations: 10,000
    /// // Warming up: Yes
    /// // Test cases: 2
    /// // Repeats: 5
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. BinarySerializer: average time: 27.16 ms
    /// //   #1          27.27 ms
    /// //   #2          26.53 ms   <---- Best
    /// //   #3          28.26 ms   <---- Worst
    /// //   #4          26.94 ms
    /// //   #5          26.78 ms
    /// //   Worst-Best difference: 1.73 ms (6.53 %)
    /// //   Result size: 16 byte(s)
    /// // 2. BinaryFormatter: average time: 42.93 ms (+15.77 ms / 158.07 %)
    /// //   #1          44.94 ms   <---- Worst
    /// //   #2          43.52 ms
    /// //   #3          42.79 ms
    /// //   #4          42.21 ms
    /// //   #5          41.18 ms   <---- Best
    /// //   Worst-Best difference: 3.76 ms (9.14 %)
    /// //   Result size: 221 byte(s) (+205.00 byte(s) / 1,381.25 %)]]></code>
    /// </para>
    /// <note type="tip">If the test focuses on size performance set <see cref="PerformanceTestBase.Iterations"/> to <c>1</c> and <see cref="PerformanceTestBase.SortBySize"/> to <see langword="true"/>.</note>
    /// <note>See some further examples in the <see cref="PerformanceTest"/> class.</note>
    /// <note type="tip">See also the <strong>Examples</strong> section of the <see cref="PerformanceTestBase{TDelegate,TResult}"/> class to see how to create a custom type for parameterized performance tests.</note>
    /// </example>
    /// <seealso cref="PerformanceTest" />
    public class PerformanceTest<TResult> : PerformanceTestBase<Func<TResult>, TResult>
    {
        #region Methods

        /// <summary>
        /// Invokes the specified delegate.
        /// </summary>
        /// <param name="del">The delegate to invoke.</param>
        /// <returns>A <typeparamref name="TResult" /> instance returned by the specified delegate.</returns>
        protected override TResult Invoke(Func<TResult> del) => del.Invoke();

        #endregion
    }
}
