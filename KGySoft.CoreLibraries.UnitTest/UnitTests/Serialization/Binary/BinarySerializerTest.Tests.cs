#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializerTest.Tests.cs
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
#if NETFRAMEWORK
using System.CodeDom.Compiler;
#endif
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
#if !NET35
using System.Numerics;
#endif
#if NETFRAMEWORK
using System.Reflection;
using System.Runtime.Remoting.Messaging;
#endif
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#if NETFRAMEWORK
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
#endif
using System.Text;

using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Binary
{
    /// <summary>
    /// Test of <see cref="BinarySerializer"/> class.
    /// </summary>
    [TestFixture]
    public partial class BinarySerializerTest : TestBase
    {
        #region Sandbox class

#if NETFRAMEWORK
        private class Sandbox : MarshalByRefObject
        {
            internal void DoTest()
            {
#if !NET35
                Assert.IsFalse(AppDomain.CurrentDomain.IsFullyTrusted);
#endif
                var test = new BinarySerializerTest();
                test.SerializeComplexTypes();
                test.SerializeComplexGenericCollections();
                test.SerializationSurrogateTest();
                test.SerializeRemoteObjects();
                test.SerializationBinderTest();
            }
        }
#endif

        #endregion

        #region Constants

        private const bool dumpDetails = false;
        private const bool dumpSerContent = false;

        #endregion

        #region Methods

        [Test]
        public void SerializeSimpleTypes()
        {
            object[] referenceObjects =
            {
                // primitive types (in terms of they are never custom serialized) so including string and void
                true,
                (sbyte)1,
                (byte)1,
                (short)1,
                (ushort)1,
                1,
                (uint)1,
                (long)1,
                (ulong)1,
                'a',
                "alpha",
                (float)1,
                (double)1,
                new IntPtr(1),
                new UIntPtr(1),

                // simple non-primitive types
                new object(),
                (decimal)1,
                DateTime.UtcNow,
                DateTime.Now,
                new Version(1, 2, 3, 4),
                new Guid("ca761232ed4211cebacd00aa0057b223"),
                new TimeSpan(1, 1, 1),
                new DateTimeOffset(DateTime.Now),
                new DateTimeOffset(DateTime.UtcNow),
                new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)),
                new Uri(@"x:\teszt"), // 19
                new DictionaryEntry(1, "alpha"),
                new KeyValuePair<int, string>(1, "alpha"),
                new BitArray(new[] { true, false, true }),
                new StringBuilder("alpha"),
#if !NET35
                new BigInteger(1),
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            // further natively supported types, which are not serializable in every framework
            referenceObjects = new object[]
            {
                null,
                DBNull.Value,
                new BitVector32(13),
                BitVector32.CreateSection(13),
                BitVector32.CreateSection(42, BitVector32.CreateSection(13)),
                typeof(int),

#if NETCOREAPP3_0_OR_GREATER
#if !NETSTANDARD_TEST
	            new Rune('a'),  
#endif
                new Index(1),
                new Range(Index.FromStart(13), Index.FromEnd(13)),
#endif

#if NET5_0_OR_GREATER
                (Half)1,
#endif

#if NET6_0_OR_GREATER
                DateOnly.FromDateTime(DateTime.Today),
                TimeOnly.FromDateTime(DateTime.Now),
#endif
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

#if !NETCOREAPP2_0 // .NET Core 2.0 throws NotSupportedException for DBNull and RuntimeType.GetObjectData. In .NET Core 3 they work but Equals fails for cloned RuntimeType, hence safeCompare
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
#endif
        }

        [Test]
        public void SerializeCompressibleValues()
        {
            object[] referenceObjects =
            {
                // 1 bytes
                SByte.MinValue,
                SByte.MaxValue,
                Byte.MinValue,
                Byte.MaxValue,

                // 2 bytes
                Int16.MinValue,
                (short)TestEnumShort.Treshold,
                Int16.MaxValue,
                (ushort)TestEnumUShort.Treshold,
                UInt16.MaxValue,
                Char.MaxValue,

                // 2 bytes compressed
                (short)TestEnumShort.Limit,
                UInt16.MinValue,
                (ushort)TestEnumUShort.Limit,
                Char.MinValue,

                // 4 bytes
                Int32.MinValue,
                (int)TestEnumInt.Treshold,
                Int32.MaxValue,
                (uint)TestEnumUInt.Treshold,
                UInt32.MaxValue,
                Single.MaxValue,

                // 4 bytes compressed
                (int)TestEnumInt.Limit,
                UInt32.MinValue,
                (uint)TestEnumUInt.Limit,
                Single.Epsilon,

                // 8 bytes
                Int64.MinValue,
                (long)TestEnumLong.Treshold,
                Int64.MaxValue,
                (ulong)TestEnumULong.Treshold,
                UInt64.MaxValue,
                Double.MaxValue,
                new IntPtr(IntPtr.Size == 4 ? Int32.MaxValue : Int64.MaxValue),
                new UIntPtr(UIntPtr.Size == 4 ? UInt32.MaxValue : UInt64.MaxValue),

                // 8 bytes compressed
                (long)TestEnumLong.Limit,
                UInt64.MinValue,
                (ulong)TestEnumULong.Limit,
                Double.Epsilon,
                IntPtr.Zero,
                UIntPtr.Zero
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None); // 217
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None); // 174
        }

        [Test]
        public void SerializeEnums()
        {
            object[] referenceObjects =
            {
                // local enums, testing 7-bit encodings
                TestEnumByte.Min,
                TestEnumByte.Max,
                TestEnumSByte.Min,
                TestEnumSByte.Max,

                TestEnumShort.Min,
                TestEnumShort.Limit,
                TestEnumShort.Treshold,
                TestEnumShort.Max,

                TestEnumUShort.Min,
                TestEnumUShort.Limit,
                TestEnumUShort.Treshold,
                TestEnumUShort.Max,

                TestEnumInt.Min,
                TestEnumInt.Limit,
                TestEnumInt.Treshold,
                TestEnumInt.Max,

                TestEnumUInt.Min,
                TestEnumUInt.Limit,
                TestEnumUInt.Treshold,
                TestEnumUInt.Max,

                TestEnumLong.Min,
                TestEnumLong.Limit,
                TestEnumLong.Treshold,
                TestEnumLong.Max,

                TestEnumULong.Min,
                TestEnumULong.Limit,
                TestEnumULong.Treshold,
                TestEnumULong.Max,

                ConsoleColor.White, // mscorlib enum
                ConsoleColor.Black, // mscorlib enum

                UriKind.Absolute, // System enum
                UriKind.Relative, // System enum

                HandleInheritability.Inheritable, // System.Core enum

                BinarySerializationOptions.RecursiveSerializationAsFallback, // KGySoft.CoreLibraries enum
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
        }

        [Test]
        public void SerializeTypes()
        {
            object[] referenceObjects =
            {
                // Simple types
                typeof(int),
                typeof(int?),

                typeof(int).MakeByRefType(),
                typeof(int).MakePointerType(),
                typeof(CustomSerializedClass),
                typeof(CustomSerializableStruct?),
                Reflector.RuntimeType,
                typeof(void),
                typeof(TypedReference),

                // Arrays
                typeof(int[]),
                typeof(int[,]),
                typeof(int).MakeArrayType(1), // int[*]
                typeof(CustomSerializedClass[]), // custom array
                typeof(Array), // unspecified array

                // Pointers and References
                typeof(int*),
                typeof(int**),
                typeof(void*),
                typeof(void**),
                typeof(int*[]),
                typeof(int**[,]),
                typeof(int*[][]),
                typeof(int).MakeByRefType(), // int&
                typeof(int*).MakeByRefType(), // int*&
                typeof(int[]).MakePointerType(), // int[]* - actually not a valid type
                typeof(int[]).MakePointerType().MakePointerType(), // int[]** - actually not a valid type
                typeof(int[]).MakePointerType().MakePointerType().MakeByRefType(), // int[]**& - actually not a valid type

                // Closed Constructed Generics
                typeof(List<int>), // supported generic
                typeof(CustomGenericCollection<CustomSerializedClass>), // custom generic
                typeof(CustomGenericCollection<int>), // custom generic with supported parameter
                typeof(List<CustomSerializedClass>), // supported generic with custom parameter
                typeof(Dictionary<string, CustomSerializedClass>), // supported generic with mixed parameters
                typeof(List<Array>),
                typeof(List<int[]>),
                typeof(List<Array[]>),
                typeof(List<int>).MakeArrayType().MakePointerType().MakeArrayType(2).MakePointerType().MakeByRefType(), // List`1[System.Int32][]*[,]*&

                // Nullable collections
                typeof(DictionaryEntry?),
                typeof(KeyValuePair<int, string>?),
                typeof(KeyValuePair<int, CustomSerializedClass>?), // supported generic with mixed parameters

                // Generic Type Definitions
                typeof(List<>), // List`1, supported generic type definition
                typeof(List<>).MakeArrayType(), // List`1[] - does not really make sense
                typeof(List<>).MakeByRefType(), // List`1& - does not really make sense
                typeof(List<>).MakePointerType(), // List`1* - not really valid
                typeof(List<>).MakeArrayType().MakeByRefType(), // List`1[]& - does not really make sense
                typeof(List<>).MakeArrayType().MakePointerType().MakeArrayType(2).MakePointerType().MakeByRefType(), // List`1[]*[,]*&
                typeof(Dictionary<,>), // supported generic type definition
                typeof(CustomGenericCollection<>), // CustomGenericCollection`1, custom generic type definition
                typeof(CustomGenericCollection<>).MakeArrayType(), // CustomGenericCollection`1[] - does not really make sense
                typeof(CustomGenericCollection<>).MakeByRefType(), // CustomGenericCollection`1& - does not really make sense
                typeof(CustomGenericCollection<>).MakePointerType(), // CustomGenericCollection`1* - not really valid
                typeof(Nullable<>), // known special type definition
                typeof(Nullable<>).MakeArrayType(),
                typeof(Nullable<>).MakeByRefType(),
                typeof(Nullable<>).MakePointerType(),
                typeof(KeyValuePair<,>), // supported special type definition

                // Generic Type Parameters
                typeof(List<>).GetGenericArguments()[0], // T of supported generic type definition argument
                typeof(List<>).GetGenericArguments()[0].MakeArrayType(), // T[]
                typeof(List<>).GetGenericArguments()[0].MakeByRefType(), // T&
                typeof(List<>).GetGenericArguments()[0].MakePointerType(), // T*
                typeof(List<>).GetGenericArguments()[0].MakeArrayType().MakeByRefType(), // T[]&
                typeof(List<>).GetGenericArguments()[0].MakeArrayType().MakePointerType().MakeArrayType(2).MakePointerType().MakeByRefType(), // T[]*[,]*&
                typeof(CustomGenericCollection<>).GetGenericArguments()[0], // T of custom generic type definition argument
                typeof(CustomGenericCollection<>).GetGenericArguments()[0].MakeArrayType(), // T[]

                // Open Constructed Generics
                typeof(List<>).MakeGenericType(typeof(KeyValuePair<,>)), // List<KeyValuePair<,>>
                typeof(List<>).MakeGenericType(typeof(List<>)), // List<List<>>
                typeof(List<>).MakeGenericType(typeof(List<>).GetGenericArguments()[0]), // List<T>
                typeof(OpenGenericDictionary<>).BaseType, // open constructed generic (Dictionary<string, TValue>)
                typeof(KeyValuePair<,>).MakeGenericType(typeof(int), typeof(KeyValuePair<,>).GetGenericArguments()[1]), // open constructed generic (KeyValuePair<int, TValue>)
                typeof(Nullable<>).MakeGenericType(typeof(KeyValuePair<,>)), // open constructed generic (KeyValuePair<,>?)
                typeof(Nullable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(typeof(int), typeof(KeyValuePair<,>).GetGenericArguments()[1])), // open constructed generic (KeyValuePair<int, TValue>?)

                // Generic Method Parameters
                typeof(Array).GetMethod(nameof(Array.Resize)).GetGenericArguments()[0], // T of Array.Resize, unique generic method definition argument
                //typeof(Array).GetMethod(nameof(Array.Resize)).GetGenericArguments()[0].MakeArrayType(), // T[] of Array.Resize - System and forced recursive serialization fails here: T != T[]
                typeof(DictionaryExtensions).GetMethods().Where(mi => mi.Name == nameof(DictionaryExtensions.GetValueOrDefault)).ElementAt(2).GetGenericArguments()[0] // TKey of a GetValueOrDefault overload, ambiguous generic method definition argument
            };

#if !NETCOREAPP // Type is not serializable in .NET Core
            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

#if NETFRAMEWORK
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
#elif NETCOREAPP3_0 // RuntimeType.GetObjectData throws PlatformNotSupportedException in .NET Core 2.0. In .NET Core 3.0 it works but the Equals fails for the clones, hence safeCompare
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
#endif
        }

        [Test]
        public void SerializeComplexTypes()
        {
            object[] referenceObjects =
            {
                new BinarySerializableSealedClass(3, "gamma"),
                new BinarySerializableClass { IntProp = 1, StringProp = "alpha" },
                new BinarySerializableStruct { IntProp = 2, StringProp = "beta" },
                new BinarySerializableStructNoCtor { IntProp = 2, StringProp = "beta" },
                new SystemSerializableClass { IntProp = 3, StringProp = "gamma", Bool = null },

                new KeyValuePair<int, object>(1, new object[] { 1, "alpha", DateTime.Now, null }),
                (new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, 1),

                new SerializationEventsClass { Name = "Parent" }.AddChild("Child").AddChild("GrandChild").Parent.Parent,
                new CustomSerializedClass { Name = "Single node" }, // ISerializable
                new CustomSerializedClass { Name = "Parent derived", Bool = null }.AddChild("Child base").AddChild("GrandChild base").Parent.Parent,
                new CustomSerializedSealedClass("Parent advanced derived").AddChild("Child base").AddChild("GrandChild base").Parent.Parent,
                DefaultGraphObjRef.Get(), // IObjectReference without ISerializable
                new CustomGraphDefaultObjRef { Name = "alpha" }, // obj is ISerializable but IObjectReference is not
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            referenceObjects = new object[]
            {
                new NonSerializableClass{ IntProp = 3, StringProp = "gamma" },
                new NonSerializableSealedClass(1, "alpha") { IntProp = 1, StringProp = "alpha" },
                new NonSerializableStruct{ Bytes3 = new byte[] {1, 2, 3}, IntProp = 1, Str10 = "alpha" },
                (new NonSerializableStruct { IntProp = 1, Str10 = "alpha", Bytes3 = new byte[] { 1, 2, 3 } }, 1),
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);
        }

        [Test]
        public void SerializeByteArrays()
        {
            object[] referenceObjects =
            {
                new byte[] { 1, 2, 3 }, // single byte array
                new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, // multidimensional byte array
                new byte[][] { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23, 24, 25 }, null }, // jagged byte array
                new byte[][,] { new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, new byte[,] { { 11, 12, 13, 14 }, { 21, 22, 23, 24 }, { 31, 32, 33, 34 } } }, // crazy jagged byte array 1 (2D matrix of 1D arrays)
                new byte[,][] { { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23 } }, { new byte[] { 11, 12, 13, 14 }, new byte[] { 21, 22, 23, 24 } } }, // crazy jagged byte array 2 (1D array of 2D matrices)
                new byte[][,,] { new byte[,,] { { { 11, 12, 13 }, { 21, 21, 23 } } }, null }, // crazy jagged byte array containing null reference
                Array.CreateInstance(typeof(byte), new int[] { 3 }, new int[] { -1 }), // array with -1..1 index interval
                Array.CreateInstance(typeof(byte), new int[] { 3, 3 }, new int[] { -1, 1 }) // array with [-1..1 and 1..3] index interval
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
        }

        [Test]
        public void SerializeSimpleArrays()
        {
            object[] referenceObjects =
            {
                new object[] { new object(), null },
                new bool[] { true, false },
                new sbyte[] { 1, 2 },
                new byte[] { 1, 2 },
                new short[] { 1, 2 },
                new ushort[] { 1, 2 },
                new int[] { 1, 2 },
                new uint[] { 1, 2 },
                new long[] { 1, 2 },
                new ulong[] { 1, 2 },
                new char[] { 'a', 'á' }, // Char.ConvertFromUtf32(0x1D161)[0] }, //U+1D161 = MUSICAL SYMBOL SIXTEENTH NOTE, serializing its low-surrogate <- System serializer fails at compare
                new string[] { "alpha", null },
                new float[] { 1, 2 },
                new double[] { 1, 2 },
                new decimal[] { 1, 2 },
                new DateTime[] { DateTime.UtcNow, DateTime.Now },
                new IntPtr[] { new IntPtr(1), IntPtr.Zero },
                new UIntPtr[] { new UIntPtr(1), UIntPtr.Zero },
                new Version[] { new Version(1, 2, 3, 4), null },
                new Guid[] { new Guid("ca761232ed4211cebacd00aa0057b223"), Guid.NewGuid() },
                new TimeSpan[] { new TimeSpan(1, 1, 1), new TimeSpan(DateTime.UtcNow.Ticks) },
                new DateTimeOffset[] { new DateTimeOffset(DateTime.Now), new DateTimeOffset(DateTime.UtcNow), new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)) },
                new Uri[] { new Uri(@"x:\teszt"), new Uri("ftp://myUrl/%2E%2E/%2E%2E"), null },
                new DictionaryEntry[] { new DictionaryEntry(1, "alpha") },
                new KeyValuePair<int, string>[] { new KeyValuePair<int, string>(1, "alpha") },
                new BitArray[] { new BitArray(new[] { true, false, true }), null },
                new StringBuilder[] { new StringBuilder("alpha"), null },
#if !NET35
                new BigInteger[] { 1, 2 }
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            referenceObjects = new object[]
            {
                new DBNull[] { DBNull.Value, null },
                new BitVector32[] { new BitVector32(13) },
                new BitVector32.Section[] { BitVector32.CreateSection(13), BitVector32.CreateSection(42, BitVector32.CreateSection(13)) },
                new Type[] { typeof(int), typeof(List<int>), null },
                Array.CreateInstance(Reflector.RuntimeType, 3), // runtime type array, set below
#if NETCOREAPP3_0_OR_GREATER
#if !NETSTANDARD_TEST
	            new Rune[] { new Rune('a') },  
#endif
                new Index[] { new Index(1), new Index(1, true) },
                new Range[] { new Range(1, 2), new Range(Index.Start, Index.End) },
#endif
#if NET5_0_OR_GREATER
                new Half[] { (Half)1, (Half)1.25 },
#endif
#if NET6_0_OR_GREATER
                new DateOnly[] { DateOnly.FromDateTime(DateTime.Today), DateOnly.MaxValue },
                new TimeOnly[] { TimeOnly.FromDateTime(DateTime.Now), TimeOnly.MaxValue },
#endif
            };

            ((Array)referenceObjects[4]).SetValue(typeof(int), 0);
            ((Array)referenceObjects[4]).SetValue(Reflector.RuntimeType, 1);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

#if !NETCOREAPP2_0 // .NET Core 2.0 throws NotSupportedException for DBNull and RuntimeType.GetObjectData. In .NET Core 3 they work but Equals fails for cloned RuntimeType, hence safeCompare
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
#endif
        }

        /// <summary>
        /// Enum types must be described explicitly
        /// </summary>
        [Test]
        public void SerializeEnumArrays()
        {
            object[] referenceObjects =
            {
                new TestEnumByte[] { TestEnumByte.One, TestEnumByte.Two }, // single enum array
                new TestEnumByte[,] { { TestEnumByte.One }, { TestEnumByte.Two } }, // multidimensional enum array
                new TestEnumByte[][] { new TestEnumByte[] { TestEnumByte.One }, new TestEnumByte[] { TestEnumByte.Two } }, // jagged enum array

                new object[] { TestEnumByte.One, null },
                new IConvertible[] { TestEnumByte.One, null },
                new Enum[] { TestEnumByte.One, null },
                new ValueType[] { TestEnumByte.One, null },
            };

            SystemSerializeObject(referenceObjects);
            //SystemSerializeObjects(referenceObjects); // System serializer fails with IConvertible is not serializable

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
        }

        /// <summary>
        /// String has variable length and can be null.
        /// </summary>
        [Test]
        public void SerializeStringArrays()
        {
            object[] referenceObjects =
            {
                new string[] { "One", "Two" }, // single string array
                new string[,] { { "One", "Two" }, { "One", "Two" } }, // multidimensional string array
                new string[][] { new string[] { "One", "Two", "Three" }, new string[] { "One", "Two", null }, null }, // jagged string array with null values (first null as string, second null as array)
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None); // 100 -> 63
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            referenceObjects = new object[]
            {
                // system serializer fails: cannot cast string[*] to object[]
                Array.CreateInstance(typeof(string), new int[] {3}, new int[]{-1}) // array with -1..1 index interval
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None); // 17 -> 20
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
        }

        [Test]
        public void SerializeComplexArrays()
        {
            object[] referenceObjects =
            {
                new BinarySerializableStruct[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, new BinarySerializableStruct { IntProp = 2, StringProp = "beta" } }, // array of a BinarySerializable struct - None: 161
                new BinarySerializableClass[] { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new BinarySerializableClass { IntProp = 2, StringProp = "beta" } }, // array of a BinarySerializable non sealed class - None: 170
                new BinarySerializableClass[] { new BinarySerializableSealedClass(1, "alpha"), new BinarySerializableSealedClass(2, "beta") }, // array of a BinarySerializable non sealed class with derived elements - None: 240
                new BinarySerializableSealedClass[] { new BinarySerializableSealedClass(1, "alpha"), new BinarySerializableSealedClass(2, "beta"), new BinarySerializableSealedClass(3, "gamma") }, // array of a BinarySerializable sealed class - None: 189
                new SystemSerializableClass[] { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { IntProp = 2, StringProp = "beta" } }, // array of a [Serializable] object - None: 419
                new SystemSerializableStruct[] { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, new SystemSerializableStruct { IntProp = 2, StringProp = "beta" } }, // None: 276 -> 271
                new AbstractClass[] { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { IntProp = 2, StringProp = "beta" } }, // array of a [Serializable] object - None: 467 -> 469
                new AbstractClass[] { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { IntProp = 2, StringProp = "beta" } }, // array of a [Serializable] object, with an IBinarySerializable element - 458 -> 393

                new KeyValuePair<int, object>[] { new KeyValuePair<int, object>(1, "alpha"), new KeyValuePair<int, object>(2, new TestEnumByte[] { TestEnumByte.One, TestEnumByte.Two }), }, // None: 151
                new KeyValuePair<int, CustomSerializedClass>[] { new KeyValuePair<int, CustomSerializedClass>(1, new CustomSerializedClass { Bool = true, Name = "alpha" }), new KeyValuePair<int, CustomSerializedClass>(2, null) }, // None: 341
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            referenceObjects = new object[]
            {
                new SystemSerializableClass[] { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { IntProp = 2, StringProp = "beta" }, new NonSerializableClassWithSerializableBase(3, "gamma") }, // a non serializable element among the serializable ones
                new NonSerializableClass[] { new NonSerializableClass { IntProp = 1, StringProp = "alpha" }, new NonSerializableSealedClass(1, "beta") { IntProp = 3, StringProp = "gamma" } },
                new NonSerializableSealedClass[] { new NonSerializableSealedClass(1, "alpha") { IntProp = 2, StringProp = "beta" }, null },
                new IBinarySerializable[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, new BinarySerializableClass { IntProp = 2, StringProp = "beta" }, new BinarySerializableSealedClass(3, "gamma") }, // IBinarySerializable array
                new IBinarySerializable[][] { new IBinarySerializable[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" } }, null }, // IBinarySerializable array
                new NonSerializableStruct[] { new NonSerializableStruct { IntProp = 1, Str10 = "alpha", Bytes3 = new byte[] { 1, 2, 3 } }, new NonSerializableStruct { IntProp = 2, Str10 = "beta", Bytes3 = new byte[] { 3, 2, 1 } } }, // array custom struct
                new ValueType[] { new NonSerializableStruct { IntProp = 1, Str10 = "alpha", Bytes3 = new byte[] { 1, 2, 3 } }, new SystemSerializableStruct { IntProp = 2, StringProp = "beta" }, new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" } }, // elements with mixes serialization strategies

                new ValueType[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, new SystemSerializableStruct { IntProp = 2, StringProp = "beta" }, null, 1 },
                new IConvertible[] { null, 1 },
                new IConvertible[][] { null, new IConvertible[] { null, 1 }, },

                new Array[] { null, new[] { 1, 2 }, new[] { "alpha", "beta" } },
                new Enum[] { null, TestEnumByte.One, TestEnumInt.Min }
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);
        }

        [Test]
        public void SerializeNullableArrays()
        {
            object[] referenceObjects =
            {
                new bool?[] { true, false, null }, // 10
                new sbyte?[] { 1, 2, null }, // 10
                new byte?[] { 1, 2, null }, // 10
                new short?[] { 1, 2, null }, // 12
                new ushort?[] { 1, 2, null }, //12
                new int?[] { 1, 2, null }, // -> 16
                new uint?[] { 1, 2, null }, // 16
                new long?[] { 1, 2, null }, // 24
                new ulong?[] { 1, 2, null }, // 24
                new char?[] { 'a', /*Char.ConvertFromUtf32(0x1D161)[0],*/ null }, // 9
                new float?[] { 1, 2, null }, // 16
                new double?[] { 1, 2, null }, // 24
                new decimal?[] { 1, 2, null }, // 40
                new DateTime?[] { DateTime.UtcNow, DateTime.Now, null }, // 26
                new IntPtr?[] { new IntPtr(1), IntPtr.Zero, null }, // 24
                new UIntPtr?[] { new UIntPtr(1), UIntPtr.Zero, null }, // 24
                new Guid?[] { new Guid("ca761232ed4211cebacd00aa0057b223"), Guid.NewGuid(), null }, // 40
                new TimeSpan?[] { new TimeSpan(1, 1, 1), new TimeSpan(DateTime.UtcNow.Ticks), null }, // 24
                new DateTimeOffset?[] { new DateTimeOffset(DateTime.Now), new DateTimeOffset(DateTime.UtcNow), new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)), null }, // 39

                new TestEnumByte?[] { TestEnumByte.One, TestEnumByte.Two, null },

                new DictionaryEntry?[] { new DictionaryEntry(1, "alpha"), null }, // 21
                new KeyValuePair<int, string>?[] { new KeyValuePair<int, string>(1, "alpha"), null }, // 21
                new KeyValuePair<int?, int?>?[] { new KeyValuePair<int?, int?>(1, 2), new KeyValuePair<int?, int?>(2, null), null }, // 28
                new KeyValuePair<KeyValuePair<int?, string>?, KeyValuePair<int?, string>?>?[] { new KeyValuePair<KeyValuePair<int?, string>?, KeyValuePair<int?, string>?>(new KeyValuePair<int?, string>(1, "alpha"), new KeyValuePair<int?, string>(2, "beta")),  }, // 28

                new BinarySerializableStruct?[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, null },
                new SystemSerializableStruct?[] { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, null },

#if !NET35
                new BigInteger?[] { 1, null },
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            referenceObjects = new object[]
            {
                new NonSerializableStruct?[] { new NonSerializableStruct { Bytes3 = new byte[] { 1, 2, 3 }, IntProp = 10, Str10 = "alpha" }, null },
                new BitVector32?[] { new BitVector32(13), null },
                new BitVector32.Section?[] { BitVector32.CreateSection(13), null },

#if NETCOREAPP3_0_OR_GREATER
                new Rune?[] { new Rune('a'), null },
                new Index?[] { new Index(1), null },
                new Range?[] { new Range(5, 10), null },
#endif
#if NET5_0_OR_GREATER
                new Half?[] { (Half)1, null },
#endif
#if NET6_0_OR_GREATER
                new DateOnly?[] { DateOnly.FromDateTime(DateTime.Today), null },
                new TimeOnly?[] { TimeOnly.FromDateTime(DateTime.Now), null },
#endif
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.IgnoreIBinarySerializable);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.IgnoreIBinarySerializable);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback);

#pragma warning disable 618 // obsolete
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForcedSerializationValueTypesAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForcedSerializationValueTypesAsFallback);
#pragma warning restore 618

            KGySerializeObject(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.RecursiveSerializationAsFallback);
        }

        [Test]
        public void SerializeSimpleGenericCollections()
        {
            object[] referenceObjects =
            {
                new List<int> { 1, 2, 3 },
                new List<int[]> { new int[] { 1, 2, 3 }, null },

                new LinkedList<int>(new[] { 1, 2, 3 }),
                new LinkedList<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new HashSet<int> { 1, 2, 3 },
                new HashSet<int[]> { new int[] { 1, 2, 3 }, null },
                new HashSet<string>(StringComparer.CurrentCulture) { "alpha", "Alpha", "ALPHA" },
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha", "Alpha", "ALPHA" },
                new HashSet<TestEnumByte>(EnumComparer<TestEnumByte>.Comparer) { TestEnumByte.One, TestEnumByte.Two },

                new Queue<int>(new[] { 1, 2, 3 }),
                new Queue<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new Stack<int>(new[] { 1, 2, 3 }),
                new Stack<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new CircularList<int>(new[] { 1, 2, 3 }),
                new CircularList<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

#if !NET35
                new SortedSet<int>(new[] { 1, 2, 3 }),
                new SortedSet<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),
                new SortedSet<string>(StringComparer.CurrentCulture) { "alpha", "Alpha", "ALPHA" },
                new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha", "Alpha", "ALPHA" },
#endif

                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new Dictionary<int, TestEnumByte> { { 1, TestEnumByte.One }, { 2, TestEnumByte.Two } },
                new Dictionary<int[], string[]> { { new int[] { 1 }, new string[] { "alpha" } }, { new int[] { 2 }, null } },
                new Dictionary<string, int>(StringComparer.CurrentCulture) { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } },
                new Dictionary<TestEnumByte, int>(EnumComparer<TestEnumByte>.Comparer) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },

                new SortedList<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new SortedList<int, string[]> { { 1, new string[] { "alpha" } }, { 2, null } },
                new SortedList<string, int>(StringComparer.OrdinalIgnoreCase) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },
                new SortedList<TestEnumByte, int>(Comparer<TestEnumByte>.Default) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },
                new SortedList<TestEnumByte, int>(EnumComparer<TestEnumByte>.Comparer) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },

                new SortedDictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new SortedDictionary<int, string[]> { { 1, new string[] { "alpha" } }, { 2, null } },
                new SortedDictionary<string, int>(StringComparer.CurrentCulture) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },
                new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },
                new SortedDictionary<TestEnumByte, int>(Comparer<TestEnumByte>.Default) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },
                new SortedDictionary<TestEnumByte, int>(EnumComparer<TestEnumByte>.Comparer) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },

                new CircularSortedList<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new CircularSortedList<int, string[]> { { 1, new string[] { "alpha" } }, { 2, null } },
                new CircularSortedList<string, int>(StringComparer.CurrentCulture) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },
                new CircularSortedList<string, int>(StringComparer.OrdinalIgnoreCase) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },
                new CircularSortedList<TestEnumByte, int>(Comparer<TestEnumByte>.Default) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },
                new CircularSortedList<TestEnumByte, int>(EnumComparer<TestEnumByte>.Comparer) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

#if NETCOREAPP2_0 || NETCOREAPP2_1
            // Only for HashSet<T> and .NET Core 2.x: typeof(IEqualityComparer<T>.IsAssignableFrom(comparer)) fails in HashSet.OnDeserialization. No idea why, and no idea why the same logic works for Dictionary.
            referenceObjects = referenceObjects.Where(o => !o.GetType().IsGenericTypeOf(typeof(HashSet<>))).ToArray();
#endif
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

#if !NET35
            referenceObjects = new object[]
            {
                new ConcurrentBag<int>(new[] { 1, 2, 3 }),
                new ConcurrentBag<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new ConcurrentQueue<int>(new[] { 1, 2, 3 }),
                new ConcurrentQueue<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new ConcurrentStack<int>(new[] { 1, 2, 3 }),
                new ConcurrentStack<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new ConcurrentDictionary<int, string>(new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }),
                new ConcurrentDictionary<int, TestEnumByte>(new Dictionary<int, TestEnumByte> { { 1, TestEnumByte.One }, { 2, TestEnumByte.Two } }),
                new ConcurrentDictionary<string, int>(new Dictionary<string, int> { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } }, StringComparer.CurrentCulture),
                new ConcurrentDictionary<TestEnumByte, int>(new Dictionary<TestEnumByte, int> { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } }, EnumComparer<TestEnumByte>.Comparer),
            };

#if NETFRAMEWORK // concurrent collections are not serializable in .NET Core
            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

#if NETFRAMEWORK
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
#else
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback);
#endif
#endif // !NET35
        }

        [Test]
        public void SerializeSimpleNonGenericCollections()
        {
            object[] referenceObjects =
            {
                new ArrayList { 1, "alpha", DateTime.Now },

                new Hashtable { { 1, "alpha" }, { (byte)2, "beta" }, { 3m, "gamma" } },
                new Hashtable(StringComparer.CurrentCulture) { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } },

                new Queue(new object[] { 1, (byte)2, 3m, new string[] { "alpha", "beta", "gamma" } }),

                new Stack(new object[] { 1, (byte)2, 3m, new string[] { "alpha", "beta", "gamma" } }),

                new StringCollection { "alpha", "beta", "gamma" },

                new SortedList { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new SortedList(StringComparer.CurrentCulture) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },
                new SortedList(StringComparer.OrdinalIgnoreCase) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },

                new ListDictionary { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new ListDictionary(StringComparer.CurrentCulture) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },
                new ListDictionary(StringComparer.OrdinalIgnoreCase) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 }, { "delta", 4 } },

                new HybridDictionary(false) { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } },

                new OrderedDictionary { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } },
                new OrderedDictionary { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } }.AsReadOnly(),
                new OrderedDictionary(StringComparer.OrdinalIgnoreCase) { { "alpha", 1 }, { "beta", 2 }, { "gamma", 3 } },

                new StringDictionary { { "a", "alpha" }, { "b", "beta" }, { "c", "gamma" }, { "x", null } },
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
        }

        [Test]
        public void SerializeRecursivelySerializedCollections()
        {
            object[] referenceObjects =
            {
                new Collection<int> { 1, 2, 3 },
                new Collection<int[]> { new int[] { 1, 2, 3 }, null },
                new Collection<ReadOnlyCollection<int>>(new Collection<ReadOnlyCollection<int>> { new ReadOnlyCollection<int>(new int[] { 1, 2, 3 }) }),
                new Collection<BinarySerializableStruct> { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, default(BinarySerializableStruct) },
                new Collection<SystemSerializableClass> { new SystemSerializableClass { Bool = null, IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { Bool = true, IntProp = 2, StringProp = "beta" }, null },

                // collections of keyvalue pairs (as object and strongly typed as well)
                new Collection<object> { new KeyValuePair<int, object>(1, "alpha"), new KeyValuePair<int, object>(2, DateTime.Today), new KeyValuePair<int, object>(3, new object()), new KeyValuePair<int, object>(4, new object[] { 1, "alpha", DateTime.Today, null }), new KeyValuePair<int, object>(5, null) },
                new Collection<KeyValuePair<int, object>> { new KeyValuePair<int, object>(1, "alpha"), new KeyValuePair<int, object>(2, DateTime.Today), new KeyValuePair<int, object>(3, new object()), new KeyValuePair<int, object>(4, new object[] { 1, "alpha", DateTime.Today, null }), new KeyValuePair<int, object>(5, null) },

                new ReadOnlyCollection<int>(new int[] { 1, 2, 3 }),
                new ReadOnlyCollection<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new CustomNonGenericCollection { "alpha", 2, null },
                new CustomNonGenericDictionary { { "alpha", 2 }, { "beta", null } },
                new CustomGenericCollection<int> { 1, 2, 3 },
                new CustomGenericDictionary<int, string> { { 1, "alpha" }, { 2, null } },

                new CustomGenericDictionary<TestEnumByte, CustomSerializedClass> { { TestEnumByte.One, new CustomSerializedClass { Name = "alpha" } } },

                new StringKeyedDictionary<int> { { "alpha", 1 }, { "beta", 2 } },
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
        }

        [Test]
        public void SerializeSupportedDictionaries()
        {
            object[] referenceObjects =
            {
                // generic collection value
                new Dictionary<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } }, // array
                new Dictionary<int, List<int>> { { 1, new List<int> { 1, 2 } }, { 2, null } }, // List
                new Dictionary<int, LinkedList<int>> { { 1, new LinkedList<int>(new[] { 1, 2 }) }, { 2, null } }, // LinkedList
                new Dictionary<int, HashSet<int>> { { 1, new HashSet<int> { 1, 2 } }, { 2, null } }, // HashSet
                new Dictionary<int, Queue<int>> { { 1, new Queue<int>(new[] { 1, 2 }) }, { 2, null } }, // Queue
                new Dictionary<int, Stack<int>> { { 1, new Stack<int>(new[] { 1, 2 }) }, { 2, null } }, // Stack
                new Dictionary<int, CircularList<int>> { { 1, new CircularList<int> { 1, 2 } }, { 2, null } }, // CircularList
#if !NET35
                new Dictionary<int, SortedSet<int>> { { 1, new SortedSet<int> { 1, 2 } }, { 2, null } }, // SortedSet
#endif

                // generic dictionary value
                new Dictionary<int, Dictionary<int, int>> { { 1, new Dictionary<int, int> { { 1, 2 } } }, { 2, null } }, // Dictionary
                new Dictionary<int, SortedList<int, int>> { { 1, new SortedList<int, int> { { 1, 2 } } }, { 2, null } }, // SortedList
                new Dictionary<int, SortedDictionary<int, int>> { { 1, new SortedDictionary<int, int> { { 1, 2 } } }, { 2, null } }, // SortedDictionary
                new Dictionary<int, KeyValuePair<int, int>> { { 1, new KeyValuePair<int, int>(1, 2) } }, // KeyValuePair
                new Dictionary<int, KeyValuePair<int, int>?> { { 1, new KeyValuePair<int, int>(1, 2) }, { 2, null } }, // KeyValuePair?
                new Dictionary<int, CircularSortedList<int, int>> { { 1, new CircularSortedList<int, int> { { 1, 2 } } }, { 2, null } }, // CircularSortedList

                // non-generic collection value
                new Dictionary<int, ArrayList> { { 1, new ArrayList { 1, 2 } }, { 2, null } }, // ArrayList
                new Dictionary<int, Queue> { { 1, new Queue(new[] { 1, 2 }) }, { 2, null } }, // Queue
                new Dictionary<int, Stack> { { 1, new Stack(new[] { 1, 2 }) }, { 2, null } }, // Stack
                new Dictionary<int, StringCollection> { { 1, new StringCollection() }, { 2, null } }, // StringCollection

                // non-generic dictionary value
                new Dictionary<int, Hashtable> { { 1, new Hashtable { { 1, 2 } } }, { 2, null } }, // Hashtable
                new Dictionary<int, SortedList> { { 1, new SortedList { { 1, 2 } } }, { 2, null } }, // SortedList
                new Dictionary<int, ListDictionary> { { 1, new ListDictionary { { 1, 2 } } }, { 2, null } }, // ListDictionary
                new Dictionary<int, HybridDictionary> { { 1, new HybridDictionary { { 1, 2 } } }, { 2, null } }, // HybridDictionary
                new Dictionary<int, OrderedDictionary> { { 1, new OrderedDictionary { { 1, 2 } } }, { 2, null } }, // OrderedDictionary
                new Dictionary<int, StringDictionary> { { 1, new StringDictionary { { "1", "2" } } }, { 2, null } }, // StringDictionary
                new Dictionary<int, DictionaryEntry> { { 1, new DictionaryEntry(1, 2) } }, // DictionaryEntry
                new Dictionary<int, DictionaryEntry?> { { 1, new DictionaryEntry(1, 2) }, { 2, null } }, // DictionaryEntry?

                // non-natively supported value: recursive
                new Dictionary<int, Collection<int>> { { 1, new Collection<int> { 1, 2 } }, { 2, null } }, // Collection
                new Dictionary<int, ReadOnlyCollection<int>> { { 1, new ReadOnlyCollection<int>(new[] { 1, 2 }) }, { 2, null } }, // ReadOnlyCollection

                // other generic dictionary types as outer objects
                new SortedList<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } },
                new SortedDictionary<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } },
                new KeyValuePair<int, int[]>(1, new[] { 1, 2 }),
                new CircularSortedList<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } },
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

#if !NET35
            referenceObjects = new object[]
            {
                new Dictionary<int, ConcurrentBag<int>> { { 1, new ConcurrentBag<int> { 1, 2 } }, { 2, null } },
                new Dictionary<int, ConcurrentQueue<int>> { { 1, new ConcurrentQueue<int>(new[] { 1, 2 }) }, { 2, null } },
                new Dictionary<int, ConcurrentStack<int>> { { 1, new ConcurrentStack<int>(new[] { 1, 2 }) }, { 2, null } },

                new ConcurrentDictionary<int, int[]>(new Dictionary<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } }),
            };

#if NETFRAMEWORK // value is not serializable
            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects); 
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
#endif
        }

        [Test]
        public void SerializeComplexGenericCollections()
        {
            object[] referenceObjects =
            {
                new List<byte>[] { new List<byte> { 11, 12, 13 }, new List<byte> { 21, 22 } }, // array of byte lists
                new List<byte[]> { new byte[] { 11, 12, 13 }, new byte[] { 21, 22 } }, // list of byte arrays
                new List<Array> { new byte[] { 11, 12, 13 }, new short[] { 21, 22 } }, // list of any arrays
                new List<Array[]> { null, new Array[] { new byte[] { 11, 12, 13 }, new short[] { 21, 22 } } }, // list of array of any arrays

                // a single key-value pair with a dictionary somewhere in value
                new KeyValuePair<int[], KeyValuePair<string, Dictionary<string, string>>>(new int[1], new KeyValuePair<string, Dictionary<string, string>>("gamma", new Dictionary<string, string> { { "alpha", "beta" } })),

                // dictionary with dictionary<int, string> value
                new Dictionary<string, Dictionary<int, string>> { { "hu", new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } } }, { "en", new Dictionary<int, string> { { 1, "apple" }, { 2, "frog" }, { 3, "cat" } } } },

                // dictionary with dictionary<int, IBinarySerializable> value
                new Dictionary<string, Dictionary<int, IBinarySerializable>> { { "alpha", new Dictionary<int, IBinarySerializable> { { 1, null }, { 2, new BinarySerializableClass { IntProp = 2, StringProp = "beta" } }, { 3, new BinarySerializableStruct { IntProp = 3, StringProp = "gamma" } } } }, { "en", null } },

                // dictionary with array key
                new Dictionary<string[], Dictionary<int, string>> { { new string[] { "hu" }, new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } } }, { new string[] { "en" }, new Dictionary<int, string> { { 1, "apple" }, { 2, "frog" }, { 3, "cat" } } } },

                // dictionary with dictionary key and value
                new Dictionary<Dictionary<int[], string>, Dictionary<int, string>> { { new Dictionary<int[], string> { { new int[] { 1 }, "key.value1" } }, new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } } }, { new Dictionary<int[], string> { { new int[] { 2 }, "key.value2" } }, new Dictionary<int, string> { { 1, "apple" }, { 2, "frog" }, { 3, "cat" } } } },

                // dictionary with many non-system types
#pragma warning disable CS0618 // Type or member is obsolete
                new SortedList<ConsoleColor, Dictionary<BinarySerializationOptions, IBinarySerializable>> { { ConsoleColor.White, new Dictionary<BinarySerializationOptions, IBinarySerializable> { { BinarySerializationOptions.ForcedSerializationValueTypesAsFallback, new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" } } } } },
#pragma warning restore CS0618 // Type or member is obsolete

                // List containing primitive, optionally customizable, always recursive and self type
                new List<object> { 1, DateTime.Today, ConsoleColor.Blue, new List<object> { 1 } },
                new List<object> { 1, "alpha", new Version(13, 0), new SystemSerializableClass { IntProp = 2, StringProp = "beta" }, new object[] { new BinarySerializableClass { IntProp = 3, StringProp = "gamma" } } },

                // dictionary with object key and value
                new Dictionary<object, object> { { 1, "alpha" }, { new object(), "beta" }, { new int[] { 3, 4 }, null }, { TestEnumByte.One, new BinarySerializableStruct { IntProp = 13, StringProp = "gamma" } } },

                // dictionary with read-only collection value
                new Dictionary<object, ReadOnlyCollection<int>> { { 1, new ReadOnlyCollection<int>(new[] { 1, 2 }) } },

                // lists with binary serializable elements
                new List<BinarySerializableStruct> { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, default(BinarySerializableStruct) },
                new List<BinarySerializableStruct?> { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, default(BinarySerializableStruct?) },
                new List<BinarySerializableClass> { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new BinarySerializableSealedClass(2, "beta"), null },
                new List<BinarySerializableSealedClass> { new BinarySerializableSealedClass(1, "alpha"), null },
                new List<IBinarySerializable> { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new BinarySerializableSealedClass(2, "beta"), new BinarySerializableStruct { IntProp = 3, StringProp = "gamma" }, null },

                // lists with default recursive elements
                new List<SystemSerializableStruct> { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(SystemSerializableStruct) },
                new List<SystemSerializableStruct?> { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(SystemSerializableStruct?) },
                new List<SystemSerializableClass> { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { IntProp = 2, StringProp = "beta" }, null },
                new List<SystemSerializableSealedClass> { new SystemSerializableSealedClass { IntProp = 1, StringProp = "alpha" }, null },

                // lists with custom recursive elements
                new List<CustomSerializableStruct> { new CustomSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(CustomSerializableStruct) },
                new List<CustomSerializableStruct?> { new CustomSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(CustomSerializableStruct?) },
                new List<CustomSerializedClass> { new CustomSerializedClass { Name = "alpha", Bool = true }, new CustomSerializedSealedClass("beta") { Bool = null }, null },
                new List<CustomSerializedSealedClass> { new CustomSerializedSealedClass("alpha") { Bool = false }, null },

                new IList<int>[] { new int[] { 1, 2, 3 }, new List<int> { 1, 2, 3 } },
                new List<IList<int>> { new int[] { 1, 2, 3 }, new List<int> { 1, 2, 3 } },
            };

            SystemSerializeObject(referenceObjects);
            //SystemSerializeObjects(referenceObjects); // System deserialization fails at List<IBinarySerializable>: IBinarySerializable/IList is not marked as serializable.

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
        }

        [Test]
        public void SerializeDerivedCollections()
        {
            object[] referenceObjects =
            {
                new List<List<int>> { new List<int> { 1 }, new CustomGenericCollection<int> { 2 }, null }, // unsealed outer and element
                new List<int[]> { new[] { 1 }, null }, // sealed element type
                new List<int>[] {  new List<int> { 1 }, new CustomGenericCollection<int> { 2 }, null }, // sealed outer collection
                new List<ArrayList> { new ArrayList { 1 }, new CustomNonGenericCollection { 2 } },
                new KeyValuePair<List<int>, ArrayList>(new CustomGenericCollection<int> { 2 }, new CustomNonGenericCollection { 2 }),
                new ListField { IntListField = new CustomGenericCollection<int> { 1 } }
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
        }

        [Test]
        public void SerializeCache()
        {
            object[] referenceObjects =
            {
                new Cache<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new Cache<int[], string[]> { { new int[] { 1 }, new string[] { "alpha" } }, { new int[] { 2 }, null } },
                new Cache<string, int>(StringComparer.CurrentCulture) { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } },
                new Cache<TestEnumByte, int> { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },
#if NETFRAMEWORK // SerializationException : Serializing delegates is not supported on this platform.
                new Cache<string, string>(s => s.ToUpper()) { { "alpha", "ALPHA" } },
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
        }

#if NETFRAMEWORK
        [Test]
        public void SerializeRemoteObjects()
        {
            Evidence evidence = new Evidence(AppDomain.CurrentDomain.Evidence);
            AppDomain domain = AppDomain.CreateDomain("TestDomain", evidence, AppDomain.CurrentDomain.BaseDirectory, null, false);
            try
            {
                object[] referenceObjects =
                {
                    new MemoryStreamWithEquals(), // local
                    domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(MemoryStreamWithEquals).FullName) // remote
                };

                // default - does not work for remote objects
                //SystemSerializeObjects(referenceObjects);
                //KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);

                // by surrogate (deserialization: default again because RemotingSurrogateSelector does not support SetObjectData)
                ISurrogateSelector surrogate = new RemotingSurrogateSelector();
                BinaryFormatter bf = new BinaryFormatter();
                BinarySerializationFormatter bsf = new BinarySerializationFormatter(BinarySerializationOptions.RecursiveSerializationAsFallback);

                Console.WriteLine($"------------------System BinaryFormatter (Items Count: {referenceObjects.Length})--------------------");
                bf.SurrogateSelector = surrogate;
                byte[] raw = SerializeObjects(referenceObjects, bf);
                bf.SurrogateSelector = null;
                object[] result = DeserializeObjects(raw, bf);
                AssertItemsEqual(referenceObjects, result);

                Console.WriteLine($"------------------KGy SOFT BinarySerializer (Items Count: {referenceObjects.Length}; Options: {bsf.Options})--------------------");
                bsf.SurrogateSelector = surrogate;
                raw = SerializeObjects(referenceObjects, bsf);
                bsf.SurrogateSelector = null;
                result = DeserializeObjects(raw, bsf);
                AssertItemsEqual(referenceObjects, result);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
#endif

        [Test]
        public void SerializationBinderTest()
        {
            object[] referenceObjects =
            {
                1, // primitive type
                new StringBuilder("1"), // natively supported by KGySoft only
                new List<int> { 1 }, // generic, natively supported for KGySoft only, in mscorlib
                new HashSet<int> { 1 }, // generic, natively supported for KGySoft only, in core
                TestEnumByte.One, // non standard assembly
                new CustomGenericCollection<TestEnumByte> { TestEnumByte.One, TestEnumByte.Two },
                new CustomGenericDictionary<TestEnumByte, CustomSerializedClass> { { TestEnumByte.One, new CustomSerializedClass { Name = "alpha" } } },
                new StringKeyedDictionary<int> { { "alpha", 1 }, { "beta", 2 } },
                new CustomSerializedSealedClass("1"), // type is changed on serialization: System BinaryFormatter fails: the binder gets the original type instead of the changed one

                typeof(List<int>), // supported generic
                typeof(CustomGenericCollection<CustomSerializedClass>), // custom generic

                typeof(List<>), // supported generic type definition
                typeof(Dictionary<,>), // supported generic type definition
                typeof(CustomGenericCollection<>), // custom generic type definition

                typeof(List<>).GetGenericArguments()[0], // supported generic type definition argument
                typeof(CustomGenericCollection<>).GetGenericArguments()[0], // custom generic type definition argument

                typeof(OpenGenericDictionary<>).BaseType, // open constructed generic (Dictionary<string, TValue>)
                typeof(Nullable<>).MakeGenericType(typeof(KeyValuePair<,>)), // open constructed generic (KeyValuePair<,>?)

                typeof(Array).GetMethod(nameof(Array.Resize)).GetGenericArguments()[0], // T of Array.Resize, unique generic method definition argument
            };

            // default
#if !NETCOREAPP // types are not serializable in .NET Core
            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            // by TestSerializationBinder
            string title = "Serialization and Deserialization with TestSerializationBinder";
            SerializationBinder binder = new TestSerializationBinder();
#if !(NET35 || NETCOREAPP)
            SystemSerializeObject(referenceObjects, title, binder: binder);
            SystemSerializeObjects(referenceObjects, title, binder: binder);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, binder: binder);

#if NETCOREAPP
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, true, binder); // safeCompare: the cloned runtime types are not equal
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, true, binder); // safeCompare: the cloned runtime types are not equal
#else
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder: binder);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames, title, binder: binder);
        }

        [Test]
        public void WeakAssemblySerializationBinderTest()
        {
            object[] referenceObjects =
            {
                1, // primitive type
                new StringBuilder("1"), // natively supported by KGySoft only
                new List<int> { 1 }, // generic, natively supported for KGySoft only, in mscorlib
                new HashSet<int> { 1 }, // generic, natively supported for KGySoft only, in core
                TestEnumByte.One, // non standard assembly
                new CustomGenericCollection<TestEnumByte> { TestEnumByte.One, TestEnumByte.Two },
                new CustomGenericDictionary<TestEnumByte, CustomSerializedClass> { { TestEnumByte.One, new CustomSerializedClass { Name = "alpha" } } },
                new CustomSerializedSealedClass("1"), // type is changed on serialization: System BinaryFormatter fails: the binder gets the original type instead of the changed one
                new StringKeyedDictionary<int> { { "alpha", 1 }, { "beta", 2 } },

                typeof(List<int>), // supported generic
                typeof(CustomGenericCollection<CustomSerializedClass>), // custom generic

                typeof(List<>), // supported generic type definition
                typeof(Dictionary<,>), // supported generic type definition
                typeof(CustomGenericCollection<>), // custom generic type definition

                typeof(List<>).GetGenericArguments()[0], // supported generic type definition argument
                typeof(CustomGenericCollection<>).GetGenericArguments()[0], // custom generic type definition argument

                typeof(OpenGenericDictionary<>).BaseType, // open constructed generic (Dictionary<string, TValue>)
                typeof(Nullable<>).MakeGenericType(typeof(KeyValuePair<,>)), // open constructed generic (KeyValuePair<,>?)
            };

            // by WeakAssemblySerializationBinder
            string title = "Deserialization with WeakAssemblySerializationBinder";
            var binder = new WeakAssemblySerializationBinder();
#if !NETCOREAPP // types are not serializable in .NET Core
            SystemSerializeObject(referenceObjects, title, binder: binder);
            SystemSerializeObjects(referenceObjects, title, binder: binder); // The constructor to deserialize an object of type 'System.RuntimeType' was not found.  
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, binder: binder);

            // by WeakAssemblySerializationBinder, including serialization
            title = "Serialization and Deserialization with WeakAssemblySerializationBinder, omitting assembly name";
            binder.OmitAssemblyNameOnSerialize = true;

#if !NETCOREAPP // types are not serializable in .NET Core
            SystemSerializeObject(referenceObjects, title, binder: binder); // ignores OmitAssemblyNameOnSerialize in .NET 3.5 but works
            SystemSerializeObjects(referenceObjects, title, binder: binder); // ignores OmitAssemblyNameOnSerialize in .NET 3.5 but works
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, binder: binder);

#if NETCOREAPP
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, true, binder); // safeCompare: the cloned runtime types are not equal
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, true, binder); // safeCompare: the cloned runtime types are not equal
#else
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder: binder);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames, title, binder: binder);
        }

        [Test]
        public void SerializationSurrogateTest()
        {
            object[] referenceObjects =
            {
                // simple types
                new object(),
                DBNull.Value,
                true,
                (sbyte)1,
                (byte)1,
                (short)1,
                (ushort)1,
                (int)1,
                (uint)1,
                (long)1,
                (ulong)1,
                'a',
                "alpha",
                (float)1,
                (double)1,
                (decimal)1,
                DateTime.UtcNow,
                DateTime.Now,
                new IntPtr(1),
                new UIntPtr(1),
                new Version(1, 2, 3, 4),
                new Guid("ca761232ed4211cebacd00aa0057b223"),
                new TimeSpan(1, 1, 1),
                new DateTimeOffset(DateTime.Now),
                new DateTimeOffset(DateTime.UtcNow),
                new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)),
                new Uri(@"x:\teszt"),
                new DictionaryEntry(1, "alpha"),
                new KeyValuePair<int, string>(1, "alpha"),
                new BitArray(new[] { true, false, true }),
                new StringBuilder("alpha"),

                TestEnumByte.Two,
                new KeyValuePair<int, object>[] { new KeyValuePair<int, object>(1, "alpha"), new KeyValuePair<int, object>(2, new TestEnumByte[] { TestEnumByte.One, TestEnumByte.Two }), },

#if !NET35
                new BigInteger(1),
#endif

#if NETCOREAPP3_0_OR_GREATER
#if !NETSTANDARD_TEST
                new Rune('a'),  
#endif
                new Index(1),
                new Range(1, 2),
#endif

#if NET5_0_OR_GREATER
                (Half)1,
#endif

#if NET6_0_OR_GREATER
                DateOnly.FromDateTime(DateTime.Today),
                TimeOnly.FromDateTime(DateTime.Now),
#endif

                // dictionary with any object key and read-only collection value
                new Dictionary<object, ReadOnlyCollection<int>> { { 1, new ReadOnlyCollection<int>(new[] { 1, 2 }) }, { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, null } },

                // nested default recursion
                new Collection<SystemSerializableClass> { new SystemSerializableClass { Bool = null, IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { Bool = true, IntProp = 2, StringProp = "beta" }, null },
                new CustomSerializedClass { Bool = false, Name = "gamma" },

                new CustomGenericCollection<TestEnumByte> { TestEnumByte.One, TestEnumByte.Two },
                new CustomGenericDictionary<TestEnumByte, CustomSerializedClass> { { TestEnumByte.One, new CustomSerializedClass { Name = "alpha" } } },

                new StringKeyedDictionary<int> { { "alpha", 1 }, { "beta", 2 } },

                // nullable arrays
                new BinarySerializableStruct?[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, null },
                new SystemSerializableStruct?[] { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, null },

                // lists with binary serializable elements
                new List<BinarySerializableStruct> { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, default(BinarySerializableStruct) },
                new List<BinarySerializableStruct?> { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, default(BinarySerializableStruct?) },
                new List<BinarySerializableClass> { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new BinarySerializableSealedClass(2, "beta"), null },
                new List<BinarySerializableSealedClass> { new BinarySerializableSealedClass(1, "alpha"), null },
                new List<object> { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new BinarySerializableSealedClass(2, "beta"), new BinarySerializableStruct { IntProp = 3, StringProp = "gamma" }, null },

                // lists with default recursive elements
                new List<SystemSerializableStruct> { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(SystemSerializableStruct) },
                new List<SystemSerializableStruct?> { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(SystemSerializableStruct?) },
                new List<SystemSerializableClass> { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { IntProp = 2, StringProp = "beta" }, null },
                new List<SystemSerializableSealedClass> { new SystemSerializableSealedClass { IntProp = 1, StringProp = "alpha" }, null },

                // lists with custom recursive elements
                new List<CustomSerializableStruct> { new CustomSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(CustomSerializableStruct) },
                new List<CustomSerializableStruct?> { new CustomSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(CustomSerializableStruct?) },
                new List<CustomSerializedClass> { new CustomSerializedClass { Name = "alpha", Bool = true }, new CustomSerializedSealedClass("beta") { Bool = null }, null },
                new List<CustomSerializedSealedClass> { new CustomSerializedSealedClass("alpha") { Bool = false }, null },

                // collections with native support
                new CircularList<int> { 1, 2, 3 },
#if !NET35
                new SortedSet<int> { 1, 2, 3 },

                //new ConcurrentBag<int>(new[] { 1, 2, 3 }), // SerializationException : The serialization surrogate has changed the reference of the result object, which prevented resolving circular references to itself. Object type: System.Threading.ThreadLocal`1+LinkedSlot[System.Collections.Concurrent.ConcurrentBag`1+WorkStealingQueue[System.Int32]]
                new ConcurrentQueue<int>(new[] { 1, 2, 3 }),
                new ConcurrentStack<int>(new[] { 1, 2, 3 }),

                new ConcurrentDictionary<int, string>(new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" } }),
#endif

                new CircularSortedList<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } },

                // Pointer fields
                // new UnsafeStruct(), - TestSurrogateSelector calls Reflector.SetField now
            };

            // default
            SystemSerializeObjects(referenceObjects); // system serialization fails: IBinarySerializable, Rune, etc. is not serializable
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            ISurrogateSelector selector = new TestSurrogateSelector();
            string title = nameof(TestSurrogateSelector);
            SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector); // system deserialization fails: Invalid BinaryFormatter stream.
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.TryUseSurrogateSelectorForAnyType, title, surrogateSelector: selector);

            selector = new TestCloningSurrogateSelector();
            title = nameof(TestCloningSurrogateSelector);
            SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.TryUseSurrogateSelectorForAnyType, title, surrogateSelector: selector);
        }

        [Test]
        public void NameInvariantSurrogateSelectorTest()
        {
            object[] referenceObjects =
            {
                // simple types
                new object(),
                DBNull.Value,
                true,
                (sbyte)1,
                (byte)1,
                (short)1,
                (ushort)1,
                (int)1,
                (uint)1,
                (long)1,
                (ulong)1,
                'a',
                "alpha",
                (float)1,
                (double)1,
                (decimal)1,
                DateTime.UtcNow,
                DateTime.Now,
                new IntPtr(1),
                new UIntPtr(1),
                new Version(1, 2, 3, 4),
                new Guid("ca761232ed4211cebacd00aa0057b223"),
                new TimeSpan(1, 1, 1),
                new DateTimeOffset(DateTime.Now),
                new DateTimeOffset(DateTime.UtcNow),
                new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)),
                new Uri(@"x:\teszt"),
                new DictionaryEntry(1, "alpha"),
                new KeyValuePair<int, string>(1, "alpha"),
                new BitArray(new[] { true, false, true }),
                new StringBuilder("alpha"),
#if !NETCOREAPP // works but Equals fails on the clone
                typeof(int),
#endif

                TestEnumByte.Two,
                new KeyValuePair<int, object>[] { new KeyValuePair<int, object>(1, "alpha"), new KeyValuePair<int, object>(2, new TestEnumByte[] { TestEnumByte.One, TestEnumByte.Two }), },

                // dictionary with any object key and read-only collection value
                new Dictionary<object, ReadOnlyCollection<int>> { { 1, new ReadOnlyCollection<int>(new[] { 1, 2 }) }, { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, null } },

                // nested default recursion
                new Collection<SystemSerializableClass> { new SystemSerializableClass { Bool = null, IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { Bool = true, IntProp = 2, StringProp = "beta" }, null },
                new CustomSerializedClass { Bool = false, Name = "gamma" },

                new CustomGenericCollection<TestEnumByte> { TestEnumByte.One, TestEnumByte.Two },
                new CustomGenericDictionary<TestEnumByte, CustomSerializedClass> { { TestEnumByte.One, new CustomSerializedClass { Name = "alpha" } } },

                new StringKeyedDictionary<int> { { "alpha", 1 }, { "beta", 2 } },

                // nullable arrays
                new BinarySerializableStruct?[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, null },
                new SystemSerializableStruct?[] { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, null },

                // lists with binary serializable elements
                new List<BinarySerializableStruct> { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, default(BinarySerializableStruct) },
                new List<BinarySerializableStruct?> { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, default(BinarySerializableStruct?) },
                new List<BinarySerializableClass> { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new BinarySerializableSealedClass(2, "beta"), null },
                new List<BinarySerializableSealedClass> { new BinarySerializableSealedClass(1, "alpha"), null },
                new List<IBinarySerializable> { new BinarySerializableClass { IntProp = 1, StringProp = "alpha" }, new BinarySerializableSealedClass(2, "beta"), new BinarySerializableStruct { IntProp = 3, StringProp = "gamma" }, null },

                // lists with default recursive elements
                new List<SystemSerializableStruct> { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(SystemSerializableStruct) },
                new List<SystemSerializableStruct?> { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(SystemSerializableStruct?) },
                new List<SystemSerializableClass> { new SystemSerializableClass { IntProp = 1, StringProp = "alpha" }, new SystemSerializableSealedClass { IntProp = 2, StringProp = "beta" }, null },
                new List<SystemSerializableSealedClass> { new SystemSerializableSealedClass { IntProp = 1, StringProp = "alpha" }, null },

                // lists with custom recursive elements
                new List<CustomSerializableStruct> { new CustomSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(CustomSerializableStruct) },
                new List<CustomSerializableStruct?> { new CustomSerializableStruct { IntProp = 1, StringProp = "alpha" }, default(CustomSerializableStruct?) },
                new List<CustomSerializedClass> { new CustomSerializedClass { Name = "alpha", Bool = true }, new CustomSerializedSealedClass("beta") { Bool = null }, null },
                new List<CustomSerializedSealedClass> { new CustomSerializedSealedClass("alpha") { Bool = false }, null },

                // collections with native support
                new CircularList<int> { 1, 2, 3 },
#if !NET35
                new SortedSet<int> { 1, 2, 3 },
#endif

                new CircularSortedList<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } },

                // Pointer fields
                new UnsafeStruct(),
            };

            ISurrogateSelector selector = new NameInvariantSurrogateSelector();
            string title = nameof(NameInvariantSurrogateSelector);
            //SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector); // System.MemberAccessException: Cannot create an abstract class.
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.TryUseSurrogateSelectorForAnyType, title, surrogateSelector: selector);
        }

        [Test]
        public void CustomSerializerSurrogateSelectorTest()
        {
            var referenceObjects = new List<object>
            {
                // natively supported types
                1,
                "alpha",

                // can be forced to use surrogate selector
                new List<int> { 1 },

                // custom serializable types
                new CustomSerializedClass { Bool = true, Name = nameof(CustomSerializedClass) },
                new SerializationEventsClass { Name = nameof(SerializationEventsClass), },

                // non serializable types
                new BitVector32(13),
                new NonSerializableClass { IntProp = 13, StringProp = "alpha"}, 

                // not serializable in .NET Core but otherwise they are compatible
                new MemoryStream(new byte[] { 1, 2, 3 }),
                new Collection<Encoding> { Encoding.ASCII, Encoding.Unicode },

                // pointer arrays
                new UnsafeStruct(),
            };

            var selector = new CustomSerializerSurrogateSelector();
            string title = "Default settings";

            SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector);

            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.TryUseSurrogateSelectorForAnyType, title, surrogateSelector: selector);

            title = "Forcing field-based serialization";
            referenceObjects.AddRange(new object[]
            {
                // Type is not serializable in .NET Core but in .NET Core 2 it still implements ISerializable throwing PlatformNotSupportedException
                typeof(List<int>),
                typeof(List<>),
                typeof(List<>).GetGenericArguments()[0],
            });
            selector.IgnoreISerializable = true;
            selector.IgnoreNonSerializedAttribute = true;

            SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector, safeCompare: true);

            KGySerializeObjects(referenceObjects, BinarySerializationOptions.IgnoreSerializationMethods, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.IgnoreSerializationMethods | BinarySerializationOptions.TryUseSurrogateSelectorForAnyType,
                title, surrogateSelector: selector, safeCompare: true); // safe: Types
        }

        [Test]
        public void SerializeSameValues()
        {
            object one = 1;
            string s1 = "alpha";
            string s2 = String.Format("{0}{1}", "al", "pha");
            SystemSerializableClass tc = new SystemSerializableClass { IntProp = 10, StringProp = "s1" };
            object ts = new SystemSerializableStruct { IntProp = 10, StringProp = "s1" };
            object[] referenceObjects =
            {
                // *: Id is generated on system serialization
                new object[] { 1, 2, 3 }, // different objects
                new object[] { 1, 1, 1 }, // same values but different instances
                new object[] { one, one, one }, // same value type boxed reference
                new object[] { s1, s1 }, // same references*
                new object[] { s1, s2 }, // different references but same values
                new string[] { s1, s1 }, // same references*
                new string[] { s1, s2 }, // different references but same values
                new SystemSerializableClass[] { tc }, // custom class, single instance
                new SystemSerializableClass[] { tc, tc, tc, tc }, // custom class, multiple instances*
                new SystemSerializableStruct[] { (SystemSerializableStruct)ts }, // custom struct, single instance
                new SystemSerializableStruct[] { (SystemSerializableStruct)ts, (SystemSerializableStruct)ts, (SystemSerializableStruct)ts, (SystemSerializableStruct)ts }, // custom struct, double instances*
                new object[] { ts }, // custom struct, boxed single instance
                new object[] { ts, ts, ts, ts }, // custom struct, boxed double instances*
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
        }

        [Test]
        public void SerializeCircularReferences()
        {
            object[] referenceObjects =
            {
                new CircularReferenceClass { Name = "Single" }, // no circular reference
                new CircularReferenceClass { Name = "Parent" }.AddChild("Child").AddChild("Grandchild").Parent.Parent, // circular reference, but logically alright
                new SelfReferencerDirect("Direct"),
                new SelfReferencerIndirect("Default") { UseCustomDeserializer = false, UseValidWay = true }, // circular reference deserialized by IObjectReference default object graph
                new SelfReferencerIndirect("Custom") { UseCustomDeserializer = true, UseValidWay = true }, // circular reference deserialized by IObjectReference custom object graph
#if NETFRAMEWORK // PlatformNotSupportedException : Operation is not supported on this platform.
                Encoding.GetEncoding("shift_jis"), // circular reference deserialized by IObjectReference custom object graph
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            var root = new CircularReferenceClass { Name = "root" }.AddChild("child").AddChild("grandchild").Parent.Parent;
            root.Children[0].Children[0].Children.Add(root);
            referenceObjects = new object[]
            {
                root, // grand-grandchild is root again
                null, // placeholder: DictionaryEntry referencing the referenceObjects and thus itself
                null, // placeholder: KeyValuePair referencing the referenceObjects and thus itself
            };
            referenceObjects[1] = new DictionaryEntry(1, referenceObjects);
            referenceObjects[2] = new KeyValuePair<int, object>(1, referenceObjects);

            SystemSerializeObject(referenceObjects, safeCompare: true);
            SystemSerializeObjects(referenceObjects, safeCompare: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, safeCompare: true);

            referenceObjects = new object[]
            {
                new SelfReferencerIndirect("Default") { UseCustomDeserializer = false, UseValidWay = false },
                new SelfReferencerIndirect("Custom") { UseCustomDeserializer = true, UseValidWay = false },
                new SelfReferencerInvalid("Default") { UseCustomDeserializer = false },
                new SelfReferencerInvalid("Custom") { UseCustomDeserializer = true },
            };

            foreach (object referenceObject in referenceObjects)
                SystemSerializeObject(referenceObject);

            foreach (object referenceObject in referenceObjects)
                Throws<SerializationException>(() => KGySerializeObject(referenceObject, BinarySerializationOptions.None),
                    "Deserialization of an IObjectReference instance has an unresolvable circular reference to itself.");
        }

        [Test]
        public void SerializeCircularReferencesBySurrogateSelector()
        {
            string title = "Valid cases using a non-replacing selector";
            var selector = new TestSurrogateSelector();
            object[] referenceObjects =
            {
                new CircularReferenceClass { Name = "Single" }, // no circular reference
                new CircularReferenceClass { Name = "Parent" }.AddChild("Child").AddChild("Grandchild").Parent.Parent, // circular reference, but logically alright
                new SelfReferencerDirect("Direct"),
                new SelfReferencerIndirect("Default") { UseCustomDeserializer = false, UseValidWay = true }, // circular reference deserialized by IObjectReference default object graph
                new SelfReferencerIndirect("Custom") { UseCustomDeserializer = true, UseValidWay = true }, // circular reference deserialized by IObjectReference custom object graph
#if NETFRAMEWORK // DBCSCodePageEncoding has pointer fields and TestSurrogateSelector uses Reflector.SetField
                Encoding.GetEncoding("shift_jis"), // circular reference deserialized by IObjectReference custom object graph  
#endif
                new SelfReferencerIndirect("Default") { UseCustomDeserializer = false, UseValidWay = false }, // would not work without the surrogate
                new SelfReferencerIndirect("Custom") { UseCustomDeserializer = true, UseValidWay = false }, // would not work without the surrogate
                new SelfReferencerInvalid("Default") { UseCustomDeserializer = false }, // would not work without the surrogate
                new SelfReferencerInvalid("Custom") { UseCustomDeserializer = true }, // would not work without the surrogate
            };

            //SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector); - SelfReferencerDirect: SerializationException: The object with ID 3 was referenced in a fixup but does not exist.
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.IgnoreSerializationMethods, // OrderedDictionary.OnDeserialization
                title, surrogateSelector: selector);

            title = "Valid cases using a replacing selector";
            selector = new TestCloningSurrogateSelector();
            referenceObjects = new object[]
            {
                new CircularReferenceClass { Name = "Single" }, // no circular reference
            };

            SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, surrogateSelector: selector);

            title = "Invalid cases using a replacing selector";
            referenceObjects = new object[]
            {
                // The first case actually works by BinaryFormatter because it orders the surrogate-deserialized objects in a way that every reference can be resolved
                // BinarySerializationFormatter has a strict traversal order but we can detect if replacing causes problems
                new CircularReferenceClass { Name = "Parent" }.AddChild("Child").AddChild("Grandchild").Parent.Parent,
                new SelfReferencerDirect("Direct"),
                new SelfReferencerIndirect("Default") { UseCustomDeserializer = false, UseValidWay = true },
                new SelfReferencerIndirect("Custom") { UseCustomDeserializer = true, UseValidWay = true },
#if NETFRAMEWORK // DBCSCodePageEncoding has pointer fields and TestSurrogateSelector uses Reflector.SetField
                Encoding.GetEncoding("shift_jis"),
#endif
                new SelfReferencerIndirect("Default") { UseCustomDeserializer = false, UseValidWay = false },
                new SelfReferencerIndirect("Custom") { UseCustomDeserializer = true, UseValidWay = false },
                new SelfReferencerInvalid("Default") { UseCustomDeserializer = false },
                new SelfReferencerInvalid("Custom") { UseCustomDeserializer = true },
            };

            foreach (object referenceObject in referenceObjects)
                SystemSerializeObject(referenceObject, title, surrogateSelector: selector);
            foreach (object referenceObject in referenceObjects)
                Throws<SerializationException>(() => KGySerializeObject(referenceObject, BinarySerializationOptions.None, title, surrogateSelector: selector),
                    "The serialization surrogate has changed the reference of the result object, which prevented resolving circular references to itself.");
        }

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical]
        public void SerializationFromPartiallyTrustedDomain()
        {
            var domain = CreateSandboxDomain(
#if NET35
                new EnvironmentPermission(PermissionState.Unrestricted),
#endif
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess),
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlAppDomain | SecurityPermissionFlag.SerializationFormatter | SecurityPermissionFlag.UnmanagedCode | SecurityPermissionFlag.ControlPolicy),
                new FileIOPermission(PermissionState.Unrestricted));
            var handle = Activator.CreateInstance(domain, Assembly.GetExecutingAssembly().FullName, typeof(Sandbox).FullName);
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

        [Test]
        public unsafe void SerializePointers()
        {
            object[] referenceObjects =
            {
                // Pointer fields
                new UnsafeStruct(),
                new UnsafeStruct
                {
                    VoidPointer = (void*)new IntPtr(1),
                    IntPointer = (int*)new IntPtr(1),
                    StructPointer = (Point*)new IntPtr(1),
                    PointerArray = null, // new int*[] { (int*)new IntPtr(1), null }, - not supported
                    PointerOfPointer = (void**)new IntPtr(1)
                },
            };

            SystemSerializeObject(referenceObjects, safeCompare: true);
            SystemSerializeObjects(referenceObjects, safeCompare: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            referenceObjects = new object[]
            {
                // Pointer Array
                new int*[] { (int*)IntPtr.Zero },
            };

            //SystemSerializeObject(referenceObjects, safeCompare: true); // InvalidCastException: Unable to cast object of type 'System.Void*[]' to type 'System.Object[]'.
            //SystemSerializeObjects(referenceObjects, safeCompare: true);

            Throws<NotSupportedException>(() => KGySerializeObject(referenceObjects, BinarySerializationOptions.None), "Array of pointer type 'System.Int32*[]' is not supported.");
            Throws<NotSupportedException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.None), "Array of pointer type 'System.Int32*[]' is not supported.");
        }

        [TestCase(typeof(bool))]
        [TestCase(typeof(int))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(LargeUnmanagedStruct))]
        [TestCase(typeof(LargeStructToBeMarshaled))]
        [TestCase(typeof(KeyValuePair<int, int>))]
        [TestCase(typeof(ValueTuple<int, int>))]
        public void SerializeValueTypeNonGenericTest(Type type)
        {
            var settings = new GenerateObjectSettings
            {
                ObjectInitialization = ObjectInitialization.Fields,
                CollectionsLength = new(16, 16),
                StringsLength = new(16, 16)
            };

            object instance = ThreadSafeRandom.Instance.NextObject(type, settings);
            byte[] serialized = BinarySerializer.SerializeValueType((ValueType)instance);
            object deserialized = BinarySerializer.DeserializeValueType(type, serialized);
            byte[] reserialized = BinarySerializer.SerializeValueType((ValueType)deserialized);
            CollectionAssert.AreEqual(serialized, reserialized);
        }

        [TestCaseGeneric(TypeArguments = new[] { typeof(bool) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(int) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(decimal) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(LargeUnmanagedStruct) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(KeyValuePair<int, int>) })] // tricky: not unmanaged, still, it works
        [TestCaseGeneric(TypeArguments = new[] { typeof(ValueTuple<int, int>) })] // tricky: not unmanaged, still, it works
        public void SerializeValueTypeGenericTest<T>()
            where T : unmanaged
        {
            var settings = new GenerateObjectSettings
            {
                ObjectInitialization = ObjectInitialization.Fields,
            };

            T instance = ThreadSafeRandom.Instance.NextObject<T>(settings);
            byte[] serialized = BinarySerializer.SerializeValueType(instance);
            T deserialized = BinarySerializer.DeserializeValueType<T>(serialized);
            byte[] reserialized = BinarySerializer.SerializeValueType(deserialized);
            CollectionAssert.AreEqual(serialized, reserialized);
        }

        [TestCaseGeneric(true, TypeArguments = new[] { typeof(bool) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(int) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(decimal) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(LargeUnmanagedStruct) })]
        [TestCaseGeneric(false, TypeArguments = new[] { typeof(LargeStructToBeMarshaled) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(KeyValuePair<int, int>) })]
        [TestCaseGeneric(false, TypeArguments = new[] { typeof(KeyValuePair<int, string>) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(ValueTuple<int, int>) })]
        public void TrySerializeValueTypeGenericTest<T>(bool expectedResult)
            where T : unmanaged
        {
            var settings = new GenerateObjectSettings
            {
                ObjectInitialization = ObjectInitialization.Fields,
            };

            T instance = ThreadSafeRandom.Instance.NextObject<T>(settings);
            bool retValue = BinarySerializer.TrySerializeValueType(instance, out byte[] result);
            Assert.AreEqual(expectedResult, retValue);
            Assert.AreEqual(expectedResult, result != null);
        }

        [TestCaseGeneric(TypeArguments = new[] { typeof(bool) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(int) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(decimal) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(LargeUnmanagedStruct) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(KeyValuePair<int, int>) })] // tricky: not unmanaged, still, it works
        [TestCaseGeneric(TypeArguments = new[] { typeof(ValueTuple<int, int>) })] // tricky: not unmanaged, still, it works
        public void SerializeValueArrayGenericTest<T>()
            where T : unmanaged
        {
            var settings = new GenerateObjectSettings
            {
                ObjectInitialization = ObjectInitialization.Fields,
            };

            T[] instance = ThreadSafeRandom.Instance.NextObject<T[]>(settings);
            byte[] serialized = BinarySerializer.SerializeValueArray(instance);
            T[] deserialized = BinarySerializer.DeserializeValueArray<T>(serialized, 0, instance.Length);
            byte[] reserialized = BinarySerializer.SerializeValueArray(deserialized);
            CollectionAssert.AreEqual(serialized, reserialized);
        }

        [Test]
        public void SerializeISerializableSetNonExistingType()
        {
            var obj = new GetObjectDataSetsUnknownType();
            var binder = new CustomSerializationBinder
            {
                TypeResolver = (asmName, typeName) =>
                {
                    Console.WriteLine($"asmName={asmName ?? "null"}");
                    Console.WriteLine($"typeName={typeName ?? "null"}");
                    return typeof(GetObjectDataSetsUnknownType);
                }
            };

            SystemSerializeObject(obj, binder: binder);
            KGySerializeObject(obj, BinarySerializationOptions.None, binder: binder);
        }

        [Test]
        public void SerializeGetObjectDataSetsInvalidType()
        {
            var obj = new GetObjectDataSetsInvalidType();
            var binder = new CustomSerializationBinder
            {
                TypeResolver = (asmName, typeName) =>
                {
                    Console.WriteLine($"asmName={asmName ?? "null"}");
                    Console.WriteLine($"typeName={typeName ?? "null"}");
                    return typeof(GetObjectDataSetsInvalidType);
                }
            };

            SystemSerializeObject(obj, binder: binder); // 
            KGySerializeObject(obj, BinarySerializationOptions.None, binder: binder);
        }

        [Test]
        public void SerializeUsingSameObjectReferenceWithDifferentNames()
        {
            object[] referenceObjects =
            {
                Singleton1.Instance,
                Singleton2.Instance,
                Singleton3.Instance,
                new[] { Singleton1.Instance },
                new[] { Singleton2.Instance },
                new[] { Singleton3.Instance },
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
        }

        [Test]
        public void NullReferenceSerializerTest()
        {
            var referenceObject = new NullReference();

            BinaryFormatter bf = new BinaryFormatter();
            BinarySerializationFormatter bsf = new BinarySerializationFormatter(BinarySerializationOptions.RecursiveSerializationAsFallback);

            Console.WriteLine("------------------System BinaryFormatter--------------------");
            byte[] raw = SerializeObject(referenceObject, bf);
            object result = DeserializeObject(raw, bf);
            Assert.IsNull(result);

            Console.WriteLine($"------------------KGy SOFT BinarySerializer--------------------");
            raw = SerializeObject(referenceObject, bsf);
            result = DeserializeObject(raw, bsf);
            Assert.IsNull(result);
        }

        [Test]
        public void SerializeForwardedTypes()
        {
            object[] referenceObjects =
            {
#if !NET35
		        new ObservableCollection<int> { 1, 2, 3 }, // WindowsBase -> System/System.ObjectModel  
#endif
                TimeSpan.MaxValue, // mscorlib -> System.Private.CorLib (missing attribute)
#if !NETCOREAPP2_0 // not serializable in .NET Core 2
                DBNull.Value, // mscorlib -> System.Private.CorLib via UnitySerializationHolder (missing attribute)  
#endif
                new BitArray(new[] { 1 }), // mscorlib -> System.Collections
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
                // Only for HashSet<T> and .NET Core 2.x: typeof(IEqualityComparer<T>.IsAssignableFrom(comparer)) fails in HashSet.OnDeserialization. No idea why, and no idea why the same logic works for Dictionary.
                new HashSet<int> { 1, 2, 3 }, // System.Core -> System.Collections  
#endif
                new LinkedList<int>(new[] { 1, 2, 3 }), // System -> System.Collections
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.IgnoreTypeForwardedFromAttribute);
        }

        [Test]
        public void SafeModeNonSerializableTest()
        {
            object[] referenceObjects =
            {
                new NonSerializableClass { IntProp = 42 }
            };

            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode));
        }

        [Test]
        public void SafeModeAssemblyLoadingTest()
        {
#if NET35
            string asmName = "System.Design, Version=2.0.0.0, PublicKeyToken=b77a5c561934e089"; 
            string typeName = "System.Windows.Forms.Design.Behavior.SnapLineType";
#else
            string asmName = "System.Numerics, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089";
            string typeName = "System.Numerics.BigInteger";
#endif

            if (Reflector.ResolveAssembly(asmName, ResolveAssemblyOptions.AllowPartialMatch) != null)
            {
                Assert.Inconclusive($"Assembly {asmName} is already loaded, test is ignored. Try to run this test alone.");
                return;
            }

            // using a proxy type and a binder to serialize a type information that is not already loaded (content is not relevant)
            Type proxyType = typeof(SystemSerializableClass);
            object[] referenceObjects =
            {
                Reflector.CreateInstance(proxyType)
            };

            // only serialization way is set
            var binder = new CustomSerializationBinder()
            {
                AssemblyNameResolver = t => t == proxyType ? asmName : null,
                TypeNameResolver = t => t == proxyType ? typeName : null
            };

            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, binder: binder), "Cannot resolve assembly in safe mode");
        }

#if NETFRAMEWORK // starting with .NET Core it is not part of the core framework anymore
        [Test]
        public void SafeModeDeleteAttackTest()
        {
            using var obj = new TempFileCollection();
            obj.AddFile("VeryImportantSystemFile", false);
            object[] referenceObjects =
            {
                obj
            };

            // As TempFileCollection is serializable in .NET Framework, it can be deserialized without fallback options
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            // But throws an exception in SafeMode
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode));
        }
#endif

#if !NET35
        [Test]
        public void SafeModeDoSAttackTest()
        {
            // Exploit: using a StructuralEqualityComparer with hopelessly complex hash computing
            // A Hashtable can accept such a non-generic comparer. We add it to the hashtable _before_ making it too complex.
            // On .NET Core and above this is safe with BinaryFormatter because StructuralEqualityComparer is not serializable anymore
            var key = new object[2];
            var obj = new Hashtable(StructuralComparisons.StructuralEqualityComparer) { { key, null } };

            // now doing the complications... - actually this makes it impossible to find it (as the hash changes) but it's not important
            var s1 = key;
            var s2 = new object[2];
            for (int i = 0; i < 50; i++)
            {
                var t1 = new object[2];
                var t2 = new object[2];
                s1[0] = t1;
                s1[1] = t2;
                s2[0] = t1;
                s2[1] = t2;
                s1 = t1;
                s2 = t2;
            }

            object[] referenceObjects =
            {
                obj
            };

            // SystemSerializeObjects(referenceObjects); // on .NET Framework this lasts forever

            // In SafeMode this cannot be deserialized even in .NET Framework
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode));

            // But actually it can be deserialized without any problem if ignoring the ISerializable implementation of the Hashtable
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.IgnoreISerializable, safeCompare: true);
        }

        [Test]
        public void SafeModeStackOverflowAttackTest()
        {
            // similar to the previous one
            var key = new object[1];
            var comp = StructuralComparisons.StructuralEqualityComparer;
            var obj = new Hashtable(comp);
            obj.Add(key, null);
            key[0] = key;

            object[] referenceObjects =
            {
                obj
            };

            // SystemSerializeObjects(referenceObjects); // on .NET Framework this causes StackOverflowException

            // In SafeMode this cannot be deserialized even in .NET Framework
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode));

            // But actually it can be deserialized without any problem if ignoring the ISerializable implementation of the Hashtable
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.IgnoreISerializable, safeCompare: true);
        }
#endif

        [Test]
        public void SafeModeArrayOutOfMemoryAttackTest()
        {
            byte[] array = { 1, 2, 3, 4, 5, 6, 7 };
            var bsf = new BinarySerializationFormatter();
            var serData = new List<byte>(SerializeObject(array, bsf));

            // 7 bit encoded length is at offset 3. Inserting 4 FF values will be decoded as MaxInt
            // 00000000 ushort: 388(0184[UInt8 | Extended | Array]) - WriteDataType < WriteRootCollection < WriteRoot
            // 00000002 byte: 0 (00) - WriteTypeNamesAndRanks < WriteRootCollection < WriteRoot
            // 00000003 byte: 7 (07) - Write7BitInt < WriteCollection < WriteRootCollection < WriteRoot
            // 00000004 7 bytes: 1, 2, 3, 4, 5, 6, 7 (01, 02, 03, 04, 05, 06, 07) - WriteCollection < WriteRootCollection < WriteRoot
            for (int i = 0; i < 4; i++)
                serData.Insert(3, 255);
            byte[] manipulatedData = serData.ToArray();

            // without safe mode a huge array is about to be allocated
            Throws<OutOfMemoryException>(() => DeserializeObject(manipulatedData, bsf));

            // in SafeMode the array is allocated in chunks and the stream simply ends unexpectedly
            bsf.Options = BinarySerializationOptions.SafeMode;
            Throws<SerializationException>(() => DeserializeObject(manipulatedData, bsf));
        }

        [Test]
        public void SafeModeListCapacityOutOfMemoryAttackTest()
        {
            var list = new List<byte> { 1, 2, 3 };
            var bsf = new BinarySerializationFormatter();
            var serData = new List<byte>(SerializeObject(list, bsf));

            // 7 bit encoded capacity is at offset 3. 07 + 4xFF will be decoded as MaxInt
            // 00000000 ushort: 644 (0284 [UInt8 | Extended | List]) - WriteDataType < WriteRootCollection < WriteRoot
            // 00000002 byte: 3 (03) - Write7BitInt < WriteSpecificProperties < WriteCollection < WriteRootCollection < WriteRoot
            // 00000003 byte: 4 (04) - Write7BitInt < WriteSpecificProperties < WriteCollection < WriteRootCollection < WriteRoot
            // 00000004 byte: 1 (01) - WritePureObject < WriteElement < WriteCollectionElements < WriteCollection < WriteRootCollection < WriteRoot
            // 00000005 byte: 2 (02) - WritePureObject < WriteElement < WriteCollectionElements < WriteCollection < WriteRootCollection < WriteRoot
            // 00000006 byte: 3 (03) - WritePureObject < WriteElement < WriteCollectionElements < WriteCollection < WriteRootCollection < WriteRoot
            serData[3] = 7;
            for (int i = 0; i < 4; i++)
                serData.Insert(3, 255);
            byte[] manipulatedData = serData.ToArray();

            // without safe mode the list is allocated with MaxInt capacity
            Throws<OutOfMemoryException>(() => DeserializeObject(manipulatedData, bsf));

            // in SafeMode the too large capacity is ignored and the list simply can be deserialized
            bsf.Options = BinarySerializationOptions.SafeMode;
            var deserialized = (List<byte>)DeserializeObject(manipulatedData, bsf);
            AssertItemsEqual(list, deserialized);
        }

        [Test]
        public void SafeModeDictionaryCapacityOutOfMemoryAttackTest()
        {
            var list = new Dictionary<byte, bool> { { 0, false }, { 1, true } };
            var bsf = new BinarySerializationFormatter();
            var serData = new List<byte>(SerializeObject(list, bsf));

            // 7 bit encoded count is at offset 3. 07 + 4xFF will be decoded as MaxInt
            // 00000000 ushort: 8324 (2084 [UInt8 | Extended | Dictionary]) - WriteDataType < WriteRootCollection < WriteRoot
            // 00000002 byte: 2 (02 [Bool]) - WriteDataType < WriteRootCollection < WriteRoot
            // 00000003 byte: 2 (02) - Write7BitInt < WriteSpecificProperties < WriteCollection < WriteRootCollection < WriteRoot
            // 00000004 bool: True (1) - WriteSpecificProperties < WriteCollection < WriteRootCollection < WriteRoot
            // 00000005 byte: 0 (00) - WritePureObject < WriteElement < WriteDictionaryElements < WriteCollection < WriteRootCollection < WriteRoot
            // 00000006 bool: False (0) - WritePureObject < WriteElement < WriteDictionaryElements < WriteCollection < WriteRootCollection < WriteRoot
            // 00000007 byte: 1 (01) - WritePureObject < WriteElement < WriteDictionaryElements < WriteCollection < WriteRootCollection < WriteRoot
            // 00000008 bool: True (1) - WritePureObject < WriteElement < WriteDictionaryElements < WriteCollection < WriteRootCollection < WriteRoot
            serData[3] = 7;
            for (int i = 0; i < 4; i++)
                serData.Insert(3, 255);
            byte[] manipulatedData = serData.ToArray();

            // without safe mode the dictionary is allocated with MaxInt capacity
            Throws<OutOfMemoryException>(() => DeserializeObject(manipulatedData, bsf));

            // in SafeMode the capacity is not preallocated and the deserialization fails when the stream ends unexpectedly
            bsf.Options = BinarySerializationOptions.SafeMode;
            Throws<SerializationException>(() => DeserializeObject(manipulatedData, bsf));
        }

        #endregion
    }
}
