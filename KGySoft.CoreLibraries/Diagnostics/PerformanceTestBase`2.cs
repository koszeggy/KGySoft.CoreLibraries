#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PerformanceTestBase`2.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a base class for performance tests.
    /// <br/>See the <strong>Examples</strong> section for an example.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate type of the test cases.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <example>
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/KNiZa7" target="_blank">online</a>.</note>
    /// The following example demonstrates how to derive this class to create parameterized performance tests.
    /// <code lang="C#"><![CDATA[
    /// public class RandomizedPerformanceTest<T> : PerformanceTestBase<Func<Random, T>, T>
    /// {
    ///     private Random random;
    /// 
    ///     // a fix seed can be specified when initializing the test
    ///     public int Seed { get; set; }
    /// 
    ///     protected override T Invoke(Func<Random, T> del) => del.Invoke(random);
    /// 
    ///     // resetting the random instance with the specified seed before executing each case
    ///     protected override void OnBeforeCase() => random = new Random(Seed);
    /// }]]></code>
    /// And now the delegate of the <see cref="AddCase">AddCase</see> method will have a <see cref="Random"/> parameter can be used in the test cases:
    /// <code lang="C#"><![CDATA[
    /// new RandomizedPerformanceTest<string> { Seed = 0, Iterations = 1_000_000 }
    ///     .AddCase(rnd => rnd.NextEnum<ConsoleColor>().ToString(), "Enum.ToString")
    ///     .AddCase(rnd => Enum<ConsoleColor>.ToString(rnd.NextEnum<ConsoleColor>()), "Enum<TEnum>.ToString")
    ///     .DoTest()
    ///     .DumpResults(Console.Out);]]></code> 
    /// <note>See also the <strong>Examples</strong> section of the <see cref="PerformanceTest"/> and <see cref="PerformanceTest{TResult}"/> classes for some further examples.</note>
    /// </example>
    /// <seealso cref="PerformanceTest"/>
    /// <seealso cref="PerformanceTest{TResult}"/>
    public abstract class PerformanceTestBase<TDelegate, TResult> : PerformanceTestBase
        where TDelegate : Delegate
    {
        #region Nested classes

        #region TestCase class

        private sealed class TestCase
        {
            #region Fields

            internal TDelegate Case = default!;
            internal string Name = default!;

            #endregion
        }

        #endregion

        #region TestResult class

        private sealed class TestResult : ITestCaseResult
        {
            #region Fields

            internal readonly List<Repetition> Repetitions = new List<Repetition>();

            internal TestCase Case = default!;
            [AllowNull]internal TResult Result = default!;
            internal int IndexBest;
            internal int IndexWorst;
            internal Exception? Error;

            #endregion

            #region Properties

            #region Internal Properties

            internal TimeSpan TotalTime => Repetitions.Aggregate(TimeSpan.Zero, (acc, curr) => acc + curr.ExecutionTime);
            internal TimeSpan AverageTime => Error != null ? TimeSpan.MaxValue : new TimeSpan(TotalTime.Ticks / Repetitions.Count);
            internal double AverageIterations => Error != null ? Double.MaxValue : Repetitions.Average(r => r.AverageIterationsPerTestTime);

            #endregion

            #region Explicitly Implemented Interface Properties

            string ITestCaseResult.Name => Case.Name;
            object? ITestCaseResult.Result => Result;
            Exception? ITestCaseResult.Error => Error;
#if NET35 || NET40
            IList<ITestCaseRepetition> ITestCaseResult.Repetitions => Repetitions.Cast<ITestCaseRepetition>().ToArray();
#else
            IReadOnlyList<ITestCaseRepetition> ITestCaseResult.Repetitions => Repetitions;
#endif

            #endregion

            #endregion
        }

        #endregion

        #region PerformanceTestResultCollection class

        private sealed class PerformanceTestResultCollection : Collection<ITestCaseResult>, IPerformanceTestResultCollection
        {
            #region Fields

            private readonly PerformanceTestBase<TDelegate, TResult> test;

            #endregion

            #region Constructors

            internal PerformanceTestResultCollection(PerformanceTestBase<TDelegate, TResult> test, List<TestResult> testResults)
            {
                this.test = test;
#if NET35
                Items.AddRange(testResults.Cast<ITestCaseResult>());
#else
                Items.AddRange(testResults);
#endif
            }

            #endregion

            #region Methods

            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
                Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
            public void DumpResults(TextWriter writer, bool dumpConfig, bool dumpReturnValue, bool forceShowSize)
            {
                #region Local Methods

                static string DumpDiff(double currentValue, double baseValue, string? unit = null)
                {
                    double diff = currentValue - baseValue;
                    if (diff.Equals(0d))
                        return Res.PerformanceTestNoDifference;

                    string sign = diff > 0 ? LanguageSettings.FormattingLanguage.NumberFormat.PositiveSign : String.Empty;
                    unit = unit == null ? String.Empty : $" {unit}";
                    return Res.PerformanceTestDifference(sign, diff, unit, currentValue / baseValue);
                }

                void DumpConfig()
                {
                    if (!dumpConfig)
                        return;
                    writer.WriteLine(test.Iterations > 0 ? Res.PerformanceTestIterations(test.Iterations) : Res.PerformanceTestTestTime(test.TestTime));
                    writer.WriteLine(Res.PerformanceTestWarmingUp(test.WarmUp));
                    writer.WriteLine(Res.PerformanceTestTestCases(test.cases.Count));
                    if (test.Repeat > 1)
                        writer.WriteLine(Res.PerformanceTestRepeats(test.Repeat));
                    writer.WriteLine(Res.PerformanceTestCallingGcCollect(test.Collect));
                    writer.WriteLine(Res.PerformanceTestCpuAffinity(test.CpuAffinity));
                    if (test.cases.Count > 1)
                        writer.WriteLine(Res.PerformanceTestSortOfCases(test.SortBySize ? Res.PerformanceTestSortBySize : test.Iterations > 0 ? Res.PerformanceTestSortByTime : Res.PerformanceTestSortByIterations));
                    writer.WriteLine(Res.PerformanceTestSeparator);
                }

                #endregion

                if (writer == null!)
                    Throw.ArgumentNullException(Argument.writer);

                writer.WriteLine(Res.PerformanceTestHeader(test.TestName ?? Res.PerformanceTestDefaultName));
                DumpConfig();
                if (Items.Count == 0)
                    return;
                var baseLine = (TestResult)Items[0];
                int baseLength = test.GetLength(baseLine.Result);
                for (int i = 0; i < Items.Count; i++)
                {
                    // Headline
                    var result = (TestResult)Items[i];
                    if (test.cases.Count > 1)
                        writer.Write(Res.PerformanceTestCaseOrder(i + 1));
                    writer.Write(Res.PerformanceTestCaseName(result.Case.Name));

                    if (result.Error != null)
                        writer.Write(Res.PerformanceTestCaseError(result.Error.GetType(), result.Error.Message));
                    else if (test.Iterations > 0)
                    {
                        writer.Write(Res.PerformanceTestCaseAverageTime(result.AverageTime.TotalMilliseconds));
                        if (i > 0)
                            writer.Write(DumpDiff(result.AverageTime.TotalMilliseconds, baseLine.AverageTime.TotalMilliseconds, Res.Millisecond));
                    }
                    else
                    {
                        int totalIterations = result.Repetitions.Sum(r => r.Iterations);
                        writer.Write(Res.PerformanceTestCaseIterations(totalIterations, result.TotalTime.TotalMilliseconds, test.TestTime, result.AverageIterations));
                        if (i > 0)
                            writer.Write(DumpDiff(result.AverageIterations, baseLine.AverageIterations));
                    }

                    writer.WriteLine();

                    // Repeats
                    if (result.Error == null && result.Repetitions.Count > 1)
                    {
                        for (int r = 0; r < result.Repetitions.Count; r++)
                        {
                            Repetition repetition = result.Repetitions[r];
                            writer.Write(Res.PerformanceTestCaseRepetitionOrder(r + 1, test.Iterations > 0
                                ? Res.PerformanceTestCaseRepetitionTime(repetition.ExecutionTime.TotalMilliseconds)
                                : Res.PerformanceTestCaseRepetitionIterations(repetition.Iterations, repetition.ExecutionTime.TotalMilliseconds, repetition.AverageIterationsPerTestTime)));
                            if (result.IndexBest != result.IndexWorst && r.In(result.IndexBest, result.IndexWorst))
                                writer.Write(r == result.IndexBest ? Res.PerformanceTestBestMark : Res.PerformanceTestWorstMark);
                            writer.WriteLine();
                        }

                        writer.Write(Res.PerformanceTestWorstBestDiff);
                        writer.WriteLine(test.Iterations > 0
                            ? Res.PerformanceTestWorstBestDiffTime((result.Repetitions[result.IndexWorst].ExecutionTime - result.Repetitions[result.IndexBest].ExecutionTime).TotalMilliseconds, result.Repetitions[result.IndexWorst].ExecutionTime.TotalMilliseconds / result.Repetitions[result.IndexBest].ExecutionTime.TotalMilliseconds - 1)
                            : Res.PerformanceTestWorstBestDiffIteration(result.Repetitions[result.IndexBest].AverageIterationsPerTestTime - result.Repetitions[result.IndexWorst].AverageIterationsPerTestTime, result.Repetitions[result.IndexBest].AverageIterationsPerTestTime / result.Repetitions[result.IndexWorst].AverageIterationsPerTestTime - 1));
                    }

                    // Error
                    if (result.Error != null)
                    {
                        if (!dumpReturnValue)
                            continue;

                        writer.WriteLine();
                        writer.WriteLine(Res.PerformanceTestDumpedError);
                        writer.WriteLine(result.Error.ToString());
                        writer.WriteLine();

                        continue;
                    }

                    // Result
                    // ReSharper disable once PossibleNullReferenceException - never null, ensured by static ctor
                    if (typeof(TDelegate).GetMethod(nameof(Action.Invoke))!.ReturnType != Reflector.VoidType
                        && (forceShowSize || dumpReturnValue || test.SortBySize))
                    {
                        int caseLength = test.GetLength(result.Result);
                        string units = Res.PerformanceTestUnitPossiblePlural(test.GetUnit());
                        writer.Write(Res.PerformanceTestResultSize(caseLength, units));
                        if (i > 0)
                            writer.Write(DumpDiff(caseLength, baseLength, units));
                        writer.WriteLine();

                        if (!dumpReturnValue)
                            continue;

                        writer.WriteLine();
                        writer.WriteLine(Res.PerformanceTestDumpedResult);
                        writer.WriteLine(test.AsString(result.Result));
                        writer.WriteLine();
                    }
                }

                writer.WriteLine();
            }

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly List<TestCase> cases = new List<TestCase>();

        private IntPtr origAffinity;
        private ProcessPriorityClass origPriority;
        private ThreadPriority origThreadPrio;

        #endregion

        #region Events

        /// <summary>
        /// Occurs before running the test cases.
        /// </summary>
        public event EventHandler? Initialize;

        /// <summary>
        /// Occurs after running the tests, even after a failure.
        /// </summary>
        public event EventHandler? TearDown;

        /// <summary>
        /// Occurs before each repetition of a test case, including the warming-up session.
        /// </summary>
        public event EventHandler? BeforeCase;

        /// <summary>
        /// Occurs after each repetition of a test case, including the warming-up session.
        /// </summary>
        public event EventHandler? AfterCase;

        #endregion

        #region Methods

        #region Static Methods

        private static void DoCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Adds a test case to the test suit.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="PerformanceTest"/> class for some examples.
        /// </summary>
        /// <param name="testCase">The test case.</param>
        /// <param name="name">The name of the test. If not specified, a default name will be added.</param>
        /// <returns>The self <see cref="PerformanceTestBase{TDelegate,TResult}"/> instance to provide fluent initialization syntax.</returns>
        public PerformanceTestBase<TDelegate, TResult> AddCase(TDelegate testCase, string? name = null)
        {
            if (testCase == null!)
                Throw.ArgumentNullException(Argument.testCase);
            cases.Add(new TestCase { Case = testCase, Name = name ?? Res.PerformanceTestCaseDefaultName(cases.Count + 1) });
            return this;
        }

        /// <summary>
        /// Performs the test and returns the test results.
        /// </summary>
        /// <returns>An <see cref="IPerformanceTestResultCollection"/> instance containing the test results.</returns>
        public override IPerformanceTestResultCollection DoTest()
        {
            if (cases.Count == 0)
                Throw.InvalidOperationException(Res.PerformanceTestNoTestCases);

            DoInitialize();
            try
            {
                List<TestResult> testResults = DoTestCases();
                SortResults(testResults);
                return new PerformanceTestResultCollection(this, testResults);
            }
            finally
            {
                DoTearDown();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the length of the result in any unit specified by the <see cref="GetUnit">GetUnit</see> method.
        /// <br/>The base implementation returns element count if <typeparamref name="TResult"/> is <see cref="IEnumerable"/>; otherwise, the size of <typeparamref name="TResult"/> in bytes (which is 4 or 8 bytes for reference types, depending on the platform target).
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The length of the result.</returns>
        protected virtual int GetLength(TResult result)
        {
            switch (result)
            {
                case ICollection collection:
                    return collection.Count;
                case string s:
                    return s.Length;
                case IEnumerable e:
                    return e.Cast<object>().Count();
                default:
                    return Reflector<TResult>.SizeOf;
            }
        }

        /// <summary>
        /// Gets the length unit name of <typeparamref name="TResult"/>.
        /// <br/>The base implementation returns the element name if <typeparamref name="TResult"/> is <see cref="IEnumerable{T}"/> (C# alias name, if applicable);
        /// a localized string for <c>item</c>, if <typeparamref name="TResult"/> is a non-generic <see cref="IEnumerable"/>; otherwise, a localized string for <c>byte</c>.
        /// </summary>
        /// <returns>The unit name of <typeparamref name="TResult"/>.</returns>
        protected virtual string GetUnit()
        {
            if (typeof(TResult).IsImplementationOfGenericType(Reflector.IEnumerableGenType, out Type? genericType))
            {
                Type genericParam = genericType.GetGenericArguments()[0];
                return KnownTypes.GetValueOrDefault(genericParam) ?? genericParam.Name;
            }

            if (typeof(IEnumerable).IsAssignableFrom(typeof(TResult)))
                return Res.PerformanceTestItem;
            return Res.PerformanceTestByte;
        }

        /// <summary>
        /// Gets the string representation of the specified <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>The string representation of the specified <paramref name="result"/>.</returns>
        /// <remarks>
        /// <para>If <typeparamref name="TResult"/> is a <see cref="Array">byte[]</see>, then it is returned as a raw string with <see cref="Encoding.Default">Encoding.Default</see>
        /// encoding (similarly to a HEX editor), while non-whitespace control characters are replaced by square characters (<c>□</c>).</para>
        /// <para>Zero characters are replaced also if <typeparamref name="TResult"/> is <see cref="string"/>.</para>
        /// <para>If <typeparamref name="TResult"/> is <see cref="IEnumerable"/>, then the string representation of elements (simply by <see cref="Object.ToString">ToString</see>) are concatenated.</para>
        /// <para>In any other case returns the result of <see cref="Object.ToString">ToString</see> for <paramref name="result"/>, or a localized string for <c>null</c>, if <paramref name="result"/> is <see langword="null"/>.</para>
        /// </remarks>
        protected virtual string AsString(TResult result)
        {
            string s;
            switch (result)
            {
                case byte[] bytes:
                    s = Encoding.Default.GetString(bytes);
                    break;
                case string str:
                    s = str;
                    break;
                case IEnumerable e:
                    s = e.Cast<object>().Join(", ");
                    break;
                default:
                    s = result?.ToString() ?? Res.Null;
                    break;
            }

            var chars = new char[s.Length];
            var whitespaceControls = new[] { '\t', '\r', '\n' };
            for (int i = 0; i < s.Length; i++)
                chars[i] = s[i] < 32 && !s[i].In(whitespaceControls) ? '□' : s[i];
            return new String(chars);
        }

        /// <summary>
        /// Invokes the specified delegate.
        /// </summary>
        /// <param name="del">The delegate to invoke.</param>
        /// <returns>A <typeparamref name="TResult"/> instance returned by the specified delegate. Returns <see langword="null"/>&#160;for <see langword="void"/>&#160;delegate types.</returns>
        protected abstract TResult Invoke(TDelegate del);

        /// <summary>
        /// Raises the <see cref="Initialize"/> event. This method is called before running the test cases.
        /// </summary>
        // Note: unlike regular event invoker methods, this one has no EventArgs parameter to maintain compatibility with <5.6.0 versions that had no events.
        protected virtual void OnInitialize() => Initialize?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Raises the <see cref="TearDown"/> event. Called after running the tests, even after a failure.
        /// </summary>
        // Note: unlike regular event invoker methods, this one has no EventArgs parameter to maintain compatibility with <5.6.0 versions that had no events.
        protected virtual void OnTearDown() => TearDown?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Raises the <see cref="BeforeCase"/> event. Called before each repetition of a test case, including the warming-up session.
        /// </summary>
        // Note: unlike regular event invoker methods, this one has no EventArgs parameter to maintain compatibility with <5.6.0 versions that had no events.
        protected virtual void OnBeforeCase() => BeforeCase?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Raises the <see cref="AfterCase"/> event. Called after each repetition of a test case, including the warming-up session.
        /// </summary>
        // Note: unlike regular event invoker methods, this one has no EventArgs parameter to maintain compatibility with <5.6.0 versions that had no events.
        protected virtual void OnAfterCase() => AfterCase?.Invoke(this, EventArgs.Empty);

        #endregion

        #region Private Methods

        [SecuritySafeCritical]
        private void DoInitialize()
        {
            OnInitialize();
            if (!IsValidAffinity())
                return;
            SetCpuAffinity();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        [SuppressMessage("Microsoft.Interoperability", "CA1416:ValidatePlatformCompatibility", Justification = "Called only if CpuAffinity is set.")]
        private void SetCpuAffinity()
        {
            Process process = Process.GetCurrentProcess();
            origAffinity = process.ProcessorAffinity;
            process.ProcessorAffinity = new IntPtr(CpuAffinity.GetValueOrDefault());
            origPriority = process.PriorityClass;
            process.PriorityClass = ProcessPriorityClass.High;
            origThreadPrio = Thread.CurrentThread.Priority;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
        }

        private bool IsValidAffinity() => CpuAffinity.HasValue && CpuAffinity.Value > 0 && CpuAffinity.Value < 2L << (Environment.ProcessorCount - 1);

        [SecuritySafeCritical]
        private void DoTearDown()
        {
            if (IsValidAffinity())
                ResetCpuAffinity();
            OnTearDown();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        [SuppressMessage("Microsoft.Interoperability", "CA1416:ValidatePlatformCompatibility", Justification = "Called only if CpuAffinity is set.")]
        private void ResetCpuAffinity()
        {
            Process process = Process.GetCurrentProcess();
            process.ProcessorAffinity = origAffinity;
            process.PriorityClass = origPriority;
            Thread.CurrentThread.Priority = origThreadPrio;
        }

        private void DoWarmUp(TDelegate testCase, TestResult testResult)
        {
            if (!WarmUp)
                return;

            try
            {
                long timeout = TimeHelper.ToStopwatchTicks(TestTime);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                do
                    Invoke(testCase);
                while (stopwatch.ElapsedTicks < timeout);
            }
            catch (Exception e) when (!e.IsCritical())
            {
                testResult.Error = e;
            }
        }

        private List<TestResult> DoTestCases()
        {
            var results = new List<TestResult>();
            foreach (TestCase testCase in cases)
            {
                var testResult = new TestResult { Case = testCase };
                results.Add(testResult);
                OnBeforeCase();
                DoWarmUp(testCase.Case, testResult);
                OnAfterCase();

                for (int r = 0; r < Repeat; r++)
                {
                    if (testResult.Error != null)
                        break;
                    OnBeforeCase();
                    if (Collect)
                        DoCollect();
                    if (Iterations > 0)
                        DoTestByIterations(testCase.Case, testResult);
                    else
                        DoTestByTime(testCase.Case, testResult);
                    OnAfterCase();
                }

                if (testResult.Error != null)
                    continue;

                for (int r = 0; r < Repeat; r++)
                {
                    if (Iterations > 0)
                    {
                        if (r == 0 || testResult.Repetitions[r].ExecutionTime < testResult.Repetitions[testResult.IndexBest].ExecutionTime)
                            testResult.IndexBest = r;
                        if (r == 0 || testResult.Repetitions[r].ExecutionTime > testResult.Repetitions[testResult.IndexWorst].ExecutionTime)
                            testResult.IndexWorst = r;
                    }
                    else
                    {
                        if (r == 0 || testResult.Repetitions[r].AverageIterationsPerTestTime > testResult.Repetitions[testResult.IndexBest].AverageIterationsPerTestTime)
                            testResult.IndexBest = r;
                        if (r == 0 || testResult.Repetitions[r].AverageIterationsPerTestTime < testResult.Repetitions[testResult.IndexWorst].AverageIterationsPerTestTime)
                            testResult.IndexWorst = r;
                    }
                }
            }

            return results;
        }

        private void DoTestByIterations(TDelegate testCase, TestResult testResult)
        {
            var stopwatch = new Stopwatch();
            TResult? result = default;
            int iterations = Iterations;
            stopwatch.Start();
            try
            {
                for (int i = 0; i < iterations; i++)
                    result = Invoke(testCase);
            }
            catch (Exception e) when (!e.IsCritical())
            {
                testResult.Error = e;
                return;
            }

            stopwatch.Stop();
            testResult.Repetitions.Add(new Repetition(stopwatch.Elapsed, iterations));
            testResult.Result = result;
        }

        private void DoTestByTime(TDelegate testCase, TestResult testResult)
        {
            var stopwatch = new Stopwatch();
            TResult result;
            int iterations = 0;
            long timeout = TimeHelper.ToStopwatchTicks(TestTime);
            stopwatch.Start();
            try
            {
                do
                {
                    iterations += 1;
                    result = Invoke(testCase);
                } while (stopwatch.ElapsedTicks < timeout);
            }
            catch (Exception e) when (!e.IsCritical())
            {
                testResult.Error = e;
                return;
            }

            stopwatch.Stop();
            testResult.Repetitions.Add(new Repetition(stopwatch.Elapsed, iterations) { AverageIterationsPerTestTime = iterations * (TestTime / stopwatch.Elapsed.TotalMilliseconds) });
            testResult.Result = result;
        }

        private void SortResults(List<TestResult> testResults)
        {
            Comparison<TestResult> comparison = SortBySize ? (x, y) => Comparer<int>.Default.Compare(GetLength(x.Result), GetLength(y.Result))
                : Iterations > 0 ? (x, y) => Comparer<TimeSpan>.Default.Compare(x.AverageTime, y.AverageTime)
                : (x, y) => -Comparer<double>.Default.Compare(x.AverageIterations, y.AverageIterations);
            testResults.Sort(comparison);
        }

        #endregion

        #endregion

        #endregion
    }
}
