using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using KGySoft.ComponentModel;
using KGySoft.Drawing;
using KGySoft.Libraries;
using KGySoft.Libraries.Collections;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;
using KGySoft.Libraries.Serialization;

using SystemResXResourceWriter = System.Resources.ResXResourceWriter;
using SystemResXResourceReader = System.Resources.ResXResourceReader;

namespace _LibrariesTest.Libraries.Resources
{
    [TestClass]
    public class ResXResourceWriterTest : TestBase
    {
        private class ByteListConverter: TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return (destinationType == typeof(string)) || base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                List<byte> bytes = value as List<byte>;
                if (destinationType == typeof(string) && bytes != null)
                    return bytes.ToArray().ToDecimalValuesString();

                HashSet<byte> hashbytes = value as HashSet<byte>;
                if (destinationType == typeof(string) && hashbytes != null)
                    return "H" + hashbytes.ToArray().ToDecimalValuesString();

                List<TestEnum> enums = value as List<TestEnum>;
                if (destinationType == typeof(string) && enums != null)
                    return "E" + enums.Select(e => (byte)e).ToArray().ToDecimalValuesString();

                HashSet<TestEnum> hashenums = value as HashSet<TestEnum>;
                if (destinationType == typeof(string) && hashenums != null)
                    return "X" + hashenums.Select(e => (byte)e).ToArray().ToDecimalValuesString();


                return base.ConvertTo(context, culture, value, destinationType);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return (sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value == null)
                    return null;

                string str = value as string;
                if (str != null)
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
        }

        private enum TestEnum : byte {}

        private class NonSerializableClass
        {
            public override bool Equals(object obj)
            {
                if (obj.GetType() == typeof(NonSerializableClass))
                    return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return true.GetHashCode();
            }
        }

        protected override bool IsResourceTest { get { return true; } }

        [TestMethod]
        public void ReadWriteRead()
        {
            string path = Path.Combine(Files.GetExecutingPath(), "Resources\\TestRes.resx");
            ReadWriteReadResX(path, true, true);
            ReadWriteReadResX(path, false, true);
            ReadWriteReadResX(path, true, false);

            Reflector.RegisterTypeConverter<Image, AdvancedImageConverter>();
            path = Path.Combine(Files.GetExecutingPath(), "Resources\\TestResourceResX.resx");
            ReadWriteReadResX(path, true, true);
            ReadWriteReadResX(path, false, true);
            ReadWriteReadResX(path, true, false);
        }

        [TestMethod]
        public void SerializePrimitiveTypes()
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
                "alma",
                (float)1,
                (double)1,
                (decimal)1,
                DBNull.Value,
                new IntPtr(1),
                new UIntPtr(1),
                1.GetType(),
                new TimeSpan(1, 2, 3, 4, 5),
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [TestMethod]
        public void SerializeEnums()
        {
            Enum[] referenceObjects = 
            {
                ConsoleColor.White, // mscorlib enum
                ConsoleColor.Black, // mscorlib enum

                UriKind.Absolute, // System enum
                UriKind.Relative, // System enum

                HandleInheritability.Inheritable, // System.Core enum

                DataAccessMethod.Random, // Microsoft.VisualStudio.QualityTools.UnitTestFramework enum

                BinarySerializationOptions.RecursiveSerializationAsFallback, // KGySoft.Libraries enum
                BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.IgnoreIObjectReference, // KGySoft.Libraries enum, multiple flags

                BinarySerializationOptions.ForcedSerializationValueTypesAsFallback, // KGySoft.Libraries enum, obsolete element
                (BinarySerializationOptions)(-1), // KGySoft.Libraries enum, non-existing value

            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
        }

        [TestMethod]
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
                Decimal.MaxValue
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, true, false); // the system serializer cannot deserialize the -0 correctly
        }

        [TestMethod]
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
                DateTimeOffset.MaxValue
            };

            KGySerializeObjects(referenceObjects, true, false);
        }

        [TestMethod]
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
                '\xffff', // U+FFFF = <noncharacter-FFFF>
                ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '​', '\u2028', '\u2029', '　', '﻿',
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);

            // system serializer fails here
            referenceObjects = new object[] 
            {
                Char.ConvertFromUtf32(0x1D161)[0], // unpaired surrogate
            };

            KGySerializeObjects(referenceObjects, true);
            KGySerializeObjects(referenceObjects, false);
        }

        [TestMethod]
        public void SerializeStrings()
        {
            string[] referenceObjects = 
            {
                null,
                String.Empty,
                "Egy",
                "Kettő",
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
                new string(new char[] { '\t', '\n', '\v', '\f', '\r', ' ', '\x0085', '\x00a0', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '​', '\u2028', '\u2029', '　', '﻿'}),
            };

            SystemSerializeObjects(referenceObjects);
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

        [TestMethod]
        public void SerializeTypes()
        {
            Type[] referenceObjects = 
            {
                typeof(int),                                // mscorlib
                typeof(int).MakeByRefType(),                // mscorlib
                typeof(int).MakePointerType(),              // mscorlib
                typeof(List<int>),                          // mscorlib
                typeof(List<ICache>),                       // mixed
                typeof(ICache),                             // custom
                typeof(CircularList<int>),                  // mixed
                typeof(CircularList<ICache>),               // custom
                typeof(List<>),                             // mscorlib, generic template
                typeof(int[]),                              // 1D zero based array
                typeof(int[,]),                             // multi-dim array
                typeof(int[][,]),                           // mixed jagged array
                Array.CreateInstance(typeof(int),new[]{3},new[]{-1}).GetType(), // nonzero based 1D array
                typeof(List<>).GetGenericArguments()[0]     // this can be only binary serialized
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [TestMethod]
        public void SerializeByTypeConverter()
        {
            Reflector.RegisterTypeConverter<Version, VersionConverter>();
            Reflector.RegisterTypeConverter<Encoding, EncodingConverter>();
            Reflector.RegisterTypeConverter<Image, AdvancedImageConverter>();
            CursorHandle cursor = Images.Information.ToCursorHandle();

            object[] referenceObjects =
            {
                // built-in
                new Guid("ca761232ed4211cebacd00aa0057b223"),
                new Point(13, 13),
                new Uri(@"x:\teszt"),
                new Uri("ftp://myUrl/%2E%2E/%2E%2E"),
                Color.Blue,

                // special handling to escape built-in
                CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("en"),
                CultureInfo.GetCultureInfo("en-US"),

                // partly working built-in
                Cursors.Arrow, // a default cursor: by string
                //new Cursor(cursor), // custom cursor: exception is thrown both for string and byte[] conversion

                // built-in is replaced by custom
                Icons.Information, // multi-resolution icon (built-in saves one page only)
                Images.InformationMultiSize, // multi-resolution bitmap-icon (built-in saves one page only)
                CreateTestTiff(), // multipage TIFF (built-in saves first page only)
                CreateTestMetafile(), // EMF image (built-in saves it as a PNG)

                // pure custom
                new Version(1, 2, 3, 4),
                Encoding.UTF7,
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
            cursor.Dispose();
        }

        [TestMethod]
        public void SerializeByteArrays()
        {
            IList[] referenceObjects = 
            {
                new byte[0], // empty array
                new byte[] { 1, 2, 3}, // single byte array
                new byte[][] { new byte[] {11, 12, 13}, new byte[] {21, 22, 23, 24, 25}, null }, // jagged byte array
                new byte[,] { {11, 12, 13}, {21, 22, 23} }, // multidimensional byte array
                new byte[][,] { new byte[,] {{11, 12, 13}, {21, 22, 23}}, new byte[,] {{11, 12, 13, 14}, {21, 22, 23, 24}, {31, 32, 33, 34}} }, // crazy jagged byte array 1 (2D matrix of 1D arrays)
                new byte[,][] { {new byte[] {11, 12, 13}, new byte[] { 21, 22, 23}}, { new byte[] {11, 12, 13, 14}, new byte[] {21, 22, 23, 24}} }, // crazy jagged byte array 2 (1D array of 2D matrices)
                new byte[][,,] { new byte[,,] { { {11, 12, 13}, {21, 21, 23} } }, null }, // crazy jagged byte array containing null reference
                Array.CreateInstance(typeof(byte), new int[] {3}, new int[]{-1}), // array with -1..1 index interval
                Array.CreateInstance(typeof(byte), new int[] {3, 3}, new int[]{-1, 1}) // array with [-1..1 and 1..3] index interval
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        /// <summary>
        /// String has variable length and can be null.
        /// </summary>
        [TestMethod]
        public void SerializeStringArrays()
        {
            Array[] referenceObjects = 
            {
                new string[] { "Egy", "Kettő" }, // single string array
                new string[][] { new string[] {"Egy", "Kettő", "Három"}, new string[] {"One", "Two", null}, null }, // jagged string array with null values (first null as string, second null as array)
                new string[,] { {"Egy", "Kettő"}, {"One", "Two"} }, // multidimensional string array
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);

            // system serializer fails here: cannot cast string[*] to object[]
            referenceObjects = new[]
            {
                Array.CreateInstance(typeof(string), new int[] {3}, new int[]{-1}) // array with -1..1 index interval
            };

            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [TestMethod]
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
                    new string[] {"alma", null},
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
        [TestMethod]
        public void SerializeGenericTypesWithTypeConverter()
        {
            Reflector.RegisterTypeConverter<List<byte>, ByteListConverter>();
            Reflector.RegisterTypeConverter<List<TestEnum>, ByteListConverter>();
            Reflector.RegisterTypeConverter<HashSet<byte>, ByteListConverter>();
            Reflector.RegisterTypeConverter<HashSet<TestEnum>, ByteListConverter>();
            IEnumerable[] referenceObjects =
                {
                    new List<int> { 1, 2, 3 }, // no converter - raw
                    new List<byte> { 1, 2, 3}, // full mscorlib
                    new List<TestEnum> { (TestEnum)1, (TestEnum)2, (TestEnum)3}, // mscorlib generic type with custom element

                    new HashSet<int> { 1, 2, 3 },  // no converter - raw
                    new HashSet<byte> { 1, 2, 3},  // non-mscorlib type with mscorlib element
                    new HashSet<TestEnum> { (TestEnum)1, (TestEnum)2, (TestEnum)3}, // full non-mscorlib generic type
                };

            //SystemSerializeObjects(referenceObjects); // system serializer fails on generic types
            //KGySerializeObjects(referenceObjects, true, false); // system reader fails on full non-mscorlib type parsing
            KGySerializeObjects(referenceObjects, false);
        }

        [TestMethod]
        public void SerializeNonSerializableType()
        {
            // - winforms.FileRef/ResXDataNode - valszeg külön teszt, mert az egyenlőség nem fog stimmelni
            object[] referenceObjects =
            {
                new NonSerializableClass(), 
            };

            // SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [TestMethod]
        public void SerializeSpecialTypes()
        {
            // these types will be transformed to their wrapped representations
            string path = Path.Combine(Files.GetExecutingPath(), "Resources\\TestRes.resx");
            object[] referenceObjects =
            {
                // binary wrapper
                new AnyObjectSerializerWrapper("test", false), 

                // legacy formats: KGy version converts these to self formats
                new System.Resources.ResXFileRef(path, "System.String"), 
                new System.Resources.ResXDataNode("TestString", "string"), 
                new System.Resources.ResXDataNode("TestRef", new System.Resources.ResXFileRef(path, "System.String")),
            };

            SystemSerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);

            referenceObjects = new object[]
            {
                // self formats: supported only by KGySoft
                new ResXFileRef(path, typeof(string)),
                new ResXDataNode("TestString", "string"), 
                new ResXDataNode("TestRef", new System.Resources.ResXFileRef(path, "System.String")),
            };

            KGySerializeObjects(referenceObjects);
            KGySerializeObjects(referenceObjects, false);
        }

        [TestMethod]
        public void TestResXSerializationBinder()
        {
            // The ResXSerializationBinder is used during (de)serialization if there is a typeResolver/typeNameConverter for a BinaryFormatted type
            object[] referenceObjects =
            {
                DBNull.Value, // type name must not be set -> UnitySerializationHolder is used
                Encoding.GetEncoding("shift_jis"), // type name must not be set -> encoding type is changed
                CultureInfo.CurrentCulture, // special handling for culture info
                new List<int[][,]> // generic type: system ResXSerializationBinder parses it wrongly, but if versions do not change, it fortunately works due to concatenation
                {
                    new int[][,] { new int[,]{ { 11, 12}, { 21, 22 } } }
                }
            };

            Func<Type, string> typeNameConverter = t => t.AssemblyQualifiedName;
            ITypeResolutionService typeResolver = new TypeResolver();
            SystemSerializeObjects(referenceObjects, typeNameConverter, typeResolver);
            KGySerializeObjects(referenceObjects, true, true, typeNameConverter, typeResolver);
            KGySerializeObjects(referenceObjects, false, true, typeNameConverter, typeResolver);
        }

        private void ReadWriteReadResX(string path, bool generateAliases, bool compatibilityMode)
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
            CompareCollections(reference.Select(de => new DictionaryEntry(de.Key, ((ResXDataNode)de.Value).GetValue())), check);

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
            CompareCollections(reference.Select(de => new DictionaryEntry(de.Key, ((ResXDataNode)de.Value).GetValue())), check);

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
            CompareCollections(reference.Select(de => new DictionaryEntry(de.Key, ((ResXDataNode)de.Value).GetValue())), check);
        }

        private void SystemSerializeObjects(object[] referenceObjects, Func<Type, string> typeNameConverter = null, ITypeResolutionService typeResolver = null)
        {
            Console.WriteLine("------------------System ResXResourceWriter (Items Count: {0})--------------------", referenceObjects.Length);
            try
            {
                StringBuilder sb = new StringBuilder();
                using (SystemResXResourceWriter writer =
#if NET35
                    new SystemResXResourceWriter(new StringWriter(sb))

#elif NET40 || NET45
                    new SystemResXResourceWriter(new StringWriter(sb), typeNameConverter)
#else
#error Unsupported .NET version
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

                CompareCollections(referenceObjects, deserializedObjects.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine("System serialization failed: {0}", e);
            }
        }

        private void KGySerializeObjects(object[] referenceObjects, bool compatibilityMode = true, bool checkCompatibleEquality = true, Func<Type, string> typeNameConverter = null, ITypeResolutionService typeResolver = null)
        {
            Console.WriteLine("------------------KGySoft ResXResourceWriter (Items Count: {0}; Compatibility mode: {1})--------------------", referenceObjects.Length, compatibilityMode);
            StringBuilder sb = new StringBuilder();
            using (ResXResourceWriter writer = new ResXResourceWriter(new StringWriter(sb), typeNameConverter){ CompatibleFormat = compatibilityMode})
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
                foreach (DictionaryEntry item in reader)
                {
                    deserializedObjects.Add(item.Value);
                }
            }

            CompareCollections(referenceObjects, deserializedObjects.ToArray());

            if (compatibilityMode)
            {
                deserializedObjects.Clear();
                using (SystemResXResourceReader reader = SystemResXResourceReader.FromFileContents(sb.ToString(), typeResolver))
                {
                    foreach (DictionaryEntry item in reader)
                    {
                        deserializedObjects.Add(item.Value);
                    }
                }

                if (checkCompatibleEquality)
                    CompareCollections(referenceObjects, deserializedObjects.ToArray());
            }
        }
    }
}
