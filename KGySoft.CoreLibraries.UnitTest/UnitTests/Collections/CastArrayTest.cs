#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CastArrayTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Permissions;
using System.Security;

using KGySoft.Collections;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class CastArrayTest : TestBase
    {
        #region Nested classes

#if NETFRAMEWORK
        private class Sandbox : MarshalByRefObject
        {
            internal void DoTest()
            {
                Assert.IsTrue(EnvironmentHelper.IsPartiallyTrustedDomain);
                var test = new CastArrayTest();
                test.CastTest();
                test.NullAndEmptyTest();
                test.SliceTest();
                test.IndexOfTest();
                test.CopyToTest();
            }
        }
#endif

        #endregion

        #region Methods

        [Test]
        public void CastTest()
        {
            // same size
            var boolAsByte = new[] { false, true }.Cast<bool, byte>();
            Assert.AreEqual(2, boolAsByte.Length);
            Assert.AreEqual((byte)0, boolAsByte[0]);
            Assert.IsFalse(boolAsByte.Buffer[0]);
            boolAsByte[0] = 1;
            Assert.AreEqual((byte)1, boolAsByte[0]);
            Assert.IsTrue(boolAsByte.Buffer[0]);
            var boolAsSbyte = boolAsByte.Cast<sbyte>();
            Assert.AreEqual(2, boolAsSbyte.Length);
            Assert.AreEqual((sbyte)1, boolAsSbyte[0]);
            Assert.IsTrue(boolAsSbyte.Buffer[0]);

            // byte-size source
            var byteAsInt = new byte[5].Cast<byte, int>();
            Assert.AreEqual(1, byteAsInt.Length);
            Assert.AreEqual(5, byteAsInt.Buffer.Length);

            // 2 to 4 bytes
            var wordAsInt = new short[5].Cast<short, int>();
            Assert.AreEqual(2, wordAsInt.Length);

            // 4 to 2 bytes
            var intAsWord = new int[5].Cast<int, short>();
            Assert.AreEqual(10, intAsWord.Length);
        }

        [Test]
        public void NullAndEmptyTest()
        {
            CastArray<int, byte> array = null;
            Assert.IsTrue(array == null, "Compare with null works due to implicit operator");
            Assert.IsNotNull(array, "CastArray is actually a value type");
            Assert.IsTrue(array.IsNull);
            Assert.IsNull(array.ToArray());

            array = Reflector.EmptyArray<int>();
            Assert.AreEqual(CastArray<int, byte>.Empty, array);
            Assert.IsTrue(array == CastArray<int, byte>.Empty);
            Assert.IsFalse(array.IsNull);
            Assert.IsTrue(array.IsNullOrEmpty);
            Assert.IsTrue(array.Length == 0);

            Assert.AreNotEqual(CastArray<int, byte>.Null, CastArray<int, byte>.Empty);
            CollectionAssert.AreEqual(CastArray<int, byte>.Null, CastArray<int, byte>.Empty);
        }

        [Test]
        public void SliceTest()
        {
            // same size: always works
            CastArray<char, ushort> charAsWord = new char[5];
            for (int i = 0; i < charAsWord.Length; i++)
                charAsWord[i] = (ushort)i;

            for (int i = 0; i < charAsWord.Length; i++)
            {
                var slice = charAsWord.Slice(i, charAsWord.Length - i);
                Assert.AreEqual(charAsWord.Length - i, slice.Length);
                CollectionAssert.AreEqual(slice, slice.ToArray());
            }

            // byte-size source: always works
            CastArray<byte, uint> byteAsDword = new byte[10];
            for (int i = 0; i < byteAsDword.Length; i++)
                byteAsDword[i] = (uint)i;

            for (int i = 0; i < byteAsDword.Length; i++)
            {
                var slice = byteAsDword.Slice(i, byteAsDword.Length - i);
                Assert.AreEqual(byteAsDword.Length - i, slice.Length);
                CollectionAssert.AreEqual(slice, slice.ToArray());
            }

            // TTo can be divided by TFrom: always works
            CastArray<ushort, uint> wordAsDword = new ushort[10];
            for (int i = 0; i < wordAsDword.Length; i++)
                wordAsDword[i] = (uint)i;

            for (int i = 0; i < wordAsDword.Length; i++)
            {
                var slice = wordAsDword.Slice(i, wordAsDword.Length - i);
                Assert.AreEqual(wordAsDword.Length - i, slice.Length);
                CollectionAssert.AreEqual(slice, slice.ToArray());
            }

            // TTo cannot be divided by TFrom: works only if startIndex is aligned with TFrom
            CastArray<ushort, byte> wordAsByte = new ushort[10];
            for (int i = 0; i < wordAsByte.Length; i++)
                wordAsByte[i] = (byte)i;

            for (int i = 0; i < wordAsByte.Length; i++)
            {
                [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Not accessed after returning from the call")]
                int SliceAndGetLength() => wordAsByte.Slice(i, wordAsByte.Length - i).Length;

                // ISSUE: Assert.That throws SecurityException from partially trusted domain when asserts ArgumentException
                //Assert.That((ActualValueDelegate<int>)SliceAndGetLength, i % 2 == 0 ? Is.EqualTo(wordAsByte.Length - i) : NUnit.Framework.Throws.ArgumentException);

                if (i % 2 == 0)
                    Assert.AreEqual(wordAsByte.Length - i, SliceAndGetLength());
                else
                    Throws<ArgumentException>(() => SliceAndGetLength(), Res.CastArraySliceWrongStartIndex(i, typeof(ushort), typeof(byte)));

                // but Span/Memory always works
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                var mem = wordAsByte.AsMemory;
                var span = wordAsByte.AsSpan;
                var sliceMem = mem.Slice(i, mem.Length - i);
                var sliceSpan = span.Slice(i, span.Length - i);
                Assert.IsTrue(sliceSpan.SequenceEqual(sliceMem.Span));
#endif
            }
        }

        [Test]
        public void IndexOfTest()
        {
            var intAsBytes = new[] { 0x12345678 }.Cast<int, byte>();
            Assert.AreEqual(BitConverter.IsLittleEndian ? 3 : 0, intAsBytes.IndexOf(0x12));
            Assert.IsFalse(intAsBytes.Contains(0));
        }

        [Test]
        public void CopyToTest()
        {
            CastArray<ushort, byte> wordAsByte = new ushort[10];
            for (int i = 0; i < wordAsByte.Length; i++)
                wordAsByte[i] = (byte)i;

            var buf = new byte[wordAsByte.Length * 2];
            for (int i = 0; i < wordAsByte.Length; i++)
            {
                wordAsByte.CopyTo(buf, i);
                CollectionAssert.AreEqual(wordAsByte, buf.AsSection(i, wordAsByte.Length));
            }
        }

        [Test]
        public void ConstraintTest()
        {
            Throws<TypeInitializationException>(() => Reflector.CreateInstance(typeof(CastArray<,>), [typeof(int), typeof(DictionaryEntry)], new int[1].AsSection()));
        }

        [Test]
        public void SerializationTest()
        {
            throw new NotImplementedException();
        }

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical]
        public void CastArray_PartiallyTrusted()
        {
            var domain = CreateSandboxDomain(
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode | SecurityPermissionFlag.SerializationFormatter),
                new FileIOPermission(PermissionState.Unrestricted),
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
            var handle = Activator.CreateInstance(domain, Assembly.GetExecutingAssembly().FullName, typeof(Sandbox).FullName!);
            var sandbox = (Sandbox)handle.Unwrap();
            try
            {
                sandbox.DoTest();
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
