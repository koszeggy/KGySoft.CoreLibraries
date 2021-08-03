#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumComparerPerformanceTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class EnumComparerPerformanceTest
    {
        #region Enumerations

        private enum ByteEnum : byte
        {
            Min = Byte.MinValue,
            Max = Byte.MaxValue,
        }

        private enum SByteEnum : sbyte
        {
            Min = SByte.MinValue,
            Max = SByte.MaxValue,
        }

        private enum ShortEnum : short
        {
            Min = Int16.MinValue,
            Max = Int16.MaxValue,
        }

        private enum UShortEnum : ushort
        {
            Min = UInt16.MinValue,
            Max = UInt16.MaxValue,
        }

        private enum IntEnum : int
        {
            Min = Int32.MinValue,
            Max = Int32.MaxValue,
        }

        private enum UIntEnum : uint
        {
            Min = UInt32.MinValue,
            Max = UInt32.MaxValue,
        }


        private enum LongEnum : long
        {
            Min = Int64.MinValue,
            Max = Int64.MaxValue,
        }

        private enum ULongEnum : ulong
        {
            Min = UInt64.MinValue,
            Max = UInt64.MaxValue,
        }

        #endregion

        #region Methods

        [TestCase(ByteEnum.Min, ByteEnum.Max)]
        [TestCase(SByteEnum.Min, SByteEnum.Max)]
        [TestCase(ShortEnum.Min, ShortEnum.Max)]
        [TestCase(UShortEnum.Min, UShortEnum.Max)]
        [TestCase(IntEnum.Min, IntEnum.Max)]
        [TestCase(UIntEnum.Min, UIntEnum.Max)]
        [TestCase(LongEnum.Min, LongEnum.Max)]
        [TestCase(ULongEnum.Min, ULongEnum.Max)]
        public void EnumComparerTest<TEnum>(TEnum min, TEnum max) where TEnum : Enum =>
            new PerformanceTest { TestName = $"EnumComparer<{typeof(TEnum).Name}> Test", Iterations = 10_000_000 }
                //#pragma warning disable 219
                //            .AddCase(() => { bool eq = TestEnum.Min == TestEnum.Max; }, "Operator ==")
                //            .AddCase(() => { bool gt = TestEnum.Min > TestEnum.Max; }, "Operator >")
                //#pragma warning restore 219
                .AddCase(() => min.Equals(max), "Enum.Equals(object)")
                .AddCase(() => min.GetHashCode(), "Enum.GetHashCode()")
                .AddCase(() => min.CompareTo(max), "Enum.CompareTo(object)")
                .AddCase(() => EqualityComparer<TEnum>.Default.Equals(min, max), "EqualityComparer<T>.Default.Equals(T,T)")
                .AddCase(() => EqualityComparer<TEnum>.Default.GetHashCode(min), "EqualityComparer<T>.Default.GetHashCode(T)")
                .AddCase(() => Comparer<TEnum>.Default.Compare(min, max), "Comparer<T>.Default.Compare(T,T)")
                .AddCase(() => EnumComparer<TEnum>.Comparer.Equals(min, max), "EnumComparer<TEnum>.Comparer.Equals(TEnum,TEnum)")
                .AddCase(() => EnumComparer<TEnum>.Comparer.GetHashCode(min), "EnumComparer<TEnum>.Comparer.GetHashCode(TEnum)")
                .AddCase(() => EnumComparer<TEnum>.Comparer.Compare(min, max), "EnumComparer<TEnum>.Comparer.Compare(TEnum,TEnum)")
                .DoTest()
                .DumpResults(Console.Out);

        #endregion
    }
}
