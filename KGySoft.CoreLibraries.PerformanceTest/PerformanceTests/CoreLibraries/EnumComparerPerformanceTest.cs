#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumComparerPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) {{author}}, 2005-2019 - All Rights Reserved
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class EnumComparerPerformanceTest
    {
        #region Enumerations

        private enum TestEnum : long
        {
            Min = Int64.MinValue,
            Max = Int64.MaxValue,
        }

        #endregion

        #region Methods

        [Test]
        public void EnumComparerTest() => new PerformanceTest { TestName = "EnumComparer Test", Iterations = 10000000 }
            .AddCase(() => { bool eq = TestEnum.Min == TestEnum.Max; }, "Operator ==")
            .AddCase(() => { bool gt = TestEnum.Min > TestEnum.Max; }, "Operator >")
            .AddCase(() => TestEnum.Min.Equals(TestEnum.Max), "Enum.Equals(object)")
            .AddCase(() => TestEnum.Min.GetHashCode(), "Enum.GetHashCode()")
            .AddCase(() => TestEnum.Min.CompareTo(TestEnum.Max), "Enum.CompareTo(object)")
            .AddCase(() => EqualityComparer<TestEnum>.Default.Equals(TestEnum.Min, TestEnum.Max), "EqualityComparer<T>.Default.Equals(T,T)")
            .AddCase(() => EqualityComparer<TestEnum>.Default.GetHashCode(TestEnum.Min), "EqualityComparer<T>.Default.GetHashCode(T)")
            .AddCase(() => Comparer<TestEnum>.Default.Compare(TestEnum.Min, TestEnum.Max), "Comparer<T>.Default.Compare(T,T)")
            .AddCase(() => EnumComparer<TestEnum>.Comparer.Equals(TestEnum.Min, TestEnum.Max), "EnumComparer<TEnum>.Comparer.Equals(TEnum,TEnum)")
            .AddCase(() => EnumComparer<TestEnum>.Comparer.GetHashCode(TestEnum.Min), "EnumComparer<TEnum>.Comparer.GetHashCode(TEnum)")
            .AddCase(() => EnumComparer<TestEnum>.Comparer.Compare(TestEnum.Min, TestEnum.Max), "EnumComparer<TEnum>.Comparer.Compare(TEnum,TEnum)")
            .DoTest()
            .DumpResults(Console.Out);

        #endregion
    }
}
