#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumPerformanceTest.cs
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

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.CoreLibraries
{
    [TestFixture]
    public class EnumPerformanceTest
    {
        #region Enumerations

        private enum NonFlagsEnum : long
        {
            Min = Int64.MinValue,
            Max = Int64.MaxValue,

            Negative = -1,

            None = 0,

            Alpha = 1,
            Beta = 2,
            Gamma = 4,
            Delta = 8,

            Redefined = 1,

            AlphaBeta = Alpha | Beta
        }

        [Flags]
        private enum FlagsEnum : long
        {
            Min = Int64.MinValue,
            Max = Int64.MaxValue,

            Negative = -1,

            None = 0,

            Alpha = 1,
            Beta = 2,
            Gamma = 4,
            Delta = 8,

            Redefined = 1,

            AlphaBeta = Alpha | Beta
        }

        #endregion

        #region Methods

        [Test]
        public void GetNamesTest() => new PerformanceTest { TestName = "GetName(s)", Iterations = 1000000 }
            .AddCase(() => Enum.GetNames(typeof(NonFlagsEnum)), "System.Enum.GetNames(Type)")
            .AddCase(() => Enum<NonFlagsEnum>.GetNames(), "KGySoft.CoreLibraries.Enum<TEnum>.GetNames()")
            .AddCase(() => Enum.GetName(typeof(NonFlagsEnum), NonFlagsEnum.Alpha), "System.Enum.GetName(Type,object)")
            .AddCase(() => Enum<NonFlagsEnum>.GetName(NonFlagsEnum.Alpha), "KGySoft.CoreLibraries.Enum<TEnum>.GetName(TEnum)")
            .DoTest()
            .DumpResults(Console.Out);

        [Test]
        public void GetValuesTest() => new PerformanceTest { TestName = "GetValues", Iterations = 1000000 }
            .AddCase(() => Enum.GetValues(typeof(NonFlagsEnum)), "System.Enum.GetValues(Type)")
            .AddCase(() => Enum<NonFlagsEnum>.GetValues(), "KGySoft.CoreLibraries.Enum<TEnum>.GetValues()")
            .DoTest()
            .DumpResults(Console.Out);

        [Test]
        public void IsDefinedTest() => new PerformanceTest<bool> { TestName = "IsDefined", Iterations = 1000000 }
            .AddCase(() => Enum.IsDefined(typeof(NonFlagsEnum), NonFlagsEnum.Alpha), "System.Enum.IsDefined(Type,object) with enum value")
            .AddCase(() => Enum.IsDefined(typeof(NonFlagsEnum), nameof(NonFlagsEnum.Alpha)), "System.Enum.IsDefined(Type,object) with string value")
            .AddCase(() => Enum.IsDefined(typeof(NonFlagsEnum), (long)NonFlagsEnum.Alpha), "System.Enum.IsDefined(Type,object) with long value")
            .AddCase(() => Enum<NonFlagsEnum>.IsDefined(NonFlagsEnum.Alpha), "KGySoft.CoreLibraries.Enum<TEnum>.IsDefined(TEnum)")
            .AddCase(() => Enum<NonFlagsEnum>.IsDefined(nameof(NonFlagsEnum.Alpha)), "KGySoft.CoreLibraries.Enum<TEnum>.IsDefined(string)")
            .AddCase(() => Enum<NonFlagsEnum>.IsDefined((long)NonFlagsEnum.Alpha), "KGySoft.CoreLibraries.Enum<TEnum>.IsDefined(long)")
            .DoTest()
            .DumpResults(Console.Out);

        [Test]
        public void ToStringTest() => new PerformanceTest<string> { TestName = "ToString", Iterations = 1_000_000 }
            .AddCase(() => NonFlagsEnum.Alpha.ToString(), $"{nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.Alpha)}.ToString()")
            .AddCase(() => ((NonFlagsEnum)100).ToString(), $"(({nameof(NonFlagsEnum)})100).ToString()")
            .AddCase(() => NonFlagsEnum.AlphaBeta.ToString(), $"{nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.AlphaBeta)}.ToString()")
            /* ReSharper disable once BitwiseOperatorOnEnumWithoutFlags */.AddCase(() => (NonFlagsEnum.Beta | NonFlagsEnum.Gamma).ToString(), $"({nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.Beta)} | {nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.Gamma)}).ToString()")

            .AddCase(() => FlagsEnum.Alpha.ToString(), $"{nameof(FlagsEnum)}.{nameof(FlagsEnum.Alpha)}.ToString()")
            .AddCase(() => ((FlagsEnum)100).ToString(), $"(({nameof(FlagsEnum)})100).ToString()")
            .AddCase(() => FlagsEnum.AlphaBeta.ToString(), $"{nameof(FlagsEnum)}.{nameof(FlagsEnum.AlphaBeta)}.ToString()")
            .AddCase(() => (FlagsEnum.Beta | FlagsEnum.Gamma).ToString(), $"({nameof(FlagsEnum)}.{nameof(FlagsEnum.Beta)} | {nameof(FlagsEnum)}.{nameof(FlagsEnum.Gamma)}).ToString()")

            .AddCase(() => Enum<NonFlagsEnum>.ToString(NonFlagsEnum.Alpha), $"Enum<{nameof(NonFlagsEnum)}>.ToString({nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.Alpha)})")
            .AddCase(() => Enum<NonFlagsEnum>.ToString((NonFlagsEnum)100), $"Enum<{nameof(NonFlagsEnum)}>.ToString(({nameof(NonFlagsEnum)})100)")
            .AddCase(() => Enum<NonFlagsEnum>.ToString(NonFlagsEnum.AlphaBeta), $"Enum<{nameof(NonFlagsEnum)}>.ToString({nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.AlphaBeta)})")
            /* ReSharper disable once BitwiseOperatorOnEnumWithoutFlags */.AddCase(() => Enum<NonFlagsEnum>.ToString(NonFlagsEnum.Beta | NonFlagsEnum.Gamma), $"Enum<{nameof(NonFlagsEnum)}>.ToString({nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.Beta)} | {nameof(NonFlagsEnum)}.{nameof(NonFlagsEnum.Gamma)})")

            .AddCase(() => Enum<FlagsEnum>.ToString(FlagsEnum.Alpha), $"Enum<{nameof(FlagsEnum)}>.ToString({nameof(FlagsEnum)}.{nameof(FlagsEnum.Alpha)})")
            .AddCase(() => Enum<FlagsEnum>.ToString((FlagsEnum)100), $"Enum<{nameof(FlagsEnum)}>.ToString(({nameof(FlagsEnum)})100)")
            .AddCase(() => Enum<FlagsEnum>.ToString(FlagsEnum.AlphaBeta), $"Enum<{nameof(FlagsEnum)}>.ToString({nameof(FlagsEnum)}.{nameof(FlagsEnum.AlphaBeta)})")
            .AddCase(() => Enum<FlagsEnum>.ToString(FlagsEnum.Beta | FlagsEnum.Gamma), $"Enum<{nameof(FlagsEnum)}>.ToString({nameof(FlagsEnum)}.{nameof(FlagsEnum.Beta)} | {nameof(FlagsEnum)}.{nameof(FlagsEnum.Gamma)})")
            .AddCase(() => Enum<FlagsEnum>.ToString(FlagsEnum.Beta | FlagsEnum.Gamma, EnumFormattingOptions.DistinctFlags), $"Enum<{nameof(FlagsEnum)}>.ToString({nameof(FlagsEnum)}.{nameof(FlagsEnum.Beta)} | {nameof(FlagsEnum)}.{nameof(FlagsEnum.Gamma)}, {nameof(EnumFormattingOptions)}.{nameof(EnumFormattingOptions.DistinctFlags)})")

            .DoTest()
            .DumpResults(Console.Out);

        [Test]
        public void ParseTest() => new PerformanceTest { TestName = "Parse", Iterations = 1_000_000 }
            .AddCase(() => Enum.Parse(typeof(FlagsEnum), nameof(FlagsEnum.Alpha)), $"System.Enum.Parse(typeof({nameof(FlagsEnum)}), \"{nameof(FlagsEnum.Alpha)}\")")
            .AddCase(() => Enum<FlagsEnum>.Parse(nameof(FlagsEnum.Alpha)), $"KGySoft.CoreLibraries.Enum<{nameof(FlagsEnum)}>.Parse(\"{nameof(FlagsEnum.Alpha)}\")")

            .AddCase(() => Enum.Parse(typeof(FlagsEnum), "0"), $"System.Enum.Parse(typeof({nameof(FlagsEnum)}), \"0\") - existing value")
            .AddCase(() => Enum<FlagsEnum>.Parse("0"), $"KGySoft.CoreLibraries.Enum<{nameof(FlagsEnum)}>.Parse(\"0\") - existing value")

            .AddCase(() => Enum.Parse(typeof(FlagsEnum), "100"), $"System.Enum.Parse(typeof({nameof(FlagsEnum)}), \"100\") - non-existing value")
            .AddCase(() => Enum<FlagsEnum>.Parse("100"), $"KGySoft.CoreLibraries.Enum<{nameof(FlagsEnum)}>.Parse(\"100\") - non-existing value")

            .AddCase(() => Enum.Parse(typeof(FlagsEnum), nameof(FlagsEnum.Gamma) + ", " + nameof(FlagsEnum.AlphaBeta) + ", " + nameof(FlagsEnum.Delta)), $"System.Enum.Parse(typeof({nameof(FlagsEnum)}), \"{nameof(FlagsEnum.Gamma)}, {nameof(FlagsEnum.AlphaBeta)}, {nameof(FlagsEnum.Delta)}\")")
            .AddCase(() => Enum<FlagsEnum>.Parse(nameof(FlagsEnum.Gamma) + ", " + nameof(FlagsEnum.AlphaBeta) + ", " + nameof(FlagsEnum.Delta)), $"KGySoft.CoreLibraries.Enum<{nameof(FlagsEnum)}>.Parse(\"{nameof(FlagsEnum.Gamma)}, {nameof(FlagsEnum.AlphaBeta)}, {nameof(FlagsEnum.Delta)}\")")

            .AddCase(() => Enum<FlagsEnum>.Parse("Alpha, 16, Beta"), $"KGySoft.CoreLibraries.Enum<{nameof(FlagsEnum)}>.Parse(\"Alpha, 16, Beta\")")
            .AddCase(() => Enum<FlagsEnum>.Parse("Alpha | 16 | Beta", "|"), $"KGySoft.CoreLibraries.Enum<{nameof(FlagsEnum)}>.Parse(\"Alpha | 16 | Beta\", \"|\")")

            .DoTest()
            .DumpResults(Console.Out);

        #endregion
    }
}
