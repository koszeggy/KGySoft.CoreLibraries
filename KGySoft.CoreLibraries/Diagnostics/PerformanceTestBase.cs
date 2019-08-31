#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PerformanceTestBase.cs
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
using System.Collections.Generic;

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides a base class for performance tests.
    /// <br/>See the <strong>Examples</strong> section of the <see cref="PerformanceTest"/> class for some examples.
    /// </summary>
    public abstract class PerformanceTestBase
    {
        #region Repetition class

        private protected sealed class Repetition : ITestCaseRepetition
        {
            #region Properties

            #region Public Properties

            public TimeSpan ExecutionTime { get; }

            public int Iterations { get; }

            #endregion

            #region Internal Properties

            internal double AverageIterationsPerTestTime { get; set; }

            #endregion

            #endregion

            #region Constructors

            internal Repetition(TimeSpan executionTime, int iterations)
            {
                ExecutionTime = executionTime;
                Iterations = iterations;
            }

            #endregion
        }

        #endregion

        #region Fields

        private protected static readonly Dictionary<Type, string> KnownTypes = new Dictionary<Type, string>
        {
            [typeof(bool)] = "bool",
            [typeof(byte)] = "byte",
            [typeof(sbyte)] = "sbyte",
            [typeof(char)] = "char",
            [typeof(string)] = "string",
            [typeof(short)] = "short",
            [typeof(ushort)] = "ushort",
            [typeof(int)] = "int",
            [typeof(uint)] = "uint",
            [typeof(long)] = "long",
            [typeof(ulong)] = "ulong",
            [typeof(float)] = "float",
            [typeof(double)] = "double",
            [typeof(decimal)] = "decimal",
            [typeof(object)] = "object",
        };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the name of the test.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// Gets or sets number of iterations of test cases. If greater than zero, then <see cref="TestTime"/> is ignored.
        /// <br/>Default value: <c>0</c>.
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// Gets or sets the test duration, in milliseconds, for each test case and the warming-up sessions. If <see cref="Iterations"/> is greater than zero, then this property is ignored.
        /// <br/>Default value: <c>2000</c>.
        /// </summary>
        public int TestTime { get; set; } = 2000;

        /// <summary>
        /// Gets or sets whether there is an untested warm-up session before each test.
        /// Its duration or iteration count equals to <see cref="TestTime"/> or <see cref="Iterations"/>, respectively.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool WarmUp { get; set; } = true;

        /// <summary>
        /// Gets or sets whether <see cref="GC.Collect()">GC.Collect</see> should be called before running the test cases.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool Collect { get; set; } = true;

        /// <summary>
        /// Gets or sets how many times the test cases should be repeated.
        /// <br/>Default value: <c>1</c>.
        /// </summary>
        public int Repeat { get; set; } = 1;

        /// <summary>
        /// Gets or sets the CPU affinity to be used for executing tests. If <see langword="null"/>, or is too large for the executing system, then the affinity is not adjusted for the test.
        /// <br/>Default value: <c>2</c>.
        /// </summary>
        public int? CpuAffinity { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether the results should be sorted by the size of the produced result instead of iterations count or time results.
        /// Makes sense only if the test delegate has a return type and the returned value of a test case is always the same for each run.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool SortBySize { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// In a derived class performs the test and returns the test results.
        /// </summary>
        /// <returns>An <see cref="IPerformanceTestResultCollection"/> instance containing the test results.</returns>
        public abstract IPerformanceTestResultCollection DoTest();

        #endregion
    }
}
