#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumComparerTest.cs
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
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions;
using KGySoft.Reflection;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class EnumComparerTest : TestBase
    {
        #region Nested Types
        
        #region Enumerations

        private enum TestLongEnum : long
        {
            Min = Int64.MinValue,
            Max = Int64.MaxValue,
        }

        private enum TestUlongEnum : ulong
        {
            Max = UInt64.MaxValue
        }

        private enum EmptyEnum { }

        #endregion

        #region Sandbox class

#if NETFRAMEWORK
        private class Sandbox : MarshalByRefObject
        {
            internal void TestEnumComparer()
            {
#if !NET35
                Assert.IsFalse(AppDomain.CurrentDomain.IsFullyTrusted); 
#endif
                new EnumComparerTest().TestEnumComparer();

                // this accesses an ulong? but unlike in LockFreeCache it interestingly does not cause a problem
                Console.WriteLine(Enum<StringSplitOptions>.GetFlagsMask());
            }
        }
#endif

        #endregion

        #endregion

        #region Methods

        [Test]
        public void TestEnumComparer()
        {
            var c1 = EnumComparer<EmptyEnum>.Comparer;
            var d1 = Comparer<EmptyEnum>.Default;
            var e1 = EqualityComparer<EmptyEnum>.Default;
            var v1 = new EmptyEnum[] { (EmptyEnum)(-1), (EmptyEnum)1 };

            Assert.AreEqual(d1.Compare(v1[0], v1[1]), c1.Compare(v1[0], v1[1]));
            Assert.AreEqual(d1.Compare(v1[1], v1[0]), c1.Compare(v1[1], v1[0]));
            Assert.AreEqual(d1.Compare(v1[1], v1[1]), c1.Compare(v1[1], v1[1]));
            Assert.AreEqual(e1.Equals(v1[0], v1[1]), c1.Equals(v1[0], v1[1]));
            Assert.AreEqual(e1.Equals(v1[1], v1[1]), c1.Equals(v1[1], v1[1]));
            Assert.AreEqual(e1.GetHashCode(v1[0]), c1.GetHashCode(v1[0]));
            Assert.AreNotEqual(c1.GetHashCode(v1[0]), c1.GetHashCode(v1[1]));
            Assert.AreEqual(-1L, c1.ToInt64(v1[0]));
            Assert.AreEqual(1L, c1.ToInt64(v1[1]));
            Assert.AreEqual((ulong)UInt32.MaxValue, c1.ToUInt64(v1[0]));
            Assert.AreEqual(1UL, c1.ToUInt64(v1[1]));

            var c2 = EnumComparer<TestLongEnum>.Comparer;
            var d2 = Comparer<TestLongEnum>.Default;
            var e2 = EqualityComparer<TestLongEnum>.Default;
            var v2 = new TestLongEnum[] { TestLongEnum.Min, TestLongEnum.Max };

            Assert.AreEqual(d2.Compare(v2[0], v2[1]), c2.Compare(v2[0], v2[1]));
            Assert.AreEqual(d2.Compare(v2[1], v2[0]), c2.Compare(v2[1], v2[0]));
            Assert.AreEqual(d2.Compare(v2[1], v2[1]), c2.Compare(v2[1], v2[1]));
            Assert.AreEqual(e2.Equals(v2[0], v2[1]), c2.Equals(v2[0], v2[1]));
            Assert.AreEqual(e2.Equals(v2[1], v2[1]), c2.Equals(v2[1], v2[1]));

            var c3 = EnumComparer<TestUlongEnum>.Comparer;
            var d3 = Comparer<TestUlongEnum>.Default;
            var e3 = EqualityComparer<TestUlongEnum>.Default;
            var v3 = new TestUlongEnum[] { TestUlongEnum.Max, (TestUlongEnum)1 };

            Assert.AreEqual(d3.Compare(v3[0], v3[1]), c3.Compare(v3[0], v3[1]));
            Assert.AreEqual(d3.Compare(v3[1], v3[0]), c3.Compare(v3[1], v3[0]));
            Assert.AreEqual(d3.Compare(v3[1], v3[1]), c3.Compare(v3[1], v3[1]));
            Assert.AreEqual(e3.Equals(v3[0], v3[1]), c3.Equals(v3[0], v3[1]));
            Assert.AreEqual(e3.Equals(v3[1], v3[1]), c3.Equals(v3[1], v3[1]));
            Assert.AreNotEqual(c3.GetHashCode(v3[0]), c3.GetHashCode(v3[1]));
        }

#if !NETFRAMEWORK
        [Obsolete]
#endif
        [Test]
        public void SerializeTest()
        {
            var comparer = EnumComparer<TestLongEnum>.Comparer;
            var clone = comparer.DeepClone();
            Assert.AreSame(comparer, clone);
        }

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical]
        public void TestEnumComparer_PartiallyTrusted()
        {
            var domain = CreateSandboxDomain(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
            var handle = Activator.CreateInstance(domain, Assembly.GetExecutingAssembly().FullName, typeof(Sandbox).FullName);
            var sandbox = (Sandbox)handle.Unwrap();
            try
            {
                sandbox.TestEnumComparer();
            }
            catch (SecurityException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
#endif

        #endregion
    }
}
