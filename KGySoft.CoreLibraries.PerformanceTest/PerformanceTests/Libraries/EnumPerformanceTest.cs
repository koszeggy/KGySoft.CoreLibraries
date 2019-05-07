using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.PerformanceTests.Libraries
{
    /// <summary>
    /// Summary description for EnumTest
    /// </summary>
    [TestFixture]
    public class EnumPerformanceTest
    {
        private enum TestEnum: long
        {
            None,
            Alpha = 1,
            Beta = 2,
            Gamma = 4,
            Delta = 8,

            Redefined = 1,

            Alphabet = Alpha | Beta,

            Min = Int64.MinValue,
            Max = Int64.MaxValue,
        }

        [Flags]
        private enum TestFlagsEnum: long
        {
            None,
            Alpha = 1,
            Beta = 2,
            Gamma = 4,
            Delta = 8,
            Redefined = 1,

            Alphabet = Alpha | Beta,

            Min = Int64.MinValue,
            Max = Int64.MaxValue,
        }

        [SetUp]
        public void ResetCaches()
        {
            Enum<TestEnum>.ClearCaches();
        }

        [Test]
        public void GetNamesTest()
        {
            const int iterations = 1000000;
            Type enumType = typeof(TestEnum);

            Console.WriteLine("==================GetName (iterations: {0:N0})==================", iterations);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.GetNames(enumType);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.GetNames(typeof({0})): {1} ms", enumType.Name, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.GetNames();
            }
            watch.Stop();
            Console.WriteLine("KGySoft.CoreLibraries.Enum<{0}>.GetNames(): {1} ms", enumType.Name, watch.ElapsedMilliseconds);

            const TestEnum e = TestEnum.Gamma;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.GetName(enumType, e);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.GetName(typeof({0}), {0}.{1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.GetName(e);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.CoreLibraries.Enum<{0}>.GetName({0}.{1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);
        }

        [Test]
        public void GetValuesTest()
        {
            const int iterations = 1000000;
            Type enumType = typeof(TestEnum);

            Console.WriteLine("==================GetValues (iterations: {0:N0})==================", iterations);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.GetValues(enumType);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.GetValues(typeof({0})): {1} ms", enumType.Name, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.GetValues();
            }
            watch.Stop();
            Console.WriteLine("KGySoft.CoreLibraries.Enum<{0}>.GetValues(): {1} ms: ", enumType.Name, watch.ElapsedMilliseconds);
        }

        [Test]
        public void IsDefinedTest()
        {
            const int iterations = 1000000;
            Type enumType = typeof(TestEnum);
            TestEnum e = TestEnum.Gamma;
            string s = "Gamma";
            long n = (long)e;

            Console.WriteLine("==================IsDefined (iterations: {0:N0})==================", iterations);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.IsDefined(enumType, e);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.IsDefined(typeof({0}), {0}.{1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.IsDefined(enumType, s);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.IsDefined(typeof({0}), \"{1}\"): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.IsDefined(enumType, n);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.IsDefined(typeof({0}), {1}): {2} ms", enumType.Name, n, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.IsDefined(e);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.CoreLibraries.Enum<{0}>.IsDefined({0}.{1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.IsDefined(s);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.CoreLibraries.Enum<{0}>.IsDefined(\"{1}\"): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.IsDefined(n);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.CoreLibraries.Enum<{0}>.IsDefined({1}): {2} ms", enumType.Name, n, watch.ElapsedMilliseconds);
        }

        [Test]
        public void ToStringTest()
        {
            const int iterations = 1000000;
            Type enumType = typeof(TestEnum);
            TestEnum e = TestEnum.Gamma;

            Console.WriteLine("==================ToString (iterations: {0:N0})==================", iterations);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                e.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString() (existing field: {1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);

            e = (TestEnum)((long)TestEnum.Gamma + 100);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                e.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString() (non-existing field: {1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);

            e = TestEnum.Alphabet;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                e.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString() (flags with self value: {1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);

            e = (TestEnum)((long)TestEnum.Beta | (long)TestEnum.Gamma); // to suppress resharper error
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                e.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString() (flags without FlagsAttribute: {1}): {2} ms", enumType.Name, e, watch.ElapsedMilliseconds);

            enumType = typeof(TestFlagsEnum);
            TestFlagsEnum f = TestFlagsEnum.Gamma;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                f.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString() (existing field: {1}): {2} ms", enumType.Name, f, watch.ElapsedMilliseconds);

            f = (TestFlagsEnum)((long)TestFlagsEnum.Gamma + 100);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                f.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString() (non-existing field: {1}): {2} ms", enumType.Name, f, watch.ElapsedMilliseconds);

            f = TestFlagsEnum.Alphabet;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                f.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString() (flags with self value: {1}): {2} ms", enumType.Name, f, watch.ElapsedMilliseconds);

            f = TestFlagsEnum.Beta | TestFlagsEnum.Gamma;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                f.ToString();
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString(): (flags with FlagsAttribute: {1}): {2} ms", enumType.Name, f, watch.ElapsedMilliseconds);

            ////////////

            enumType = typeof(TestEnum);
            e = TestEnum.Gamma;

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.ToString(e);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (existing field: {1}): {2} ms", enumType.Name, Enum<TestEnum>.ToString(e), watch.ElapsedMilliseconds);

            e = (TestEnum)((long)TestEnum.Gamma + 100);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.ToString(e);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (non-existing field: {1}): {2} ms", enumType.Name, Enum<TestEnum>.ToString(e), watch.ElapsedMilliseconds);

            e = TestEnum.Alphabet;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.ToString(e);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (flags with self value: {1}): {2} ms", enumType.Name, Enum<TestEnum>.ToString(e), watch.ElapsedMilliseconds);

            e = (TestEnum)((long)TestEnum.Beta | (long)TestEnum.Gamma); // to suppress resharper error
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.ToString(e);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (flags without FlagsAttribute: {1}): {2} ms", enumType.Name, Enum<TestEnum>.ToString(e), watch.ElapsedMilliseconds);

            enumType = typeof(TestFlagsEnum);
            f = TestFlagsEnum.Gamma;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestFlagsEnum>.ToString(f);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (existing field: {1}): {2} ms", enumType.Name, Enum<TestFlagsEnum>.ToString(f), watch.ElapsedMilliseconds);

            f = (TestFlagsEnum)((long)TestFlagsEnum.Gamma + 100);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestFlagsEnum>.ToString(f);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (non-existing field: {1}): {2} ms", enumType.Name, Enum<TestFlagsEnum>.ToString(f), watch.ElapsedMilliseconds);

            f = TestFlagsEnum.Alphabet;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestFlagsEnum>.ToString(f);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (flags with self value: {1}): {2} ms", enumType.Name, Enum<TestFlagsEnum>.ToString(f), watch.ElapsedMilliseconds);

            f = TestFlagsEnum.Beta | TestFlagsEnum.Gamma;
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestFlagsEnum>.ToString(f);
            }
            watch.Stop();
            Console.WriteLine("Enum<{0}>.ToString(value) (flags with FlagsAttribute: {1}): {2} ms", enumType.Name, Enum<TestFlagsEnum>.ToString(f), watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                f.ToString(EnumFormattingOptions.Auto, " | ");
            }
            watch.Stop();
            Console.WriteLine("{0}.ToString(extension) (flags with FlagsAttribute: {1}): {2} ms", enumType.Name, Enum<TestFlagsEnum>.ToString(f), watch.ElapsedMilliseconds);
        }

        [Test]
        public void ParseTest()
        {
            const int iterations = 1000000;
            Type enumType = typeof(TestEnum);

            string s = "Gamma";
            Console.WriteLine("==================Parse (iterations: {0:N0})==================", iterations);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.Parse(enumType, s);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.Parse(typeof({0}), \"{1}\") (existing field): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.Parse(s);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.Enum<{0}>.Parse(\"{1}\") (existing field): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            s = "0";
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.Parse(enumType, s);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.Parse(typeof({0}), \"{1}\") (existing field from number): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.Parse(s);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.Enum<{0}>.Parse(\"{1}\") (existing field from number): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            s = "30";
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.Parse(enumType, s);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.Parse(typeof({0}), \"{1}\") (non-existing field from number): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.Parse(s);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.Enum<{0}>.Parse(\"{1}\") (non-existing field from number): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            s = "Gamma, Alphabet";
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.Parse(enumType, s);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.Parse(typeof({0}), \"{1}\") (compound flags): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.Parse(s);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.Enum<{0}>.Parse(\"{1}\") (coumpond flags): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            s = "Gamma, Delta, Min";
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.Parse(enumType, s);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.Parse(typeof({0}), \"{1}\") (distinct flags): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.Parse(s);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.Enum<{0}>.Parse(\"{1}\") (distinct flags): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            s = "cica, kecske, mínusz";
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum.Parse(enumType, s, true);
            }
            watch.Stop();
            Console.WriteLine("System.Enum.Parse(typeof({0}), \"{1}\") (flags, case-insensitive): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.Parse(s, true);
            }
            watch.Stop();
            Console.WriteLine("KGySoft.Enum<{0}>.Parse(\"{1}\") (flags, case-insensitive): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);

            s = "Gamma | Delta | Beta | 16";

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Enum<TestEnum>.Parse(s, "|");
            }
            watch.Stop();
            Console.WriteLine("KGySoft.Enum<{0}>.Parse(\"{1}\") (flags-numbers): {2} ms", enumType.Name, s, watch.ElapsedMilliseconds);
        }

        [Test]
        public void EnumComparerTest()
        {
            PerformanceTest.CheckTestingFramework();

            const int iterations = 10000000;
            Type enumType = typeof(TestEnum);

            var c1 = EnumComparer<TestEnum>.Comparer;
            var d1 = Comparer<TestEnum>.Default;
            var e1 = EqualityComparer<TestEnum>.Default;
            var v1 = new TestEnum[] { TestEnum.Min, TestEnum.Max };

            Console.WriteLine("==================EnumComparer (iterations: {0:N0})==================", iterations);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                bool eq = v1[0] == v1[1];
            }
            watch.Stop();
            Console.WriteLine("{0}.{1} == {0}.{2}: {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                v1[0].Equals(v1[1]);
            }
            watch.Stop();
            Console.WriteLine("{0}.{1}.Equals({0}.{2}): {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                e1.Equals(v1[0], v1[1]);
            }
            watch.Stop();
            Console.WriteLine("EqualityComparer<{0}>.Default.Equals({0}.{1}, {0}.{2}): {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                c1.Equals(v1[0], v1[1]);
            }
            watch.Stop();
            Console.WriteLine("EnumComparer<{0}>.Comparer.Equals({0}.{1}, {0}.{2}): {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                v1[0].GetHashCode();
            }
            watch.Stop();
            Console.WriteLine("{0}.{1}.GetHashCode(): {2} ms", enumType.Name, v1[0], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                e1.GetHashCode(v1[0]);
            }
            watch.Stop();
            Console.WriteLine("EqualityComparer<{0}>.Default.GetHashCode({0}.{1}): {2} ms", enumType.Name, v1[0], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                c1.GetHashCode(v1[0]);
            }
            watch.Stop();
            Console.WriteLine("EnumComparer<{0}>.Comparer.GetHashCode({0}.{1}): {2} ms", enumType.Name, v1[0], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                bool eq = v1[0] > v1[1];
            }
            watch.Stop();
            Console.WriteLine("{0}.{1} > {0}.{2}: {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                v1[0].CompareTo(v1[1]);
            }
            watch.Stop();
            Console.WriteLine("{0}.{1}.CompareTo({0}.{2}): {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                d1.Compare(v1[0], v1[1]);
            }
            watch.Stop();
            Console.WriteLine("Comparer<{0}>.Default.Compare({0}.{1}, {0}.{2}): {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                c1.Compare(v1[0], v1[1]);
            }
            watch.Stop();
            Console.WriteLine("EnumComparer<{0}>.Comparer.Compare({0}.{1}, {0}.{2}): {3} ms", enumType.Name, v1[0], v1[1], watch.ElapsedMilliseconds);
        }
    }
}
