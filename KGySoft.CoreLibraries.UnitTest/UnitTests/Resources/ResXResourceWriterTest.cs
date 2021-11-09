#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceWriterTest.cs
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



#region Used Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
#if NET
using System.Diagnostics.CodeAnalysis;
#endif
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NET35
using System.Numerics;
#endif
using System.Runtime.Serialization;
using System.Text;
#if NETFRAMEWORK
using System.Windows.Forms; 
#endif

using KGySoft.Collections;
using KGySoft.Drawing;
using KGySoft.ComponentModel;
using KGySoft.Reflection;
using KGySoft.Resources;
using KGySoft.Serialization.Binary;

using NUnit.Framework;
#if NETFRAMEWORK
using NUnit.Framework.Internal;
#endif

#endregion

#region Used Aliases

#if NETFRAMEWORK
using SystemResXResourceReader = System.Resources.ResXResourceReader;
using SystemResXResourceWriter = System.Resources.ResXResourceWriter;
#endif

#endregion

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Resources
{
    [TestFixture]
    public class ResXResourceWriterTest : TestBase
    {
        #region Nested types

        #region Enumerations

        private enum TestEnum : byte
        {
        }

        #endregion

        #region Nested classes

        #region ByteListConverter class

        private class ByteListConverter : TypeConverter
        {
            #region Methods

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => (destinationType == typeof(string)) || base.CanConvertTo(context, destinationType);

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string) && value is List<byte> bytes)
                    return bytes.ToArray().ToDecimalValuesString();

                if (destinationType == typeof(string) && value is HashSet<byte> hashbytes)
                    return "H" + hashbytes.ToArray().ToDecimalValuesString();

                if (destinationType == typeof(string) && value is List<TestEnum> enums)
                    return "E" + enums.Select(e => (byte)e).ToArray().ToDecimalValuesString();

                if (destinationType == typeof(string) && value is HashSet<TestEnum> hashenums)
                    return "X" + hashenums.Select(e => (byte)e).ToArray().ToDecimalValuesString();

                return base.ConvertTo(context, culture, value, destinationType);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                => (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null)
                    return null;

                if (value is string str)
                {
                    if (str[0] == 'H')
                        return new HashSet<byte>(str.Substring(1).ParseDecimalBytes(","));
                    if (str[0] == 'X')
                        return new HashSet<TestEnum>(str.Substring(1).ParseDecimalBytes(",").Select(b => (TestEnum)b));
                    if (str[0] == 'E')
                        return new List<TestEnum>(str.Substring(1).ParseDecimalBytes(",").Select(b => (TestEnum)b));
                    return new List<byte>(str.ParseDecimalBytes(","));
                }
                return base.ConvertFrom(context, culture, value);
            }

            #endregion
        }

        #endregion

        #region NonSerializableClass class

        private class NonSerializableClass
        {
            #region Methods

            public override bool Equals(object obj)
            {
                if (obj.GetType() == typeof(NonSerializableClass))
                    return true;
                return base.Equals(obj);
            }

            public override int GetHashCode() => true.GetHashCode();

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        private static void ReadWriteReadResX(string path, bool generateAliases, bool compatibilityMode)
        {
            // read from file
            List<DictionaryEntry> reference, check;
            string basePath = Path.GetDirectoryName(path);
            using (ResXResourceReader reader = new ResXResourceReader(path) { BasePath = basePath, SafeMode = true })
            {
                // reference contains now string-ResXDataNode elements
                reference = reader.Cast<DictionaryEntry>().ToList();
            }

            // write to string: from ResXDataNodes without generated values
            StringBuilder sb = new StringBuilder();
            using (ResXResourceWriter writer = new ResXResourceWriter(new StringWriter(sb)) { AutoGenerateAlias = generateAliases, CompatibleFormat = compatibilityMode })
            {
                reference.ForEach(e => writer.AddResource(e.Key.ToString(), e.Value));
            }

            // re-read from string
            using (ResXResourceReader reader = ResXResourceReader.FromFileContents(sb.ToString()))
            {
                reader.BasePath = basePath;
                // check contains now string-object elements
                check = reader.Cast<DictionaryEntry>().ToList();
            }

            // compare 1: check is from ResXDataNodes objects with original DataNodeInfos and without generated values
            AssertItemsEqual(reference.Select(de => new DictionaryEntry(de.Key, ((ResXDataNode)de.Value).GetValue())), check);

            // -----------------

            // write to string: from objects (fileref resources will be embedded now)
            sb = new StringBuilder();
            using (ResXResourceWriter writer = new ResXResourceWriter(new StringWriter(sb)) { AutoGenerateAlias = generateAliases, CompatibleFormat = compatibilityMode })
            {
                // cleaning up nodes during the compare so DataNodeInfos will be nullified in reference
                reference.ForEach(de => writer.AddResource(de.Key.ToString(), ((ResXDataNode)de.Value).GetValue(cleanupRawData: true)));
            }

            // re-read from string
            using (ResXResourceReader reader = ResXResourceReader.FromFileContents(sb.ToString()))
            {
                // no base path is needed because there are no filerefs
                // check contains now string-object elements
                check = reader.Cast<DictionaryEntry>().ToList();
            }

            // compare 2: check is from objects so DataNodeInfos are generated, every object is embedded
            AssertItemsEqual(reference.Select(de => new DictionaryEntry(de.Key, ((ResXDataNode)de.Value).GetValue())), check);

            // -----------------

            // write to string: from ResXDataNodes with nullified DataNodeInfos
            sb = new StringBuilder();
            using (ResXResourceWriter writer = new ResXResourceWriter(new StringWriter(sb)) { AutoGenerateAlias = generateAliases, CompatibleFormat = compatibilityMode })
            {
                // DataNodeInfos will be now re-generated in ResXDataNodes
                reference.ForEach(de => writer.AddResource(de.Key.ToString(), de.Value));
            }

            // re-read from string
            using (ResXResourceReader reader = ResXResourceReader.FromFileContents(sb.ToString()))
            {
                reader.BasePath = basePath;
                // check contains now string-object elements
                check = reader.Cast<DictionaryEntry>().ToList();
            }

            // compare 3: check is from ResXDataNodes objects with re-generated DataNodeInfos from values
            AssertItemsEqual(reference.Select(de => new DictionaryEntry(de.Key, ((ResXDataNode)de.Value).GetValue())), check);
        }

#if NETFRAMEWORK
        private static void SystemSerializeObjects(object[] referenceObjects, Func<Type, string> typeNameConverter = null, ITypeResolutionService typeResolver = null)
        {
            using (new TestExecutionContext.IsolatedContext())
            {
                Console.WriteLine($"------------------System ResXResourceWriter (Items Count: {referenceObjects.Length})--------------------");
                try
                {
                    StringBuilder sb = new StringBuilder();
                    using (SystemResXResourceWriter writer =
#if NET35
                        new SystemResXResourceWriter(new StringWriter(sb))

#else
                        new SystemResXResourceWriter(new StringWriter(sb), typeNameConverter)
#endif

                        )
                    {
                        int i = 0;
                        foreach (object item in referenceObjects)
                        {
                            writer.AddResource(i++ + "_" + (item == null ? "null" : item.GetType().Name), item);
                        }
                    }

                    Console.WriteLine(sb.ToString());
                    List<object> deserializedObjects = new List<object>();
                    using (SystemResXResourceReader reader = SystemResXResourceReader.FromFileContents(sb.ToString(), typeResolver))
                    {
                        foreach (DictionaryEntry item in reader)
                        {
                            deserializedObjects.Add(item.Value);
                        }
                    }

                    AssertItemsEqual(referenceObjects, deserializedObjects.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"System serialization failed: {e}");
                }
            }  
        }
#endif

        private static void KGySerializeObjects(object[] referenceObjects, bool compatibilityMode = true, bool checkCompatibleEquality = true, Func<Type, string> typeNameConverter = null, ITypeResolutionService typeResolver = null, bool safeMode = true)
        {
            Console.WriteLine($"------------------KGySoft ResXResourceWriter (Items Count: {referenceObjects.Length}; Compatibility mode: {compatibilityMode})--------------------");
            StringBuilder sb = new StringBuilder();
            using (ResXResourceWriter writer = new ResXResourceWriter(new StringWriter(sb), typeNameConverter) { CompatibleFormat = compatibilityMode })
            {
                int i = 0;
                foreach (object item in referenceObjects)
                {
                    writer.AddResource(i++ + "_" + (item == null ? "null" : item.GetType().Name), item);
                }
            }

            Console.WriteLine(sb.ToString());
            List<object> deserializedObjects = new List<object>();
            using (ResXResourceReader reader = ResXResourceReader.FromFileContents(sb.ToString(), typeResolver))
            {
                reader.SafeMode = safeMode;
                foreach (DictionaryEntry item in reader)
                {
                    deserializedObjects.Add(safeMode ? ((ResXDataNode)item.Value)!.GetValueSafe() : item.Value);
                }
            }

            AssertItemsEqual(referenceObjects, deserializedObjects.ToArray());

#if NETFRAMEWORK
            if (compatibilityMode)
            {
                deserializedObjects.Clear();
                using (SystemResXResourceReader reader = SystemResXResourceReader.FromFileContents(sb.ToString(), typeResolver))
                {
                    try
                    {
                        foreach (DictionaryEntry item in reader)
                        {
                            deserializedObjects.Add(item.Value);
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"System serialization failed: {e}");
                        Console.WriteLine("Skipping equality check");
                        return;
                    }
                }

                if (checkCompatibleEquality)
                    AssertItemsEqual(referenceObjects, deserializedObjects.ToArray());
            } 
#endif
        }

        #endregion

        #region Instance Methods

        [Test]
        public void ReadWriteRead()
        {
            string path = Combine(Files.GetExecutingPath(), "Resources", "TestRes.resx");
            ReadWriteReadResX(path, true, true);
            ReadWriteReadResX(path, false, true);
            ReadWriteReadResX(path, true, false);

#if NETFRAMEWORK
            typeof(Image).RegisterTypeConverter<AdvancedImageConverter>();
            path = Combine(Files.GetExecutingPath(), "Resources", "TestResourceResX.resx");
            ReadWriteReadResX(path, true, true);
            ReadWriteReadResX(path, false, true);
            ReadWriteReadResX(path, true, false);
#endif
        }

        [Test]
        public void SerializeNativelySupportedTypes()
        {
            object[] referenceObjects =
            {
                null,
                new object(),
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
                "dummy",
                (float)1,
                (double)1,
                (decimal)1,
                new IntPtr(1),
                new UIntPtr(1),
#if !NET35
                new BigInteger(1),
#endif
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#else
            // preloading BigInteger original identity for safe deserialization in compatibility mode
            Reflector.ResolveAssembly("System.Numerics, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089");
#endif
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void SerializeEnums()
        {
            Enum[] referenceObjects =
            {
                ConsoleColor.White, // mscorlib enum
                ConsoleColor.Black, // mscorlib enum

                UriKind.Absolute, // System enum
                UriKind.Relative, // System enum

                HandleInheritability.Inheritable, // System.Core enum

                ActionTargets.Default, // NUnit.Framework enum

                BinarySerializationOptions.RecursiveSerializationAsFallback, // KGySoft.CoreLibraries enum
                BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.IgnoreIObjectReference, // KGySoft.CoreLibraries enum, multiple flags

#pragma warning disable 618
                BinarySerializationOptions.ForcedSerializationValueTypesAsFallback, // KGySoft.CoreLibraries enum, obsolete element
#pragma warning restore 618
                (BinarySerializationOptions)(-1), // KGySoft.Libraries enum, non-existing value

            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects);
        }

        [Test]
        public void SerializeFloatingPointNumbers()
        {
            object[] referenceObjects =
            {
                +0.0f,
                -0.0f,
                Single.NegativeInfinity,
                Single.PositiveInfinity,
                Single.NaN,
                Single.MinValue,
                Single.MaxValue,

                +0.0d,
                -0.0d,
                Double.NegativeInfinity,
                Double.PositiveInfinity,
                Double.NaN,
                Double.MinValue,
                Double.MaxValue,

                +0m,
                -0m,
                +0.0m,
                -0.0m,
                +0.00m,
                -0.00m,
                Decimal.MinValue,
                Decimal.MaxValue,

#if NET5_0_OR_GREATER
                (Half)(+0.0f),
                (Half)(-0.0f),
                Half.NegativeInfinity,
                Half.PositiveInfinity,
                Half.NaN,
                Half.MinValue,
                Half.MaxValue,
#endif
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects, true, false); // the system serializer cannot deserialize the -0 correctly
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void SerializeDateAndTime()
        {
            // DateTime(Offset): utc/local, min/max
            // These types cannot be serialized with system serializer: it is not precise enough and deserialized Kind is always Local
            var referenceObjects = new object[]
            {
                DateTime.Now,
                DateTime.UtcNow,
                DateTime.MinValue,
                DateTime.MaxValue,
                new DateTimeOffset(DateTime.Now),
                new DateTimeOffset(DateTime.UtcNow),
                new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)),
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue,
                new TimeSpan(1, 2, 3, 4, 5),
#if NET6_0_OR_GREATER
                DateOnly.FromDateTime(DateTime.Today),
                TimeOnly.FromDateTime(DateTime.Now),
#endif
            };

            KGySerializeObjects(referenceObjects, true, false);
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void SerializeChars()
        {
            object[] referenceObjects =
            {
                'a',
                'á',
                ' ',
                '\'',
                '<',
                '>',
                '"',
                '{',
                '}',
                '&',
                '\0',
                '\t', // U+0009 = <control> HORIZONTAL TAB
                '\n', // U+000a = <control> LINE FEED
                '\v', // U+000b = <control> VERTICAL TAB
                '\f', // U+000c = <contorl> FORM FEED
                '\r', // U+000d = <control> CARRIAGE RETURN
                '\x85', // U+0085 = <control> NEXT LINE
                '\xa0', // U+00a0 = NO-BREAK SPACE
                '\xFDD0', // U+FDD0 - <noncharacter-FDD0>
                '\xffff', // U+FFFF = <noncharacter-FFFF>
                ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '​', '\u2028', '\u2029', '　', '﻿',
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects);

            // system serializer fails here
            referenceObjects = new object[]
            {
                Char.ConvertFromUtf32(0x1D161)[0], // unpaired surrogate
            };

            KGySerializeObjects(referenceObjects, true);
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void SerializeStrings()
        {
            string[] referenceObjects =
            {
                null,
                String.Empty,
                "One",
                "Two",
                " space ",
                "space after ",
                "space  space",
                "<>\\'\"&{}{{}}",
                "tab\ttab",
                Environment.NewLine,
                "\0",
                "\r",
                "\n",
                "x\r\rx",
                "x\n\nx",
                " ",
                "\t",
                @"new

                    lines  ",
                "<>\\'\"&{}{{}}\0\\0000",
                "\xffff", // U+FFFF = <noncharacter-FFFF>
                "🏯", // paired surrogate
                new string(new char[] { '\t', '\n', '\v', '\f', '\r', ' ', '\x0085', '\x00a0', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '​', '\u2028', '\u2029', '　', '﻿' }),
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects);

            // These strings cannot be (de)serialized with system serializer
            referenceObjects = new string[]
            {
                "🏯"[0].ToString(null), // unpaired surrogate
                "🏯" + "🏯"[0].ToString(null) + " b 🏯 " + "🏯"[1].ToString(null) + "\xffff \0 <>'\"&" // string containing unpaired surrogates
            };

            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

#if NETCOREAPP3_0_OR_GREATER && !NETSTANDARD_TEST
        [Test]
        public void SerializeRunes()
        {
            object[] referenceObjects =
            {
                new Rune('a'),
                Rune.GetRuneAt("🏯", 0)
            };

            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }
#endif

        [Test]
        public void SerializeTypes()
        {
            Type[] referenceObjects =
            {
                typeof(int), // mscorlib
                typeof(int).MakeByRefType(), // mscorlib
                typeof(int).MakePointerType(), // mscorlib
                typeof(List<int>), // mscorlib
                typeof(List<ICache>), // mixed
                typeof(ICache), // custom
                typeof(CircularList<int>), // mixed
                typeof(CircularList<ICache>), // custom
                typeof(List<>), // mscorlib, generic template
                typeof(int[]), // 1D zero based array
                typeof(int[,]), // multi-dim array
                typeof(int[][,]), // mixed jagged array
                Array.CreateInstance(typeof(int), new[] { 3 }, new[] { -1 }).GetType(), // nonzero based 1D array
                typeof(List<>).GetGenericArguments()[0] // generic type parameter
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void SerializeByTypeConverter()
        {
#if !NETCOREAPP3_0_OR_GREATER
            typeof(Version).RegisterTypeConverter<VersionConverter>();
#endif
            typeof(Encoding).RegisterTypeConverter<EncodingConverter>();
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
            typeof(Image).RegisterTypeConverter<AdvancedImageConverter>();
#endif
#if NETCOREAPP
            Assembly.Load("System.Drawing"); // preloading assembly for safe mode
#endif

            object[] referenceObjects =
            {
                // built-in
                new Guid("ca761232ed4211cebacd00aa0057b223"),
                new Point(13, 13),
                new Uri(@"x:\teszt"),
                new Uri("ftp://myUrl/%2E%2E/%2E%2E"),
                Color.Blue,
                StringSegment.Empty,
#if !NETFRAMEWORK // System serializer dumps <value/> both for null and empty, and reads empty string for both
                StringSegment.Null,
#endif

                // special handling to escape built-in
                CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("en"),
                CultureInfo.GetCultureInfo("en-US"),

                // partly working built-in
#if NETFRAMEWORK
                Cursors.Arrow, // a default cursor: by string
#endif

#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
                Icons.Information, // multi-resolution icon (built-in saves one page only)
                Icons.Information.ToMultiResBitmap(), // multi-resolution bitmap-icon (built-in saves one page only)  
#if WINDOWS
                CreateTestTiff(), // multipage TIFF (built-in saves first page only)
                CreateTestMetafile(), // EMF image (built-in saves it as a PNG)  
#endif
#endif
                // pure custom
                new Version(1, 2, 3, 4),
#if !NET
                Encoding.UTF7,
#endif
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#else
            // To be able to resolve Uri in safe mode
            Reflector.ResolveAssembly("System");
#endif

            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
#if NET
        [SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Serialization test, deserialized instance will be a new instance anyway")]
#endif
        public void SerializeByteArrays()
        {
            IList[] referenceObjects =
            {
                new byte[0], // empty array
                new byte[] { 1, 2, 3 }, // single byte array
                new byte[][] { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23, 24, 25 }, null }, // jagged byte array
                new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, // multidimensional byte array
                new byte[][,] { new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, new byte[,] { { 11, 12, 13, 14 }, { 21, 22, 23, 24 }, { 31, 32, 33, 34 } } }, // crazy jagged byte array 1 (2D matrix of 1D arrays)
                new byte[,][] { { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23 } }, { new byte[] { 11, 12, 13, 14 }, new byte[] { 21, 22, 23, 24 } } }, // crazy jagged byte array 2 (1D array of 2D matrices)
                new byte[][,,] { new byte[,,] { { { 11, 12, 13 }, { 21, 21, 23 } } }, null }, // crazy jagged byte array containing null reference
                Array.CreateInstance(typeof(byte), new int[] { 3 }, new int[] { -1 }), // array with -1..1 index interval
                Array.CreateInstance(typeof(byte), new int[] { 3, 3 }, new int[] { -1, 1 }) // array with [-1..1 and 1..3] index interval
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
#if !NETCOREAPP3_0
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
#else // .NET Core 3.0 fails to deserialize empty byte array - returns null instead
            KGySerializeObjects(referenceObjects, checkCompatibleEquality: false);
            KGySerializeObjects(referenceObjects, false, false);
#endif
        }

        /// <summary>
        /// String has variable length and can be null.
        /// </summary>
        [Test]
        public void SerializeStringArrays()
        {
            Array[] referenceObjects =
            {
                new string[] { "One", "Two" }, // single string array
                new string[][] { new string[] { "One", "Two", "Three" }, new string[] { "One", "Two", null }, null }, // jagged string array with null values (first null as string, second null as array)
                new string[,] { { "One", "Two" }, { "One", "Two" } }, // multidimensional string array
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);

            // system serializer (and also compatible mode) fails here: cannot cast string[*] to object[]
            referenceObjects = new[]
            {
                Array.CreateInstance(typeof(string), new int[] { 3 }, new int[] { -1 }) // array with -1..1 index interval
            };

            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
#if NET
        [SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Serialization test, deserialized instance will be a new instance anyway")]
#endif
        public void SerializeSimpleArrays()
        {
            IList[] referenceObjects =
            {
                new object[0],
                new object[] {new object(), null},
                new bool[] {true, false},
                new sbyte[] {1, 2},
                new byte[] {1, 2},
                new short[] {1, 2},
                new ushort[] {1, 2},
                new int[] {1, 2},
                new uint[] {1, 2},
                new long[] {1, 2},
                new ulong[] {1, 2},
                new char[] {'a', Char.ConvertFromUtf32(0x1D161)[0]}, //U+1D161 = MUSICAL SYMBOL SIXTEENTH NOTE, serializing its low-surrogate
                new string[] {"dummy", null},
                new float[] {1, 2},
                new double[] {1, 2},
                new decimal[] {1, 2},
                new DateTime[] {DateTime.UtcNow, DateTime.Now},
                new IntPtr[] {new IntPtr(1), IntPtr.Zero},
                new UIntPtr[] {new UIntPtr(1), UIntPtr.Zero},
            };

            // SystemSerializeObjects(referenceObjects); - system serialization fails for sbyte[] and char[]
            //KGySerializeObjects(referenceObjects); //- assert check fails for char[] because BinaryFormatter cannot handle it correctly
            KGySerializeObjects(referenceObjects, false);
        }

        /// <summary>
        /// Generic types with type converter: the generic type name is dumped into the type attribute
        /// </summary>
        [Test]
        public void SerializeGenericTypesWithTypeConverter()
        {
            typeof(List<byte>).RegisterTypeConverter<ByteListConverter>();
            typeof(List<TestEnum>).RegisterTypeConverter<ByteListConverter>();
            typeof(HashSet<byte>).RegisterTypeConverter<ByteListConverter>();
            typeof(HashSet<TestEnum>).RegisterTypeConverter<ByteListConverter>();
            IEnumerable[] referenceObjects =
            {
                new List<int> {1, 2, 3}, // no converter - raw
                new List<byte> {1, 2, 3}, // full mscorlib
                new List<TestEnum> {(TestEnum) 1, (TestEnum) 2, (TestEnum) 3}, // mscorlib generic type with custom element

                new HashSet<int> {1, 2, 3}, // no converter - raw
                new HashSet<byte> {1, 2, 3}, // non-mscorlib type with mscorlib element
                new HashSet<TestEnum> {(TestEnum) 1, (TestEnum) 2, (TestEnum) 3}, // full non-mscorlib generic type
            };

            //SystemSerializeObjects(referenceObjects); // system serializer fails on generic types

#if !NETFRAMEWORK
            // To be able to resolve HashSet in safe mode
            Reflector.ResolveAssembly("System.Core");
#endif
            KGySerializeObjects(referenceObjects, true, false); // system reader fails on full non-mscorlib type parsing
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void SerializeNonSerializableType()
        {
            object[] referenceObjects =
            {
                new NonSerializableClass(),
            };

            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects));
            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, false));

            KGySerializeObjects(referenceObjects, safeMode: false);
            KGySerializeObjects(referenceObjects, false, safeMode: false);
        }

        [Test]
        public void SerializeSpecialTypes()
        {
            // these types will be transformed to their wrapped representations
            string path = Combine(Files.GetExecutingPath(), "Resources", "TestRes.resx");
            object[] referenceObjects =
            {
#pragma warning disable 618
                // binary wrapper
                new AnyObjectSerializerWrapper("test", false),
#pragma warning restore 618
#if NETFRAMEWORK
                // legacy formats: KGy version converts these to self formats
                new System.Resources.ResXFileRef(path, TypeResolver.StringTypeFullName),
                new System.Resources.ResXDataNode("TestString", "string"),
                new System.Resources.ResXDataNode("TestRef", new System.Resources.ResXFileRef(path, TypeResolver.StringTypeFullName)),
#endif
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);

            referenceObjects = new object[]
            {
                // self formats: supported only by KGySoft
                new ResXFileRef(path, typeof(string)),
                new ResXDataNode("TestString", "string"),
#if NETFRAMEWORK
                new ResXDataNode("TestRef", new System.Resources.ResXFileRef(path, TypeResolver.StringTypeFullName)),
#endif
            };

            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void SerializeMemoryStream()
        {
            // Starting with .NET Core it is not serializable anymore but still has to be supported even in safe mode
            // to maintain compatibility (as even the VS designer embeds MemoryStream into .resx in some cases)
            object[] referenceObjects =
            {
                new MemoryStream(new byte[] { 1, 2, 3 }),
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [Test]
        public void TestResXSerializationBinder()
        {
            // The ResXSerializationBinder is used during (de)serialization if there is a typeResolver/typeNameConverter for a BinaryFormatted type
            // ReSharper disable once ConvertToLocalFunction - it will be a delegate in the end when passed to the methods
#pragma warning disable IDE0039 // Use local function
            Func<Type, string> typeNameConverter = t => t.AssemblyQualifiedName;
#pragma warning restore IDE0039 // Use local function
            ITypeResolutionService typeResolver = new TestTypeResolver();

            object[] referenceObjects =
            {
#if !NETCOREAPP2_0 // throws PlatformNotSupportedException on .NET Core 2.0
		        DBNull.Value, // type name must not be set -> UnitySerializationHolder is used  
#endif
#if NETFRAMEWORK
                //Encoding.GetEncoding("shift_jis"), // type name must not be set -> encoding type is changed  
#endif
                CultureInfo.CurrentCulture, // special handling for culture info
                new List<int[][,]> // generic type: system ResXSerializationBinder parses it wrongly, but if versions do not change, it fortunately works due to concatenation
                {
                    new int[][,] { new int[,] { { 11, 12 }, { 21, 22 } } }
                }
            };

#if NETFRAMEWORK
            SystemSerializeObjects(referenceObjects);
#endif
            KGySerializeObjects(referenceObjects, true, true, typeNameConverter, typeResolver);
            KGySerializeObjects(referenceObjects, false, true, typeNameConverter, typeResolver);
        }

        #endregion

        #endregion
    }
}
