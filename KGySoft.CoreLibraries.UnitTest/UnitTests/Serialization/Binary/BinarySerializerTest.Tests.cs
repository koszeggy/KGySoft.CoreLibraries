#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializerTest.Tests.cs
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
#if NETFRAMEWORK
using System.CodeDom.Compiler;
#endif
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
#if NETCOREAPP && !NETSTANDARD_TEST
using System.Collections.Immutable;
#endif
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
#if !NET35
using System.Numerics;
#endif
#if NETFRAMEWORK
using System.Reflection;
#endif
using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif
#if NETFRAMEWORK
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

#region Suppressions

#pragma warning disable SYSLIB0011 // Type or member is obsolete - this class uses BinaryFormatter for comparisons. It's safe because both serialization and deserialization is in the same process.
#pragma warning disable CS0618 // Use of obsolete symbol - as above, as well as indicating some obsolete types as expected ones when deserializing a Hashtable or other non-generic collections

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
                Assert.IsTrue(EnvironmentHelper.IsPartiallyTrustedDomain);
                var smokeTestObj = ThreadSafeRandom.Instance.NextObject<Dictionary<int, List<string>>>();
                byte[] raw = BinarySerializer.Serialize(smokeTestObj);
                var clone = BinarySerializer.Deserialize<Dictionary<int, List<string>>>(raw);
                AssertDeepEquals(smokeTestObj, clone);

                raw = BinarySerializer.Serialize(smokeTestObj, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
                clone = BinarySerializer.Deserialize<Dictionary<int, List<string>>>(raw, 0, BinarySerializationOptions.None);
                AssertDeepEquals(smokeTestObj, clone);

                var test = new BinarySerializerTest();
                test.SerializeComplexTypes();
                test.SerializeComplexGenericCollections(false); // false: Setting read-only field in ArraySection does not work without SecurityPermissionFlag.SkipVerification
                test.SerializationSurrogateTest(false); // Setting read-only field in StringSegment
                test.SerializeRemoteObjects();
                test.SerializationBinderTest(false);
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
                new Uri(@"x:\teszt"),
                new DictionaryEntry(1, "alpha"),
                new KeyValuePair<int, string>(1, "alpha"),
                new BitArray(new[] { true, false, true }),
                new StringBuilder("alpha"),
                StringSegment.Null,
                "123456".AsSegment(1, 2),
                CultureInfo.InvariantCulture.CompareInfo,
                CompareInfo.GetCompareInfo("en"),
                Comparer.DefaultInvariant,
                Comparer.Default,
                new Comparer(CultureInfo.GetCultureInfo("en")),
                StringComparer.InvariantCulture,
                StringComparer.InvariantCultureIgnoreCase,
                StringComparer.Create(CultureInfo.GetCultureInfo("en"), true),
                StringSegmentComparer.Ordinal,
                StringSegmentComparer.OrdinalIgnoreCase,
                StringSegmentComparer.InvariantCulture,
                StringSegmentComparer.InvariantCultureIgnoreCase,
                StringSegmentComparer.OrdinalNonRandomized,
                StringSegmentComparer.OrdinalIgnoreCaseNonRandomized,
                StringSegmentComparer.OrdinalRandomized,
                StringSegmentComparer.OrdinalIgnoreCaseRandomized,
                StringSegmentComparer.Create(CultureInfo.GetCultureInfo("en"), true),
#if !NET35
                new BigInteger(1),
                new Complex(1.2, 2.3),
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
                new ValueTuple(),
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            var expectedTypes = GetExpectedTypes(referenceObjects).Append(typeof(CompareOptions)).ToArray();
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            // further natively supported types, which are not serializable in every framework
            referenceObjects = new object[]
            {
                null,
                DBNull.Value,
                new BitVector32(13),
                BitVector32.CreateSection(13),
                BitVector32.CreateSection(42, BitVector32.CreateSection(13)),
                typeof(int),
                CultureInfo.InvariantCulture,
                CultureInfo.CurrentCulture,
                CultureInfo.GetCultureInfo(0x10407), // Name: de-DE, CompareInfo.Name: de-DE_phoneb
                StringComparer.Ordinal,
                StringComparer.OrdinalIgnoreCase,
                CaseInsensitiveComparer.DefaultInvariant,
                CaseInsensitiveComparer.Default,
                new CaseInsensitiveComparer(CultureInfo.GetCultureInfo("en")),

#if NET46_OR_GREATER || NETCOREAPP && !(NETCOREAPP2_0 && NETSTANDARD_TEST)
                new Vector2(1, 2),
                new Vector3(1, 2, 3),
                new Vector4(1, 2, 3, 4),
                new Quaternion(1, 2, 3, 4),
                new Plane(1, 2, 3, 4),
                new Matrix3x2(11, 12, 21, 22, 31, 32),
                new Matrix4x4(11, 12, 13, 14, 21, 22, 23, 24, 31, 32, 33, 34, 41, 42, 43, 44),
#endif

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

#if NET7_0_OR_GREATER
                (Int128)1,
                (UInt128)1,
#endif
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);

            // NOTE: SafeMode with ForceRecursiveSerializationOfSupportedTypes cannot be used (eg. due to the internal CultureData type)

#if NETFRAMEWORK
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
#else
            // IgnoreISerializable:
            // - .NET Core 2.0 throws NotSupportedException for DBNull and RuntimeType.GetObjectData.
            // - StringComparer.Ordinal and StringComparer.OrdinalIgnoreCase changes its type during serialization in .NET Core to be compatible with .NET Framework
            // safeCompare: In .NET Core 3 they work but Equals fails for cloned RuntimeType
            // IgnoreSerializationMethods: TextInfo in CultureInfo is no longer [NonSerialized] and it has a IDeserializationCallback that throws PlatformNotSupportedException
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.IgnoreISerializable | BinarySerializationOptions.IgnoreSerializationMethods, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.IgnoreISerializable | BinarySerializationOptions.IgnoreSerializationMethods, safeCompare: true);
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

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);
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

#if !NET5_0_OR_GREATER // System.Runtime.Serialization.SerializationException: Unable to load type System.IO.HandleInheritability required for deserialization.
            SystemSerializeObject(referenceObjects); 
#endif
            SystemSerializeObjects(referenceObjects); // actually here, too, but only fro one row

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            Throws<SerializationException>(() => KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode), "In safe mode you should specify the expected types in the expectedCustomTypes parameter of the deserialization methods.");
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode), "In safe mode you should specify the expected types in the expectedCustomTypes parameter of the deserialization methods.");

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: GetExpectedTypes(referenceObjects));
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: GetExpectedTypes(referenceObjects));

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
        }

#if NET40_OR_GREATER || NETCOREAPP
        [Test]
        public void SerializeTuples()
        {
            object[] referenceObjects =
            {
                // Reference Tuples
                Tuple.Create(1),
                Tuple.Create(1, 2u),
                Tuple.Create(1, 2u, 3L),
                Tuple.Create(1, 2u, 3L, 4ul),
                Tuple.Create(1, 2u, 3L, 4ul, "5"),
                Tuple.Create(1, 2u, 3L, 4ul, "5", '6'),
                Tuple.Create(1, 2u, 3L, 4ul, "5", '6', 7f),
                Tuple.Create(1, 2u, 3L, 4ul, "5", '6', 7f, 8d),
                Tuple.Create(1, 2u, 3L, 4ul, "5", '6', Tuple.Create(7.0f, 7.1d, 7.3m), 8d),
                Tuple.Create(1, 2u, 3L, 4ul, "5", '6', Tuple.Create(7.0f, 7.1d, 7.3m), Tuple.Create(8.0f, 8.1d, 8.3m)),

#if NET47_OR_GREATER || NETCOREAPP
                // Value Tuples
                ValueTuple.Create(),
                ValueTuple.Create(1),
                ValueTuple.Create(1, 2u),
                ValueTuple.Create(1, 2u, 3L),
                ValueTuple.Create(1, 2u, 3L, 4ul),
                ValueTuple.Create(1, 2u, 3L, 4ul, "5"),
                ValueTuple.Create(1, 2u, 3L, 4ul, "5", '6'),
                ValueTuple.Create(1, 2u, 3L, 4ul, "5", '6', 7f),
                ValueTuple.Create(1, 2u, 3L, 4ul, "5", '6', 7f, 8d), // TRest is is ValueTuple`1
                (1, 2u, 3L, 4ul, "5", '6', 7f, 8d, 9m), // TRest is ValueTuple`2
                new ValueTuple<int, uint, long, ulong, string, char, float, double> { Item1 = 1, Item2 = 2u, Item3 = 3L, Item4 = 4ul, Item5 = "5", Item6 = '6', Item7 = 7f, Rest = 8d, }, // TRest is not a nested tuple
                ValueTuple.Create(1, 2u, 3L, 4ul, "5", '6', ValueTuple.Create(7.0f, 7.1d, 7.3m), 8d),
                ValueTuple.Create(1, 2u, 3L, 4ul, "5", '6', ValueTuple.Create(7.0f, 7.1d, 7.3m), Tuple.Create(8.0f, (8.1d, 8.3m))),
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode);

#if NET47_OR_GREATER || !NETFRAMEWORK
            // More complex tuples (safe compare is needed due to embedded collections
            referenceObjects = new object[]
            {
                (new[] { 1, 2 }, 1u),
                (new KeyValuePair<int, string>(1, "2"), (Tuple.Create(3u), new Dictionary<(int, string), (uint, char)?[]>
                {
                    [default] = null,
                    [(11, "22")] = new (uint, char)?[] { (33u, '4'), null },
                })),
                new (int?, string)?[] { (1, "1"), (null, "2"), (3, null), (null, null), null }
            };

            SystemSerializeObjects(referenceObjects, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, safeCompare: true,
                expectedTypes: GetExpectedTypes(referenceObjects).Concat(new[] { EqualityComparer<(int, string)>.Default.GetType(), typeof(IEqualityComparer<>) }));
#endif
        }
#endif

        [Test]
        public void SerializeTypes()
        {
            Type[] referenceObjects =
            {
                // Simple types
                typeof(int),
                typeof(int?),

                typeof(int).MakeByRefType(),
                typeof(int).MakePointerType(),
                typeof(CustomSerializedClass),
                typeof(CustomSerializableStruct?),
                typeof(Type),
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
#if !NET35
                typeof(Tuple<int>),
                typeof(Tuple<int, byte, string, bool, decimal, short, long, char>), // invalid (Rest is not tuple)
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
                typeof(ValueTuple<int>),
                typeof(ValueTuple<int, byte, string, bool, decimal, short, long, char>), // semi-invalid (Rest is not tuple)
#endif

                // Nullable "collections"
                typeof(DictionaryEntry?),
                typeof(KeyValuePair<int, string>?),
                typeof(KeyValuePair<int, CustomSerializedClass>?), // supported generic with mixed parameters
                typeof(ArraySegment<int>?[]),
                typeof(ArraySegment<int?>?[]),

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
#if !NET35
                typeof(Tuple<>),
                typeof(Tuple<,,,,,,,>),
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
                typeof(ValueTuple<>),
                typeof(ValueTuple<,,,,,,,>),
#endif

                // Generic Type Parameters
                typeof(List<>).GetGenericArguments()[0], // T of supported generic type definition argument
                typeof(List<>).GetGenericArguments()[0].MakeArrayType(), // T[]
                typeof(List<>).GetGenericArguments()[0].MakeByRefType(), // T&
                typeof(List<>).GetGenericArguments()[0].MakePointerType(), // T*
                typeof(List<>).GetGenericArguments()[0].MakeArrayType().MakeByRefType(), // T[]&
                typeof(List<>).GetGenericArguments()[0].MakeArrayType().MakePointerType().MakeArrayType(2).MakePointerType().MakeByRefType(), // T[]*[,]*&
                typeof(CustomGenericCollection<>).GetGenericArguments()[0], // T of custom generic type definition argument
                typeof(CustomGenericCollection<>).GetGenericArguments()[0].MakeArrayType(), // T[]
#if !NET35
                typeof(Tuple<>).GetGenericArguments()[0], // T of tuple
#endif

                // Open Constructed Generics
                typeof(List<>).MakeGenericType(typeof(KeyValuePair<,>)), // List<KeyValuePair<,>>
                typeof(List<>).MakeGenericType(typeof(List<>)), // List<List<>>
                typeof(List<>).MakeGenericType(typeof(List<>).GetGenericArguments()[0]), // List<T>
                typeof(OpenGenericDictionary<>).BaseType, // open constructed generic (Dictionary<string, TValue>)
                typeof(KeyValuePair<,>).MakeGenericType(typeof(int), typeof(KeyValuePair<,>).GetGenericArguments()[1]), // open constructed generic (KeyValuePair<int, TValue>)
                typeof(Nullable<>).MakeGenericType(typeof(KeyValuePair<,>)), // open constructed generic (KeyValuePair<,>?)
                typeof(Nullable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(typeof(int), typeof(KeyValuePair<,>).GetGenericArguments()[1])), // open constructed generic (KeyValuePair<int, TValue>?)
#if !NET35
                typeof(Tuple<>).MakeGenericType(typeof(KeyValuePair<,>)), // Tuple<KeyValuePair<,>>
#endif

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

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: referenceObjects.Where(t => t.FullName != null).Concat(new[] { typeof(OpenGenericDictionary<>), typeof(DictionaryExtensions) }));
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: referenceObjects.Where(t => t.FullName != null).Concat(new[] { typeof(OpenGenericDictionary<>), typeof(DictionaryExtensions) }));

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
            // serializable types
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

            IEnumerable<Type> expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(Collection<>), typeof(CustomAdvancedSerializedClassHelper), typeof(CustomGraphDefaultObjRefDeserializer) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            // non-serializable types
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

            expectedTypes = GetExpectedTypes(referenceObjects);
            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
        }

        [Test]
        public void SerializeByteArrays()
        {
            object[] referenceObjects =
            {
                new byte[] { 1, 2, 3 }, // single byte array
                Reflector.EmptyArray<byte>(), // empty array
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

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);
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
                Enumerable.Range(0, 2050).ToArray(), // large primitive non-byte[] array
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
                new StringSegment[] { new StringSegment("alpha", 1, 2), null },
                new Comparer[] { Comparer.DefaultInvariant, Comparer.Default, null },
                new StringComparer[] { StringComparer.InvariantCulture, StringComparer.CurrentCulture, new CustomStringComparer(), null },
#if !NET35
                new BigInteger[] { 1, 2 },
                new Complex[] { new Complex(1.2, 3.4), new Complex(5.6, 7.8) },
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
                new ValueTuple[] { ValueTuple.Create(), new ValueTuple() },
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: new[] { typeof(StringComparer), typeof(CustomStringComparer) });
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: new[] { typeof(StringComparer), typeof(CustomStringComparer) });

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            referenceObjects = new object[]
            {
                new DBNull[] { DBNull.Value, null },
                new BitVector32[] { new BitVector32(13) },
                new BitVector32.Section[] { BitVector32.CreateSection(13), BitVector32.CreateSection(42, BitVector32.CreateSection(13)) },
                new Type[] { typeof(int), typeof(List<int>), null },
                Array.CreateInstance(Reflector.RuntimeType, 3), // runtime type array, set below

#if NET46_OR_GREATER || NETCOREAPP && !(NETCOREAPP2_0 && NETSTANDARD_TEST)
                new[] { new Vector2(1, 2) },
                new[] { new Vector3(1, 2, 3) },
                new[] { new Vector4(1, 2, 3, 4) },
                new[] { new Quaternion(1, 2, 3, 4) },
                new[] { new Plane(1, 2, 3, 4) },
                new[] { new Matrix3x2(11, 12, 21, 22, 31, 32) },
                new[] { new Matrix4x4(11, 12, 13, 14, 21, 22, 23, 24, 31, 32, 33, 34, 41, 42, 43, 44) },
#endif

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

#if NET7_0_OR_GREATER
                new Int128[] { 1, 2 },
                new UInt128[] { 1, 2 },
#endif
            };

            ((Array)referenceObjects[4]).SetValue(typeof(int), 0);
            ((Array)referenceObjects[4]).SetValue(Reflector.RuntimeType, 1);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: new[] { typeof(Type) });
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: new[] { typeof(Type) });

#if !NETCOREAPP2_0 // .NET Core 2.0 throws NotSupportedException for DBNull and RuntimeType.GetObjectData. In .NET Core 3 they work but Equals fails for cloned RuntimeType, hence safeCompare
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, safeCompare: true);

            // SafeMode isn't really usable here, eg. System.UnitySerializationHolder and Types
            //KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, safeCompare: true);
            //KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, safeCompare: true);
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

            var expectedTypes = GetExpectedTypes(referenceObjects);
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

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

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);

            referenceObjects = new object[]
            {
                // system serializer fails: cannot cast string[*] to object[]
                Array.CreateInstance(typeof(string), new int[] {3}, new int[]{-1}) // array with -1..1 index interval
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);
        }

        [Test]
        public void SerializeComplexArrays()
        {
            // serializable types
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

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(SystemSerializableSealedClass), typeof(TestEnumByte), typeof(CustomSerializedClass), typeof(Collection<>), typeof(SerializationEventsClass) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.IgnoreIBinarySerializable);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            // non-serializable types
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

            expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(SystemSerializableSealedClass), typeof(NonSerializableClassWithSerializableBase), typeof(BinarySerializableStruct), typeof(BinarySerializableClass), typeof(BinarySerializableSealedClass), typeof(SystemSerializableStruct), typeof(TestEnumByte), typeof(TestEnumInt) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
        }

        [Test]
        public void SerializeNullableArrays()
        {
            // serializable types
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

                new StringSegment?[] { new StringSegment("123456", 1, 2), StringSegment.Null, null },

                new ArraySegment<int>?[] { new ArraySegment<int>(new[] { 1, 2, 3 }, 1, 2), new ArraySegment<int>(), null },
                new ArraySegment<int?>?[] { new ArraySegment<int?>(new int?[] { 1, 2, 3, null }, 1, 2), new ArraySegment<int?>(), null },

                new ArraySection<int>?[] { new ArraySection<int>(new[] { 1, 2, 3 }, 1, 2), ArraySection<int>.Null, null },
                new ArraySection<int?>?[] { new ArraySection<int?>(new int?[] { 1, 2, 3, null }, 1, 2), ArraySection<int?>.Null, null },

                new Array2D<int>?[] { new Array2D<int>(new[] { 1, 2, 3 }, 1, 2), new Array2D<int>(), null },
                new Array2D<int?>?[] { new Array2D<int?>(new int?[] { 1, 2, 3, null }, 1, 2), new Array2D<int?>(), null },

                new Array3D<int>?[] { new Array3D<int>(new[] { 1, 2, 3, 4, 5, 6 }, 1, 2, 3), new Array3D<int>(), null },
                new Array3D<int?>?[] { new Array3D<int?>(new int?[] { 1, 2, 3, 4, 5, null }, 1, 2, 3), new Array3D<int?>(), null },

                new BinarySerializableStruct?[] { new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" }, null },
                new SystemSerializableStruct?[] { new SystemSerializableStruct { IntProp = 1, StringProp = "alpha" }, null },

#if !NET35
                new BigInteger?[] { 1, null },
                new Complex?[] { new Complex(1.2, 3.4), new Complex(5.6, 7.8), null },
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
                new ValueTuple?[] { new ValueTuple(), null },
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            var expectedTypes = new[] { typeof(TestEnumByte), typeof(BinarySerializableStruct), typeof(SystemSerializableStruct) };
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            // non-serializable types
            referenceObjects = new object[]
            {
                new NonSerializableStruct?[] { new NonSerializableStruct { Bytes3 = new byte[] { 1, 2, 3 }, IntProp = 10, Str10 = "alpha" }, null },
                new BitVector32?[] { new BitVector32(13), null },
                new BitVector32.Section?[] { BitVector32.CreateSection(13), null },

#if NET46_OR_GREATER || NETCOREAPP && !(NETCOREAPP2_0 && NETSTANDARD_TEST)
                new Vector2?[] { new Vector2(1, 2), null, null },
                new Vector3?[] { new Vector3(1, 2, 3), null },
                new Vector4?[] { new Vector4(1, 2, 3, 4), null },
                new Quaternion?[] { new Quaternion(1, 2, 3, 4), null },
                new Plane?[] { new Plane(1, 2, 3, 4), null },
                new Matrix3x2?[] { new Matrix3x2(11, 12, 21, 22, 31, 32), null },
                new Matrix4x4?[] { new Matrix4x4(11, 12, 13, 14, 21, 22, 23, 24, 31, 32, 33, 34, 41, 42, 43, 44), null },
#endif

#if NETCOREAPP3_0_OR_GREATER
#if  !NETSTANDARD_TEST
		        new Rune?[] { new Rune('a'), null },
                new Vector64<int>?[] { Vector64.Create(1, 2), null },
#endif
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

#if NET7_0_OR_GREATER
                new Int128?[] { 1, null },
                new UInt128?[] { 1, null },
#endif
#if NETCOREAPP && !NETSTANDARD_TEST
                new ImmutableArray<int>?[] { ImmutableArray.Create(1, 2, 3, 4), ImmutableArray<int>.Empty, new ImmutableArray<int>(), null },
                new ImmutableArray<int?>?[] { ImmutableArray.Create<int?>(1, 2, 3, 4, null), ImmutableArray<int?>.Empty, new ImmutableArray<int?>(), null },
#endif
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);


            expectedTypes = new[] { typeof(NonSerializableStruct) };
            KGySerializeObject(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForcedSerializationValueTypesAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForcedSerializationValueTypesAsFallback);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes);
        }

        [Test]
        public void SerializeSimpleGenericCollections()
        {
            // serializable types
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

                new ThreadSafeHashSet<int>(new[] { 1, 2, 3 }),
                new ThreadSafeHashSet<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),
                new ThreadSafeHashSet<TestEnumByte>(EnumComparer<TestEnumByte>.Comparer) { TestEnumByte.One, TestEnumByte.Two },

                new ArraySegment<int>(new[] { 1, 2, 3 }, 1, 2),
                new ArraySegment<int[]>(new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 }, null }, 1, 2),
                new ArraySegment<int>(),
#if !NET35
                new ArraySegment<Complex>(new[] { new Complex(1.2, 3.4), new Complex(5.6, 7.8), default }, 1, 2),
#endif

                new ArraySection<int>(new[] { 1, 2, 3 }, 1, 2),
                new ArraySection<int[]>(new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 }, null }, 1, 2),
                new ArraySection<int>(),

                new Array2D<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.AsSection(1), 1, 2),
                new Array2D<int>(),

                new Array3D<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.AsSection(1), 1, 2, 3),
                new Array3D<int>(),

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

                new ThreadSafeDictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new ThreadSafeDictionary<int, TestEnumByte> { { 1, TestEnumByte.One }, { 2, TestEnumByte.Two } },
                new ThreadSafeDictionary<int[], string[]> { { new int[] { 1 }, new string[] { "alpha" } }, { new int[] { 2 }, null } },
                new ThreadSafeDictionary<string, int>(StringComparer.CurrentCulture) { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } },
                new ThreadSafeDictionary<TestEnumByte, int>(EnumComparer<TestEnumByte>.Comparer) { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },

                new StringKeyedDictionary<int> { { "1", 1 }, { "2", 2 } },
                new StringKeyedDictionary<int>(ignoreCase: true) { { "1", 1 }, { "2", 2 } },
                new StringKeyedDictionary<TestEnumByte>(StringSegmentComparer.OrdinalRandomized) { { "1", TestEnumByte.One }, { "2", TestEnumByte.Two } },

                EqualityComparer<int>.Default,
                EqualityComparer<byte>.Default,
#if !NET9_0_OR_GREATER // .NET 9+ changes StringEqualityComparer to GenericEqualityComparer<string>, which is intended: https://github.com/dotnet/runtime/blob/c87cbf63954f179785bb038c23352e60d3c0a933/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/EqualityComparer.cs#L295
                EqualityComparer<string>.Default,
#endif
                EqualityComparer<object>.Default,
                Comparer<int>.Default,
                Comparer<byte>.Default,
                Comparer<string>.Default,
                Comparer<object>.Default,
                EnumComparer<ConsoleColor>.Comparer,
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            IEnumerable<Type> expectedTypes = new[] { typeof(TestEnumByte), typeof(ConsoleColor) };
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

#if NETCOREAPP2_0 || NETCOREAPP2_1
            // Only for HashSet<T> and .NET Core 2.x: typeof(IEqualityComparer<T>.IsAssignableFrom(comparer)) fails in HashSet.OnDeserialization. No idea why, and no idea why the same logic works for Dictionary.
            referenceObjects = referenceObjects.Where(o => !o.GetType().IsGenericTypeOf(typeof(HashSet<>))).ToArray();
#endif
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(IEqualityComparer<>), EqualityComparer<int>.Default.GetType(), EqualityComparer<int[]>.Default.GetType(), StringComparer.CurrentCulture.GetType(), typeof(CompareInfo), typeof(CompareOptions),
#if NETFRAMEWORK
                StringComparer.Ordinal.GetType(),
#else
		        StringComparer.Ordinal.GetType().BaseType,
#endif
                Reflector.ResolveType("KGySoft.CoreLibraries.EnumComparer`1+SerializationUnityHolder"), typeof(IComparer<>), typeof(KeyValuePair<,>), Comparer<int>.Default.GetType(), Comparer<int[]>.Default.GetType(),
                Reflector.ResolveType("System.Collections.Generic.TreeSet`1"), Reflector.ResolveType("System.Collections.Generic.SortedDictionary`2+KeyValuePairComparer"),
                StringSegmentComparer.OrdinalIgnoreCase.GetType(), StringSegmentComparer.OrdinalRandomized.GetType() });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            // non-serializable types
            referenceObjects = new object[]
            {
                new StrongBox<int>(1),
                new StrongBox<int[]>(new[] { 1, 2, 3 }),

#if !NET35
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
#endif

#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
                Vector64.Create(1, 2, 3, 4),
                Vector64.Create(1.2f, 3.4f),
#if NET6_0 // Only in .NET 6 Expression.Field works incorrectly for Vector128. Not a big problem because affects ForceRecursiveSerializationOfSupportedTypes only
                Vector128.Create(1, 2),
#else
                Vector128.Create(1, 2, 3, 4),
#endif
                Vector128.Create(1.2d, 3.4d),
                Vector256.Create(1, 2, 3, 4),
                Vector256.Create(1.2d, 3.4d, 5.6d, 7.8d),
#endif
#if NET8_0_OR_GREATER
                Vector512.Create(1, 2, 3, 4, 5, 6, 7, 8),
                Vector512.Create(1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d),
#endif
#if NETCOREAPP && !NETSTANDARD_TEST
                new ImmutableArray<int>(),
                ImmutableArray<int>.Empty,
                ImmutableArray.Create(1, 2, 3, 4),
                ImmutableArray.Create(1, 2, 3, 4).ToBuilder(),
                ImmutableList<int>.Empty,
                ImmutableList.Create(1, 2, 3, 4),
                ImmutableList.Create(1, 2, 3, 4).ToBuilder(),
                ImmutableHashSet<int>.Empty,
                ImmutableHashSet.Create(1, 2, 3, 4),
                ImmutableHashSet.Create(EnumComparer<ConsoleColor>.Comparer, ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.Green),
                ImmutableHashSet.Create(1, 2, 3, 4).ToBuilder(),
                ImmutableHashSet.Create(EnumComparer<ConsoleColor>.Comparer, ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.Green).ToBuilder(),
                ImmutableSortedSet<int>.Empty,
                ImmutableSortedSet.Create(1, 2, 3, 4),
                ImmutableSortedSet.Create(EnumComparer<ConsoleColor>.Comparer, ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.Green),
                ImmutableSortedSet.Create(1, 2, 3, 4).ToBuilder(),
                ImmutableSortedSet.Create(EnumComparer<ConsoleColor>.Comparer, ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Red, ConsoleColor.Green).ToBuilder(),
                ImmutableQueue<int>.Empty,
                ImmutableQueue.Create(1, 2, 3, 4),
                ImmutableStack<int>.Empty,
                ImmutableStack.Create(1, 2, 3, 4),
                ImmutableDictionary<int, string>.Empty,
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableDictionary(),
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableDictionary((IEqualityComparer<int>)null, StringComparer.OrdinalIgnoreCase),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableDictionary(EnumComparer<ConsoleColor>.Comparer),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableDictionary(EnumComparer<ConsoleColor>.Comparer, StringComparer.OrdinalIgnoreCase),
                ImmutableDictionary.CreateBuilder<int, string>(),
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableDictionary().ToBuilder(),
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableDictionary((IEqualityComparer<int>)null, StringComparer.OrdinalIgnoreCase).ToBuilder(),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableDictionary(EnumComparer<ConsoleColor>.Comparer).ToBuilder(),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableDictionary(EnumComparer<ConsoleColor>.Comparer, StringComparer.OrdinalIgnoreCase).ToBuilder(),
                ImmutableSortedDictionary<int, string>.Empty,
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableSortedDictionary(),
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableSortedDictionary(null, StringComparer.OrdinalIgnoreCase),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableSortedDictionary(EnumComparer<ConsoleColor>.Comparer),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableSortedDictionary(EnumComparer<ConsoleColor>.Comparer, StringComparer.OrdinalIgnoreCase),
                ImmutableSortedDictionary.CreateBuilder<int, string>(),
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableSortedDictionary().ToBuilder(),
                new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }.ToImmutableSortedDictionary(null, StringComparer.OrdinalIgnoreCase).ToBuilder(),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableSortedDictionary(EnumComparer<ConsoleColor>.Comparer).ToBuilder(),
                new Dictionary<ConsoleColor, string> { { ConsoleColor.Red, "R" }, { ConsoleColor.Green, "G" }, { ConsoleColor.Blue, "B" } }.ToImmutableSortedDictionary(EnumComparer<ConsoleColor>.Comparer, StringComparer.OrdinalIgnoreCase).ToBuilder(),
#endif
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            expectedTypes = new[] { typeof(TestEnumByte), typeof(ConsoleColor) };
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

#if !NET7_0_OR_GREATER
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback);
#endif

            // collections with properties to restore
            referenceObjects = new object[]
            {
                new ThreadSafeHashSet<int>(new[] { 1, 2, 3 }) { MergeInterval = TimeSpan.FromMinutes(1), PreserveMergedItems = true },
                new ThreadSafeDictionary<int, string>(new Dictionary<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } }) { MergeInterval = TimeSpan.FromMinutes(1), PreserveMergedKeys = true },
            };

            SystemSerializeObject(referenceObjects, forceEqualityByMembers: true);
            SystemSerializeObjects(referenceObjects, forceEqualityByMembers: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, forceEqualityByMembers: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, forceEqualityByMembers: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, forceEqualityByMembers: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, forceEqualityByMembers: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback, forceEqualityByMembers: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback, forceEqualityByMembers: true);
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

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(DateTime), typeof(IComparer), typeof(IHashCodeProvider), typeof(decimal),
                StringComparer.CurrentCulture.GetType(), typeof(CompareInfo), typeof(CompareOptions), typeof(IEqualityComparer), typeof(Comparer),
#if NETFRAMEWORK
                StringComparer.OrdinalIgnoreCase.GetType(),
#else
		        StringComparer.OrdinalIgnoreCase.GetType().BaseType,
#endif
            Reflector.ResolveType("System.Collections.Specialized.ListDictionary+DictionaryNode"), typeof(DictionaryEntry) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
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
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(SystemSerializableSealedClass), typeof(IComparer), typeof(IHashCodeProvider),
                typeof(IEqualityComparer<>), typeof(SerializationEventsClass) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
        }

        [Test]
        public void SerializeSupportedDictionaries()
        {
            // serializable types
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
                new Dictionary<int, ThreadSafeHashSet<int>> { { 1, new ThreadSafeHashSet<int> { 1, 2 } }, { 2, null } }, // ThreadSafeHashSet
                new Dictionary<int, ArraySegment<int>> { { 1, new ArraySegment<int>(new[] { 1, 2, 3 }, 1, 2) }, { 2, new ArraySegment<int>() } }, // ArraySegment
                new Dictionary<int, ArraySegment<int?>?> { { 1, new ArraySegment<int?>(new int?[] { 1, 2, 3, null }, 1, 2) }, { 2, new ArraySegment<int?>() }, { 3, null } }, // ArraySegment?
                new Dictionary<int, ArraySection<int>> { { 1, new ArraySection<int>(new[] { 1, 2, 3 }, 1, 2) }, { 2, new ArraySection<int>() } }, // ArraySection
                new Dictionary<int, ArraySection<int?>?> { { 1, new ArraySection<int?>(new int?[] { 1, 2, 3, null }, 1, 2) }, { 2, new ArraySection<int?>() }, { 3, null } }, // ArraySection?
                new Dictionary<int, Array2D<int>> { { 1, new Array2D<int>(new[] { 1, 2, 3 }, 1, 2) }, { 2, new Array2D<int>() } }, // Array2D
                new Dictionary<int, Array2D<int?>?> { { 1, new Array2D<int?>(new int?[] { 1, 2, 3, null }, 1, 2) }, { 2, new Array2D<int?>() }, { 3, null } }, // Array2D?
                new Dictionary<int, Array3D<int>> { { 1, new Array3D<int>(new[] { 1, 2, 3 }, 1, 1, 2) }, { 2, new Array3D<int>() } }, // Array3D
                new Dictionary<int, Array3D<int?>?> { { 1, new Array3D<int?>(new int?[] { 1, 2, 3, null }, 1, 1, 2) }, { 2, new Array3D<int?>() }, { 3, null } }, // Array3D?
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
                new Dictionary<int, StringKeyedDictionary<int>> { { 1, new StringKeyedDictionary<int> { { "1", 1 } } }, { 2, null } }, // StringKeyedDictionary

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

                // tuple value
#if !NET35
                new Dictionary<int, Tuple<int, int>> { { 1, new Tuple<int, int>(1, 2) }, { 2, null } }, // Tuple
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
                new Dictionary<int, (int, int)> { { 1, (1, 2) } }, // ValueTuple
                new Dictionary<int, (int, int)?> { { 1, (1, 2) }, { 2, null } }, // ValueTuple?
#endif

                // non-natively supported value: recursive
                new Dictionary<int, Collection<int>> { { 1, new Collection<int> { 1, 2 } }, { 2, null } }, // Collection
                new Dictionary<int, ReadOnlyCollection<int>> { { 1, new ReadOnlyCollection<int>(new[] { 1, 2 }) }, { 2, null } }, // ReadOnlyCollection

                // other generic dictionary types as outer objects
                new SortedList<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } },
                new SortedDictionary<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } },
                new KeyValuePair<int, int[]>(1, new[] { 1, 2 }),
                new CircularSortedList<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } },
                new StringKeyedDictionary<int> { { "alpha", 1 }, { "beta", 2 } },
            };

#if !(NETCOREAPP2_0 || NETCOREAPP2_1) // ArraySegment: ArgumentException: Field in TypedReferences cannot be static or init only.
            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            IEnumerable<Type> expectedTypes = new[] { typeof(Collection<>), typeof(ReadOnlyCollection<>) };
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

#if !(NETCOREAPP2_0 || NETCOREAPP2_1) // HashSet<T>.OnDeserialized for its comparer: InvalidCastException: Object must implement IConvertible
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);

            expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { EqualityComparer<int>.Default.GetType(), typeof(IEqualityComparer<>), Comparer<int>.Default.GetType(), typeof(IComparer<>),
                Reflector.ResolveType("System.Collections.Generic.TreeSet`1"), Reflector.ResolveType("System.Collections.Generic.SortedDictionary`2+KeyValuePairComparer"),
                typeof(IComparer), typeof(IHashCodeProvider), typeof(Comparer), typeof(CompareInfo), typeof(IEqualityComparer),
                Reflector.ResolveType("System.Collections.Specialized.ListDictionary+DictionaryNode") });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
#endif

            // non-serializable types
            referenceObjects = new object[]
            {
                new Dictionary<int, StrongBox<int>> { { 1, new StrongBox<int>(1) }, { 2, null } },
#if !NET35
                new Dictionary<int, ConcurrentBag<int>> { { 1, new ConcurrentBag<int> { 1, 2 } }, { 2, null } },
                new Dictionary<int, ConcurrentQueue<int>> { { 1, new ConcurrentQueue<int>(new[] { 1, 2 }) }, { 2, null } },
                new Dictionary<int, ConcurrentStack<int>> { { 1, new ConcurrentStack<int>(new[] { 1, 2 }) }, { 2, null } },
#endif

#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
                new Dictionary<int, Vector128<int>> { { 1, Vector128.Create(1, 2, 3, 4) } },
#endif
#if NETCOREAPP && !NETSTANDARD_TEST
                new Dictionary<int, ImmutableArray<int>> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableArray() } },
                new Dictionary<int, ImmutableArray<int>.Builder> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableArray().ToBuilder() } },
                new Dictionary<int, ImmutableList<int>> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableList() } },
                new Dictionary<int, ImmutableList<int>.Builder> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableList().ToBuilder() } },
                new Dictionary<int, ImmutableHashSet<int>> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableHashSet() } },
                new Dictionary<int, ImmutableHashSet<int>.Builder> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableHashSet().ToBuilder() } },
                new Dictionary<int, ImmutableSortedSet<int>> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableSortedSet() } },
                new Dictionary<int, ImmutableSortedSet<int>.Builder> { { 1, new[] { 1, 2, 3, 4 }.ToImmutableSortedSet().ToBuilder() } },
                new Dictionary<int, ImmutableQueue<int>> { { 1, ImmutableQueue.Create(1, 2, 3, 4) } },
                new Dictionary<int, ImmutableStack<int>> { { 1, ImmutableStack.Create(1, 2, 3, 4) } },
                new Dictionary<int, ImmutableDictionary<int, int>> { { 1, new Dictionary<int, int> { { 1, 2 } }.ToImmutableDictionary() }, { 2, null } }, // ImmutableDictionary 
                new Dictionary<int, ImmutableSortedDictionary<int, int>> { { 1, new Dictionary<int, int> { { 1, 2 } }.ToImmutableSortedDictionary() }, { 2, null } }, // ImmutableSortedDictionary 
#endif
                new Dictionary<int, AllowNullDictionary<int?, int>> { { 1, new AllowNullDictionary<int?, int> { { null, 0 }, { 1, 1 } } } },
#if NET9_0_OR_GREATER
                new Dictionary<int, OrderedDictionary<int, int>> { { 1, new OrderedDictionary<int, int> { { 0, 0 }, { 1, 1 } } } },
#endif

                // other generic dictionary types as outer objects
#if !NET35
                new ConcurrentDictionary<int, int[]>(new Dictionary<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } }),
#endif
#if NETCOREAPP && !NETSTANDARD_TEST
                new Dictionary<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } }.ToImmutableDictionary(), // ImmutableDictionary
                new Dictionary<int, int[]> { { 1, new[] { 1, 2 } }, { 2, null } }.ToImmutableSortedDictionary(), // ImmutableSortedDictionary
#endif
                new AllowNullDictionary<string, int> { { null, 0 }, { "1", 1 } },
#if NET9_0_OR_GREATER
                new OrderedDictionary<int, int> { { 0, 0 }, { 1, 1 } },
#endif
            };

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode);

#if !NET6_0_OR_GREATER // .NET6: Expression.Field gets fields of Vector128<int> incorrectly; .NET 7+: ConcurrentBag: https://github.com/dotnet/runtime/issues/67491
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback);
#endif
        }

        [TestCase(true)] // false is called from Sandbox.DoTest
        public void SerializeComplexGenericCollections(bool includeForcedRecursive)
        {
            object[] referenceObjects =
            {
                new List<byte>[] { new List<byte> { 11, 12, 13 }, new List<byte> { 21, 22 } }, // array of byte lists
                new List<byte[]> { new byte[] { 11, 12, 13 }, new byte[] { 21, 22 } }, // list of byte arrays
                new List<Array> { new byte[] { 11, 12, 13 }, new short[] { 21, 22 } }, // list of any arrays
                new List<Array[]> { null, new Array[] { new byte[] { 11, 12, 13 }, new short[] { 21, 22 } } }, // list of array of any arrays
                new List<ArraySegment<int>?> { new ArraySegment<int>(new[] { 1, 2, 3 }, 1, 2), new ArraySegment<int>(), null },
                new List<ArraySegment<int?>?> { new ArraySegment<int?>(new int?[] { 1, 2, 3, null }, 1, 2), new ArraySegment<int?>(), null },
                new List<ArraySection<int>?> { new ArraySection<int>(new[] { 1, 2, 3 }, 1, 2), new ArraySection<int>(), null },
                new List<ArraySection<int?>?> { new ArraySection<int?>(new int?[] { 1, 2, 3, null }, 1, 2), new ArraySection<int?>(), null },
                new List<Array2D<int>?> { new Array2D<int>(new[] { 1, 2, 3 }, 1, 2), new Array2D<int>(), null },
                new List<Array2D<int?>?> { new Array2D<int?>(new int?[] { 1, 2, 3, null }, 1, 2), new Array2D<int?>(), null },

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
                new SortedList<ConsoleColor, Dictionary<BinarySerializationOptions, IBinarySerializable>> { { ConsoleColor.White, new Dictionary<BinarySerializationOptions, IBinarySerializable> { { BinarySerializationOptions.ForcedSerializationValueTypesAsFallback, new BinarySerializableStruct { IntProp = 1, StringProp = "alpha" } } } } },

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

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(TestEnumByte), typeof(Collection<>), typeof(SerializationEventsClass), typeof(CustomAdvancedSerializedClassHelper) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            if (includeForcedRecursive)
            {
                KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
                KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            }
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

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(CustomGenericCollection<>), typeof(CustomNonGenericCollection) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
        }

        [Test]
        public void SerializeCache()
        {
#if NETFRAMEWORK // must be a static method; otherwise, a compiler-generated type should be included to the expected types when serializing delegates
            static string ItemLoader(string s) => s.ToUpperInvariant();
#endif

            object[] referenceObjects =
            {
                new Cache<int, string> { { 1, "alpha" }, { 2, "beta" }, { 3, "gamma" } },
                new Cache<int[], string[]> { { new int[] { 1 }, new string[] { "alpha" } }, { new int[] { 2 }, null } },
                new Cache<string, int>(StringComparer.CurrentCulture) { { "alpha", 1 }, { "Alpha", 2 }, { "ALPHA", 3 } },
                new Cache<TestEnumByte, int> { { TestEnumByte.One, 1 }, { TestEnumByte.Two, 2 } },
#if NETFRAMEWORK // SerializationException : Serializing delegates is not supported on this platform.
                new Cache<string, string>(ItemLoader) { { "alpha", "ALPHA" } },
#endif
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            var expectedTypes = new[]
            {
                typeof(Cache<,>), typeof(TestEnumByte), typeof(IEqualityComparer<>),
#if NETFRAMEWORK
                Reflector.ResolveType("System.DelegateSerializationHolder"), Reflector.ResolveType("System.DelegateSerializationHolder+DelegateEntry"), Reflector.ResolveType("System.Reflection.MemberInfoSerializationHolder"), typeof(Type), Reflector.ResolveType("System.Reflection.RuntimeMethodInfo"), typeof(Func<,>)
#endif
            };
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
        }

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical] // because of RemotingSurrogateSelector
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

        [TestCase(true)] // false is called from Sandbox.DoTest
        public void SerializationBinderTest(bool includeForcedRecursive)
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

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[] { typeof(IEqualityComparer<>), typeof(Collection<>), typeof(SerializationEventsClass), typeof(CustomAdvancedSerializedClassHelper), typeof(OpenGenericDictionary<>), typeof(Array) });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            // by TestSerializationBinder
            string title = "Serialization and Deserialization with TestSerializationBinder";
            SerializationBinder binder = new TestSerializationBinder();
#if !(NET35 || NETCOREAPP)
            //SystemSerializeObject(referenceObjects, title, binder: binder); // Executes the special constructor on CustomSerializedSealedClass that should not be executed; 'The constructor to deserialize an object of type 'System.RuntimeType' was not found.'
            //SystemSerializeObjects(referenceObjects, title, binder: binder); // On deserialization calls the binder with names that were not set by on serialization
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, binder: binder);

            Throws<SerializationException>(() => KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, title, binder: binder), Res.BinarySerializationBinderNotAllowedInSafeMode);
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, title, binder: binder), Res.BinarySerializationBinderNotAllowedInSafeMode);

#if NETCOREAPP
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, safeCompare: true, binder: binder); // safeCompare: the cloned runtime types are not equal
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, safeCompare: true, binder: binder); // safeCompare: the cloned runtime types are not equal
#else
            if (includeForcedRecursive)
            {
                KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder:binder);
                KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder:binder);
            }
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

            Throws<SerializationException>(() => KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, binder: binder), Res.BinarySerializationBinderNotAllowedInSafeMode);
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, binder: binder), Res.BinarySerializationBinderNotAllowedInSafeMode);

#if NETCOREAPP
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, safeCompare: true, binder: binder); // safeCompare: the cloned runtime types are not equal
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes
                | BinarySerializationOptions.RecursiveSerializationAsFallback // .NET Core 2/3: RuntimeType is not serializable
                | BinarySerializationOptions.IgnoreISerializable, // .NET Core 2: still, it has the GetObjectData that throws a PlatformNotSupportedException
                title, safeCompare: true, binder: binder); // safeCompare: the cloned runtime types are not equal
#else
            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, binder: binder);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames, title, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.OmitAssemblyQualifiedNames, title, binder: binder);
        }

        [TestCase(true)]
        public void SerializationSurrogateTest(bool alsoForSupportedTypes)
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
                StringSegment.Null,
                new StringSegment("12345", 1, 2),

                TestEnumByte.Two,
                new KeyValuePair<int, object>[] { new KeyValuePair<int, object>(1, "alpha"), new KeyValuePair<int, object>(2, new TestEnumByte[] { TestEnumByte.One, TestEnumByte.Two }), },

#if !NET35
                Tuple.Create(1, "2"),
                new BigInteger(1),
                new Complex(1.2, 3.4),
#endif

#if NET46_OR_GREATER || NETCOREAPP && !(NETCOREAPP2_0 && NETSTANDARD_TEST)
                new Vector2(1, 2),
                new Vector3(1, 2, 3),
                new Vector4(1, 2, 3, 4),
                new Quaternion(1, 2, 3, 4),
                new Plane(1, 2, 3, 4),
                new Matrix3x2(11, 12, 21, 22, 31, 32),
                new Matrix4x4(11, 12, 13, 14, 21, 22, 23, 24, 31, 32, 33, 34, 41, 42, 43, 44),
#endif

#if NET47_OR_GREATER || !NETFRAMEWORK
                (1, "2"),
#endif

#if NETCOREAPP3_0_OR_GREATER
#if !NETSTANDARD_TEST
                new Rune('a'),  
#endif
                new Index(1),
                new Range(1, 2),
#endif

#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
                Vector64.Create(1, 2, 3, 4),
#if NET6_0 // Only in .NET 6 Expression.Field works incorrectly for Vector128. Not a big problem because affects the TestSurrogateSelector only
                Vector128.Create(1, 2),
#else
                Vector128.Create(1, 2, 3, 4),
#endif
                Vector256.Create(1, 2, 3, 4),
#endif
#if NET8_0_OR_GREATER
                Vector512.Create(1, 2, 3, 4, 5, 6, 7, 8),
#endif

#if NET5_0_OR_GREATER
                (Half)1,
#endif

#if NET6_0_OR_GREATER
                DateOnly.FromDateTime(DateTime.Today),
                TimeOnly.FromDateTime(DateTime.Now),
#endif

#if NET7_0_OR_GREATER
                (Int128)1,
                (UInt128)1,
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
                new List<ArraySegment<int>> { new ArraySegment<int>(new[] { 1, 2, 3 }, 1, 2), new ArraySegment<int>() },
                new List<ArraySegment<int?>?> { new ArraySegment<int?>(new int?[] { 1, 2, 3, null }, 1, 2), new ArraySegment<int?>(), null },
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

#if NETCOREAPP && !NETSTANDARD_TEST
		        new[] { 1, 2, 3 }.ToImmutableList(),
#endif
            };

            // default
            //SystemSerializeObjects(referenceObjects); // system serialization fails: IBinarySerializable, Rune, etc. is not serializable
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            var selector = new TestSurrogateSelector();
            string title = nameof(TestSurrogateSelector);
            //SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector); // system deserialization fails: Invalid BinaryFormatter stream.
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            if (alsoForSupportedTypes)
                KGySerializeObjects(referenceObjects, BinarySerializationOptions.TryUseSurrogateSelectorForAnyType, title, surrogateSelector: selector);
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, title, surrogateSelector: selector), Res.BinarySerializationSurrogateNotAllowedInSafeMode);

            selector = new TestCloningSurrogateSelector();
            title = nameof(TestCloningSurrogateSelector);
#if NETFRAMEWORK
            if (!EnvironmentHelper.IsPartiallyTrustedDomain) // Setting read-only field StringSegment.str by the surrogate selector
#endif
            {
                SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector);
            }

            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, title, surrogateSelector: selector));
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            if (alsoForSupportedTypes)
                KGySerializeObjects(referenceObjects, BinarySerializationOptions.TryUseSurrogateSelectorForAnyType, title, surrogateSelector: selector);
        }

        [Test]
        [Obsolete]
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
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, title, surrogateSelector: selector), Res.BinarySerializationSurrogateNotAllowedInSafeMode);
        }

        [Test]
#if NET8_0_OR_GREATER
        [Obsolete]
#endif
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
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, title, surrogateSelector: selector), Res.BinarySerializationSurrogateNotAllowedInSafeMode);

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
                new object[] { s1, s1 }, // same references
                new object[] { s1, s2 }, // different references but same values
                new string[] { s1, s1 }, // same references
                new string[] { s1, s2 }, // different references but same values
                new SystemSerializableClass[] { tc }, // custom class, single instance
                new SystemSerializableClass[] { tc, tc, tc, tc }, // custom class, multiple instances*
                new SystemSerializableStruct[] { (SystemSerializableStruct)ts }, // custom struct, single instance
                new SystemSerializableStruct[] { (SystemSerializableStruct)ts, (SystemSerializableStruct)ts, (SystemSerializableStruct)ts, (SystemSerializableStruct)ts }, // custom struct, multiple unboxed instances
                new object[] { ts }, // custom struct, single boxed instance
                new object[] { ts, ts, ts, ts }, // custom struct, multiple boxed instances
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            var expectedTypes = new[] { typeof(SystemSerializableClass), typeof(SystemSerializableStruct) };
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
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

            //SystemSerializeObject(referenceObjects); // Field in TypedReferences cannot be static or init only (SelfReferencerDirect/Indirect), StrongBox is not serializable
            //SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[]
            {
                typeof(Collection<>), typeof(Box<>), typeof(SelfReferencerIndirect.SelfReferencerIndirectDefaultDeserializer), typeof(SelfReferencerIndirect.SelfReferencerIndirectCustomDeserializer),
#if NETFRAMEWORK
                Reflector.ResolveType("System.Text.CodePageEncoding"), Reflector.ResolveType("System.Text.InternalEncoderBestFitFallback"), Reflector.ResolveType("System.Text.InternalDecoderBestFitFallback")
#endif
            });
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);

            // Constructed indirect self references
            referenceObjects = new object[]
            {
                false, // grand-grandchild is root again
                false, // DictionaryEntry referencing the referenceObjects and thus itself
                false, // KeyValuePair referencing the referenceObjects and thus itself
                false, // indirect tuple self-reference
                false, // ArraySegment
            };

            var root = new CircularReferenceClass { Name = "root" }.AddChild("child").AddChild("grandchild").Parent.Parent;
            root.Children[0].Children[0].Children.Add(root);
            referenceObjects[0] = root;

            referenceObjects[1] = new DictionaryEntry(1, referenceObjects);
            referenceObjects[2] = new KeyValuePair<int, object>(2, referenceObjects);
#if NET47_OR_GREATER || !NETFRAMEWORK
            referenceObjects[3] = (3, referenceObjects);
#endif
            var segment = new ArraySegment<object>(new object[2]);
            object segmentRef = segment;
            segment.Array![0] = segmentRef;
            segment.Array![1] = segment.Array;
            referenceObjects[4] = segmentRef;

            SystemSerializeObject(referenceObjects, safeCompare: true);
            SystemSerializeObjects(referenceObjects, safeCompare: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, safeCompare: true);

            // SafeMode compare fails on List<T>.Capacity because the collection initializer overrides capacity in safe mode
            //expectedTypes = new[] { typeof(CircularReferenceClass), typeof(Collection<>) };
            //KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, safeCompare: true, expectedTypes: expectedTypes);
            //KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, safeCompare: true, expectedTypes: expectedTypes);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback, safeCompare: true);

            // Direct self-references
            // NOTE: These don't actually test ApplyPendingUsages because neither an IObjectReference, nor a surrogate selector is involved
            //       To test updating tracked replaced object see Box<T> in SelfReferencerIndirect.
            referenceObjects = new object[]
            {
                false, // tuple
                false, // value tuple
                false, // DictionaryEntry
                false, // KeyValuePair
                false, // array
                false, // ImmutableArray
                false, // ImmutableArray.Builder
                false, // ImmutableList.Builder
                false, // ImmutableHashSet.Builder
                false, // ImmutableSortedSet.Builder
                false, // ImmutableDictionary.Builder/Key
                false, // ImmutableDictionary.Builder/Value
                false, // ImmutableDictionary.Builder/Key+Value
                false, // OrderedDictionary<,>/Key
                false, // OrderedDictionary<,>/Value
                false, // OrderedDictionary<,>/Key+Value
            };
#if !NET35
            // tuple
            var tupleDirectReference = Tuple.Create(1, new object());
            Reflector.SetField(tupleDirectReference, "m_Item2", tupleDirectReference);
            referenceObjects[0] = tupleDirectReference;
#endif

#if NET47_OR_GREATER || !NETFRAMEWORK
            // value tuple
            object valueTupleDirectReference = (2, new object());
            Reflector.SetField(valueTupleDirectReference, "Item2", valueTupleDirectReference);
            referenceObjects[1] = valueTupleDirectReference;
#endif

            // DictionaryEntry
            object dictionaryEntryDirectReference = new DictionaryEntry(3, null);
            Reflector.SetProperty(dictionaryEntryDirectReference, "Value", dictionaryEntryDirectReference);
            referenceObjects[2] = dictionaryEntryDirectReference;

            // KeyValuePair
            object kvpDirectReference = new KeyValuePair<int, object>(4, null);
            Reflector.SetField(kvpDirectReference, "value", kvpDirectReference);
            referenceObjects[3] = kvpDirectReference;

            // array
#if NET47_OR_GREATER || !NETFRAMEWORK
            referenceObjects[4] = (5, referenceObjects);
#endif

            // Here, because the following types are not binary serializable
            SystemSerializeObject(referenceObjects, safeCompare: true);
            SystemSerializeObjects(referenceObjects, safeCompare: true);

#if NETCOREAPP && !NETSTANDARD_TEST
            // ImmutableArray
            object[] array = new object[1];
            object immutableArray = typeof(ImmutableArray<object>).CreateInstance(array.GetType(), array);
            array[0] = immutableArray;
            referenceObjects[5] = immutableArray;

            // ImmutableArray.Builder
            ImmutableArray<object>.Builder immutableArrayBuilder = ImmutableArray.CreateBuilder<object>(1);
            immutableArrayBuilder.Add(immutableArrayBuilder);
            referenceObjects[6] = immutableArrayBuilder;

            // ImmutableList.Builder
            ImmutableList<object>.Builder immutableListBuilder = ImmutableList.CreateBuilder<object>();
            immutableListBuilder.Add(immutableListBuilder);
            referenceObjects[7] = immutableListBuilder;

            // ImmutableHashSet.Builder
            ImmutableHashSet<object>.Builder immutableHashSetBuilder = ImmutableHashSet.CreateBuilder<object>();
            immutableHashSetBuilder.Add(immutableHashSetBuilder);
            referenceObjects[8] = immutableHashSetBuilder;

            // ImmutableSortedSet.Builder
            ImmutableSortedSet<object>.Builder immutableSortedSetBuilder = ImmutableSortedSet.CreateBuilder<object>();
            immutableSortedSetBuilder.Add(immutableSortedSetBuilder);
            referenceObjects[9] = immutableSortedSetBuilder;

            // ImmutableDictionary.Builder
            ImmutableDictionary<object, object>.Builder immutableDictionaryBuilder = ImmutableDictionary.CreateBuilder<object, object>();
            immutableDictionaryBuilder.Add(immutableDictionaryBuilder, null); // key
            referenceObjects[10] = immutableDictionaryBuilder;

            immutableDictionaryBuilder = ImmutableDictionary.CreateBuilder<object, object>();
            immutableDictionaryBuilder.Add(new object(), immutableDictionaryBuilder); // value
            referenceObjects[11] = immutableDictionaryBuilder;

            immutableDictionaryBuilder = ImmutableDictionary.CreateBuilder<object, object>();
            immutableDictionaryBuilder.Add(immutableDictionaryBuilder, immutableDictionaryBuilder); // key+value
            referenceObjects[12] = immutableDictionaryBuilder;
#endif

#if NET9_0_OR_GREATER
            // OrderedDictionary<,>
            var orderedDict = new OrderedDictionary<object, object> { { 0, 0 }, { 1, 1 } };
            orderedDict.Insert(1, orderedDict, null);
            referenceObjects[13] = orderedDict;

            orderedDict = new OrderedDictionary<object, object> { { 0, 0 }, { 1, 1 } };
            orderedDict.Insert(1, new object(), orderedDict);
            referenceObjects[14] = orderedDict;

            orderedDict = new OrderedDictionary<object, object> { { 0, 0 }, { 1, 1 } };
            orderedDict.Insert(1, orderedDict, orderedDict);
            referenceObjects[15] = orderedDict;
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, safeCompare: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, safeCompare: true);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback, safeCompare: true);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.RecursiveSerializationAsFallback, safeCompare: true);

            // Invalid self references
            referenceObjects = new object[]
            {
                new SelfReferencerIndirect("Default") { UseCustomDeserializer = false, UseValidWay = false },
                new SelfReferencerIndirect("Custom") { UseCustomDeserializer = true, UseValidWay = false },
                new SelfReferencerInvalid("Default") { UseCustomDeserializer = false },
                new SelfReferencerInvalid("Custom") { UseCustomDeserializer = true },
            };

            //// Field in TypedReferences cannot be static or init only (SelfReferencerIndirect); StrongBox is not serializable
            //foreach (object referenceObject in referenceObjects)
            //    SystemSerializeObject(referenceObject);

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
            //KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, surrogateSelector: selector); // OrderedDictionary.OnDeserialization, Box<T>.Equals: other.xxxDictionary[other.Value] (because the surrogate creates clones where it's not needed)

            title = "Valid cases using a replacing selector";
            selector = new TestCloningSurrogateSelector();
            referenceObjects = new object[]
            {
                new CircularReferenceClass { Name = "Single" }, // no circular reference
            };

            SystemSerializeObjects(referenceObjects, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None, title, surrogateSelector: selector);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes, title, surrogateSelector: selector);
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, title, surrogateSelector: selector), Res.BinarySerializationSurrogateNotAllowedInSafeMode);

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
        public void BinarySerializerTest_PartiallyTrusted()
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

#if NETFRAMEWORK // .NET Core: SerializationException: Type 'System.Reflection.Pointer' is not marked as serializable.
            SystemSerializeObject(referenceObjects, safeCompare: true);
            SystemSerializeObjects(referenceObjects, safeCompare: true);
#endif

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: new[] { typeof(UnsafeStruct) });
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: new[] { typeof(UnsafeStruct) });

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

#if NETCOREAPP3_0_OR_GREATER
        [TestCase<bool>]
        [TestCase<int>]
        [TestCase<decimal>]
        [TestCase<LargeUnmanagedStruct>]
        [TestCase<KeyValuePair<int, int>>] // tricky: not unmanaged, still, it works
        [TestCase<ValueTuple<int, int>>] // tricky: not unmanaged, still, it works
#else
        [TestCaseGeneric(TypeArguments = new[] { typeof(bool) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(int) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(decimal) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(LargeUnmanagedStruct) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(KeyValuePair<int, int>) })] // tricky: not unmanaged, still, it works
        [TestCaseGeneric(TypeArguments = new[] { typeof(ValueTuple<int, int>) })] // tricky: not unmanaged, still, it works
#endif
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

#if NETCOREAPP3_0_OR_GREATER
        [TestCase<bool>(true)]
        [TestCase<int>(true)]
        [TestCase<decimal>(true)]
        [TestCase<LargeUnmanagedStruct>(true)]
        [TestCase<LargeStructToBeMarshaled>(false)]
        [TestCase<KeyValuePair<int, int>>(true)]
        [TestCase<KeyValuePair<int, string>>(false)]
        [TestCase<ValueTuple<int, int>>(true)]
#else
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(bool) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(int) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(decimal) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(LargeUnmanagedStruct) })]
        [TestCaseGeneric(false, TypeArguments = new[] { typeof(LargeStructToBeMarshaled) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(KeyValuePair<int, int>) })]
        [TestCaseGeneric(false, TypeArguments = new[] { typeof(KeyValuePair<int, string>) })]
        [TestCaseGeneric(true, TypeArguments = new[] { typeof(ValueTuple<int, int>) })]
#endif
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

#if NETCOREAPP3_0_OR_GREATER
        [TestCase<bool>]
        [TestCase<int>]
        [TestCase<decimal>]
        [TestCase<LargeUnmanagedStruct>]
        [TestCase<KeyValuePair<int, int>>] // tricky: not unmanaged, still, it works
        [TestCase<ValueTuple<int, int>>] // tricky: not unmanaged, still, it works
#else
        [TestCaseGeneric(TypeArguments = new[] { typeof(bool) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(int) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(decimal) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(LargeUnmanagedStruct) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(KeyValuePair<int, int>) })] // tricky: not unmanaged, still, it works
        [TestCaseGeneric(TypeArguments = new[] { typeof(ValueTuple<int, int>) })] // tricky: not unmanaged, still, it works
#endif
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

            SystemSerializeObject(obj, binder: binder);
            KGySerializeObject(obj, BinarySerializationOptions.None, binder: binder);
        }

        [Test]
        public void SerializeUsingSameTypeReferenceWithDifferentNames()
        {
            object[] referenceObjects =
            {
                Singleton1.Instance, // SerializationInfo.SetType
                Singleton2.Instance, // SerializationInfo.AssemblyName = weak name
                Singleton3.Instance, // SerializationInfo.AssemblyName = full name (the same name as if SetType was used but by string)
                new[] { Singleton1.Instance },
                new[] { Singleton2.Instance },
                new[] { Singleton3.Instance },
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);

            // Singleton2 uses a weak assembly name identity, which cannot be resolved in SafeMode even if the type is specified
            var expectedTypes = new[] { typeof(Singleton1), typeof(Singleton2), typeof(Singleton3), typeof(SingletonDeserializer) };
            Throws<SerializationException>(() => KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes), "in assembly \"KGySoft.CoreLibraries.UnitTest\"");
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes), "in assembly \"KGySoft.CoreLibraries.UnitTest\"");

            // But we can use the ForwardedTypesSerializationBinder in SafeMode
            var binder = new ForwardedTypesSerializationBinder { SafeMode = true };
            binder.AddTypes(expectedTypes);
            KGySerializeObject(referenceObjects, BinarySerializationOptions.SafeMode, binder: binder);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, binder: binder);

            // Even if we use it only for deserializing because ForwardedTypesSerializationBinder allows weak matching if no assembly name was specified
            var bsf = new BinarySerializationFormatter(BinarySerializationOptions.SafeMode);
            byte[] rawData = SerializeObjects(referenceObjects, bsf);
            bsf.Binder = binder;
            var clone = DeserializeObjects(rawData, bsf);
            AssertDeepEquals(referenceObjects, clone);
        }

        [Test]
        public void NullReferenceSerializerTest()
        {
            var referenceObject = new NullReference();
            byte[] raw;
            object result;

#if !NET9_0_OR_GREATER
            Console.WriteLine("------------------System BinaryFormatter--------------------");
            BinaryFormatter bf = new BinaryFormatter();
            raw = SerializeObject(referenceObject, bf);
            result = DeserializeObject(raw, bf);
            Assert.IsNull(result); 
#endif

            Console.WriteLine($"------------------KGy SOFT BinarySerializer--------------------");
            BinarySerializationFormatter bsf = new BinarySerializationFormatter(BinarySerializationOptions.RecursiveSerializationAsFallback);
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

            var expectedTypes = GetExpectedTypes(referenceObjects).Concat(new[]
            { 
#if !NET35
		        Reflector.ResolveType("System.Collections.ObjectModel.ObservableCollection`1+SimpleMonitor"),
#endif
                typeof(List<>),
#if !NETCOREAPP2_0
                Reflector.ResolveType("System.UnitySerializationHolder"),
#endif
                EqualityComparer<int>.Default.GetType(),
                typeof(IEqualityComparer<>)
            });
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.IgnoreTypeForwardedFromAttribute | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes);
        }

        [Test]
        public void SafeModeNonSerializableTest()
        {
            object[] referenceObjects =
            {
                new NonSerializableClass { IntProp = 42 }
            };

            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback);
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode), "In safe mode you should specify the expected types in the expectedCustomTypes parameter of the deserialization methods.");

            var expectedTypes = new[] { typeof(NonSerializableClass) };
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes), Res.BinarySerializationCannotCreateSerializableObjectSafe(typeof(NonSerializableClass)));
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes);
        }

        [Test]
        public void SafeModeAssemblyLoadingTest()
        {
#if NET35
            string asmName = "System.Design, Version=2.0.0.0, PublicKeyToken=b77a5c561934e089"; 
            string typeName = "System.Windows.Forms.Design.Behavior.SnapLineType";
#else
            string asmName = "System.Data, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089";
            string typeName = "System.Data.ConnectionState";
#endif
            if (Reflector.ResolveAssembly(asmName, ResolveAssemblyOptions.AllowPartialMatch) != null)
            {
                Assert.Inconclusive($"Assembly {asmName} is already loaded, test is ignored. Try to run this test alone.");
                return;
            }

            // using a proxy type and a binder to serialize a type information that is not already loaded (content is not relevant)
            Type proxyType = typeof(SystemSerializableClass);

            // only serialization way is set
            var binder = new CustomSerializationBinder()
            {
                AssemblyNameResolver = t => t == proxyType ? asmName : null,
                TypeNameResolver = t => t == proxyType ? typeName : null
            };

            var formatter = new BinarySerializationFormatter(BinarySerializationOptions.SafeMode) { Binder = binder };
            var data = SerializeObject(Reflector.CreateInstance(proxyType), formatter);
            formatter.Binder = null;
            Throws<SerializationException>(() => DeserializeObject(data, formatter), Res.BinarySerializationCannotResolveExpectedTypeInAssemblySafe(typeName, asmName));
        }


        [Test]
        public void SerializeRecords()
        {
            object[] referenceObjects =
            {
                new ClassRecord("alpha", 1),
                new ValueRecord("alpha", 1),
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.None);
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
            var expectedTypes = new[] { typeof(TempFileCollection) };
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.SafeMode, expectedTypes: expectedTypes), "In safe mode it is not supported to deserialize type \"System.CodeDom.Compiler.TempFileCollection\"");
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

            // In SafeMode this cannot be deserialized even in .NET Framework, even if we allow StructuralEqualityComparer explicitly
            var expectedTypes = new[] { typeof(Hashtable), StructuralComparisons.StructuralEqualityComparer.GetType() };
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes), "In safe mode it is not supported to deserialize type \"System.Collections.StructuralEqualityComparer\"");

            // But in non-safe mode actually it can be deserialized without any problem if ignoring the ISerializable implementation of the Hashtable
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.IgnoreISerializable, safeCompare: true);
        }

        [Test]
        public void HashtableResolveByNameTest()
        {
            // In .NET Core 2.x there are 2 Hashtable classes: an internal one in System.Private.CoreLib and a public one in System.Runtime.Extensions
            // and unfortunately the mscorlib TypeForwardedToAttribute points to the internal implementation so resolving by name must be performed very carefully.
            var obj = new Hashtable { { 1, "alpha" } };
            KGySerializeObject(obj, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
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

            // In SafeMode this cannot be deserialized even in .NET Framework, even if we allow StructuralEqualityComparer explicitly
            var expectedTypes = new[] { typeof(Hashtable), StructuralComparisons.StructuralEqualityComparer.GetType() };
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.SafeMode | BinarySerializationOptions.AllowNonSerializableExpectedCustomTypes, expectedTypes: expectedTypes), "In safe mode it is not supported to deserialize type \"System.Collections.StructuralEqualityComparer\"");

            // But in non-safe mode actually it can be deserialized without any problem if ignoring the ISerializable implementation of the Hashtable
            KGySerializeObjects(referenceObjects, BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes | BinarySerializationOptions.IgnoreISerializable, safeCompare: true);
        }
#endif

        [Test]
        public void SafeModeArrayOutOfMemoryAttackTest()
        {
            byte[] array = { 1, 2, 3, 4, 5, 6, 7 };
            var bsf = new BinarySerializationFormatter(BinarySerializationOptions.None);
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
            var bsf = new BinarySerializationFormatter(BinarySerializationOptions.None);
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
            var bsf = new BinarySerializationFormatter(BinarySerializationOptions.None);
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
            Throws<SerializationException>(() => DeserializeObject(manipulatedData, bsf), "Invalid stream data.");
        }

#if NETCOREAPP3_0_OR_GREATER
        [TestCase<int>]
        [TestCase<Point>]
        [TestCase<ConsoleColor>]
        [TestCase<Dictionary<string, ConsoleColor>>]
        [TestCase<SystemSerializableClass>]
#else
        [TestCaseGeneric(TypeArguments = new[] { typeof(int) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(Point) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(ConsoleColor) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(Dictionary<string, ConsoleColor>) })]
        [TestCaseGeneric(TypeArguments = new[] { typeof(SystemSerializableClass) })]
#endif
        public void SafeModeGenericTest<T>()
        {
            T instance = ThreadSafeRandom.Instance.NextObject<T>();
            Console.WriteLine($"{typeof(T).GetName(TypeNameKind.ShortName)}: {instance.Dump()}");

            byte[] raw = BinarySerializer.Serialize(instance);
            T clone = BinarySerializer.Deserialize<T>(raw);
            AssertDeepEquals(instance, clone);
        }

        [Test]
        public void ExtractExpectedTypesTest()
        {
            // primitive type: nothing, unless forced
            Assert.AreEqual(0, BinarySerializer.ExtractExpectedTypes<int>().Count());
            Assert.AreEqual(1, BinarySerializer.ExtractExpectedTypes<int>(forceAll: true).Count());

            // natively supported type: the possible unknown generic parameters only, unless forced
            Assert.AreEqual(0, BinarySerializer.ExtractExpectedTypes<Dictionary<int, string>>().Count());
            Assert.AreEqual(1, BinarySerializer.ExtractExpectedTypes<Dictionary<int, ConsoleColor>>().Count());
            Assert.Greater(BinarySerializer.ExtractExpectedTypes<Dictionary<int, string>>(forceAll: true).Count(), 0);

            // default [Serializable] unknown type: contained unknown fields recursively
            IEnumerable<Type> types = BinarySerializer.ExtractExpectedTypes<SystemSerializableClass>();
            Assert.AreEqual(2, types.Count());
            Assert.IsTrue(types.Contains(typeof(SystemSerializableClass)));
            Assert.IsTrue(types.Contains(typeof(ConsoleColor)));
            Assert.Greater(BinarySerializer.ExtractExpectedTypes<SystemSerializableClass>(forceAll: true).Count(), 2);

            // non-serializable or I[Binary]Serializable type: the root only, unless forced
            Assert.AreEqual(1, BinarySerializer.ExtractExpectedTypes<NonSerializableClass>().Count());
            Assert.AreEqual(1, BinarySerializer.ExtractExpectedTypes<CustomSerializedClass>().Count());
            Assert.AreEqual(1, BinarySerializer.ExtractExpectedTypes<BinarySerializableClass>().Count());
        }

        #endregion
    }
}
