#if !NET35
#endif
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using KGySoft.Collections;
using KGySoft.ComponentModel;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KGyXmlSerializer = KGySoft.Serialization.XmlSerializer;
using SystemXmlSerializer = System.Xml.Serialization.XmlSerializer;

namespace _LibrariesTest.Tests.Serialization
{
    /// <summary>
    /// Test for XmlSerializer
    /// </summary>
    [TestClass]
    public class XmlSerializerTest: TestBase
    {
        // ReSharper disable CoVariantArrayConversion

        #region Types used in tests

        public class EmptyType
        {
            public override bool Equals(object obj) => true;
            public override int GetHashCode() => 0;
        }

        public class IntList : List<int>, ICollection<int>
        {
            public bool IsReadOnly => false;
            bool ICollection<int>.IsReadOnly => (this as ICollection<int>).IsReadOnly;
        }

        [Serializable]
        private class CustomGenericCollection<T> : List<T>
        {
        }


        [Serializable]
        private class CustomNonGenericCollection : ArrayList
        {
        }

        [Serializable]
        private class CustomGenericDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public CustomGenericDictionary()
            {
            }

            public CustomGenericDictionary(SerializationInfo info, StreamingContext context) :
                base(info, context)
            {
            }
        }

        [Serializable]
        private class CustomNonGenericDictionary : Hashtable
        {
            public CustomNonGenericDictionary()
            {
            }

            public CustomNonGenericDictionary(SerializationInfo info, StreamingContext context) :
                base(info, context)
            {
            }
        }

        [XmlRoot("root")]
        public class XmlSerializableClass : IXmlSerializable
        {
            public int ReadWriteProperty { get; set; }

            public int SemiReadOnlyProperty { get; private set; }

            private int backingFieldOfRealReadOnlyProperty;
            public int RealReadOnlyProperty => backingFieldOfRealReadOnlyProperty;

            #region IXmlSerializable Members

            public System.Xml.Schema.XmlSchema GetSchema()
            {
                throw new NotImplementedException();
            }

            public void ReadXml(XmlReader reader)
            {
                if (reader.Settings != null && !reader.Settings.IgnoreWhitespace)
                {
                    reader = XmlReader.Create(reader, new XmlReaderSettings { IgnoreWhitespace = true });
                    reader.Read();
                }

                reader.ReadStartElement();
                ReadWriteProperty = reader.ReadElementContentAsInt("ReadWriteProperty", String.Empty);
                SemiReadOnlyProperty = reader.ReadElementContentAsInt("ReadOnlyAutoProperty", String.Empty);
                backingFieldOfRealReadOnlyProperty = reader.ReadElementContentAsInt("ReadOnlyProperty", String.Empty);
            }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteElementString("ReadWriteProperty", ReadWriteProperty.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("ReadOnlyAutoProperty", SemiReadOnlyProperty.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("ReadOnlyProperty", RealReadOnlyProperty.ToString(CultureInfo.InvariantCulture));
            }

            #endregion

            internal XmlSerializableClass()
            {/*needed for deserialization*/
            }

            public XmlSerializableClass(int realProp, int semiReadOnlyProp, int realReadOnlyProp)
            {
                ReadWriteProperty = realProp;
                SemiReadOnlyProperty = semiReadOnlyProp;
                backingFieldOfRealReadOnlyProperty = realReadOnlyProp;
            }

            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            public override int GetHashCode() => backingFieldOfRealReadOnlyProperty.GetHashCode() ^ ReadWriteProperty.GetHashCode() ^ SemiReadOnlyProperty.GetHashCode();
        }

        public struct XmlSerializableStruct : IXmlSerializable
        {
            public int ReadWriteProperty { get; set; }

            public int SemiReadOnlyProperty { get; private set; }

            private int backingFieldOfRealReadOnlyProperty;
            public int RealReadOnlyProperty => backingFieldOfRealReadOnlyProperty;

            #region IXmlSerializable Members

            public System.Xml.Schema.XmlSchema GetSchema()
            {
                throw new NotImplementedException();
            }

            public void ReadXml(XmlReader reader)
            {
                if (reader.Settings != null && !reader.Settings.IgnoreWhitespace)
                {
                    reader = XmlReader.Create(reader, new XmlReaderSettings { IgnoreWhitespace = true });
                    reader.Read();
                }

                reader.ReadStartElement();
                ReadWriteProperty = reader.ReadElementContentAsInt("ReadWriteProperty", String.Empty);
                SemiReadOnlyProperty = reader.ReadElementContentAsInt("ReadOnlyAutoProperty", String.Empty);
                backingFieldOfRealReadOnlyProperty = reader.ReadElementContentAsInt("ReadOnlyProperty", String.Empty);
            }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteElementString("ReadWriteProperty", ReadWriteProperty.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("ReadOnlyAutoProperty", SemiReadOnlyProperty.ToString(CultureInfo.InvariantCulture));
                writer.WriteElementString("ReadOnlyProperty", RealReadOnlyProperty.ToString(CultureInfo.InvariantCulture));
            }

            #endregion

            public XmlSerializableStruct(int realProp, int semiReadOnlyProp, int realReadOnlyProp)
                : this()
            {
                ReadWriteProperty = realProp;
                SemiReadOnlyProperty = semiReadOnlyProp;
                backingFieldOfRealReadOnlyProperty = realReadOnlyProp;
            }
        }

        public class ReadOnlyProperties
        {
            public XmlSerializableClass XmlSerializable { get; } = new XmlSerializableClass();
            public object[] Array3 { get; } = new object[3];
            public Cache<int, string> Cache { get; } = new Cache<int, string>(i => i.ToString());
            public ReadOnlyCollection<object> ReadOnlyCollection { get; set; }
            public ReadOnlyCollection<object> ConstReadOnlyCollection { get; } = new ReadOnlyCollection<object>(new object[] { 42, 'x' });

            public ReadOnlyProperties Init(XmlSerializableClass xmlSerializableClass = null, object[] array = null, int[] toCache = null, ReadOnlyCollection<object> readOnlyCollection = null)
            {
                CopyContent(XmlSerializable, xmlSerializableClass);
                CopyContent(Array3, array);
                toCache?.ForEach(i => { var dummy = Cache[i]; });
                ReadOnlyCollection = readOnlyCollection;
                return this;
            }

            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);
        }

        public class PopulatableCollectionWithReadOnlyProperties : ReadOnlyProperties, ICollection<string>
        {
            private readonly List<string> list = new List<string>();

            public IEnumerator<string> GetEnumerator() => list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
            public void Add(string item) => list.Add(item);
            public void Clear() => list.Clear();
            public bool Contains(string item) => throw new NotImplementedException();
            public void CopyTo(string[] array, int arrayIndex) => throw new NotImplementedException();
            public bool Remove(string item) => throw new NotImplementedException();
            public int Count => list.Count;
            public bool IsReadOnly => false;
        }

        public class ReadOnlyCollectionWithInitCtorAndReadOnlyProperties : ReadOnlyProperties, IEnumerable<string>
        {
            private readonly List<string> list;

            public ReadOnlyCollectionWithInitCtorAndReadOnlyProperties(IEnumerable<string> collection) => list = new List<string>(collection);

            public IEnumerator<string> GetEnumerator() => list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        }

        public class ReadOnlyCollectionWithoutInitCtorAndReadOnlyProperties : ReadOnlyProperties, IEnumerable<string>
        {
            private readonly List<string> list;

            public ReadOnlyCollectionWithoutInitCtorAndReadOnlyProperties() => list = new List<string> { "1", "2" };

            public IEnumerator<string> GetEnumerator() => list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        }

        public class FullExtraComponent
        {
            public class TestInner
            {
                public string InnerString { get; set; }

                public int InnerInt { get; set; }

                public TestInner()
                {
                    InnerString = "InnerStringValue";
                    InnerInt = 15;
                }
            }

            public struct InnerStructure
            {
                public string InnerString { get; set; }

                public int InnerInt { get; set; }

                public InnerStructure(string s, int i)
                    : this()
                {
                    InnerString = s;
                    InnerInt = i;
                }
            }

            [DefaultValue(0)]
            public int IntProp { get; set; }

            [DefaultValue(null)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public IntList IntList { get; set; }

            public FullExtraComponent()
            {
            }

            public int[] IntArray { get; set; }

            readonly int[] readOnlyIntArray = new int[5];
            public int[] ReadOnlyIntArray
            {
                get { return readOnlyIntArray; }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public TestInner Inner { get; set; }

            private readonly List<TestInner> innerList = new List<TestInner>();

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public List<TestInner> InnerList
            {
                get { return innerList; }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public LinkedList<int> IntLinkedList { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public TestInner[] InnerArray { get; set; }

            public XmlSerializableClass InnerXmlSerializable { get; set; }

            public Point Point { get; set; }

            public Point[] PointArray { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public InnerStructure Structure { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public InnerStructure[] StructureArray { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public List<InnerStructure> StructureList { get; set; }

            public string StringValue { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public Dictionary<string, object> StrObjDictionary { get; set; }

            public FullExtraComponent(bool init)
            {
                if (init)
                {
                    IntProp = 1;
                    Inner = new TestInner();
                    IntArray = new int[] { 1, 2, 3, 4, 5 };
                    readOnlyIntArray = new int[] { 1, 2, 3, 4, 5 };
                    IntList = new IntList { 1, 2 };
                    innerList = new List<TestInner>
                    {
                        new TestInner {InnerInt = 1, InnerString = "Egy"},
                        new TestInner {InnerInt = 2, InnerString = "Kettő"},
                        null
                    };
                    InnerXmlSerializable = new XmlSerializableClass(1, 2, 3);
                    IntLinkedList = new LinkedList<int>(new[] { 1, 2 });
                    Point = new Point(13, 13);
                    PointArray = new Point[] { new Point(1, 2), new Point(3, 4) };
                    InnerArray = new TestInner[] { new TestInner { InnerInt = 1, InnerString = "Egy" }, new TestInner { InnerInt = 2, InnerString = "Kettő" } };
                    Structure = new InnerStructure("InnerStructureString", 13);
                    StructureArray = new InnerStructure[] { new InnerStructure("Egyeske", 1), new InnerStructure("Ketteske", 2), };
                    StructureList = new List<InnerStructure> { new InnerStructure("Első", 1), new InnerStructure("Második", 2) };
                    StringValue = String.Empty;
                    StrObjDictionary = new Dictionary<string, object>
                    {
                        {"Kulcs1", "Érték1"},
                        {"Kulcs2", 15},
                        {"Kulcs3", new Point(13, 10)},
                        {"Kulcs4", new TestInner{InnerInt = 13, InnerString = "Trallala"}},
                        {"Kulcs5", new InnerStructure("StructValue", 111)},
                        {"Kulcs6", null}
                    };
                }
            }
        }

        public enum TestEnum
        {
            One,
            Two
        }

        public struct NonSerializableStruct
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            private string str10;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private byte[] bytes3;

            public int IntProp { get; set; }

            public string Str10
            {
                get { return str10; }
                set { str10 = value; }
            }

            public byte[] Bytes3
            {
                get { return bytes3; }
                set { bytes3 = value; }
            }

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);
        }

        [Serializable]
        public class BinarySerializableClass : AbstractClass, IBinarySerializable
        {
            public int IntProp { get; set; }

            public string StringProp { get; set; }

            public object ObjectProp { get; set; }

            #region IBinarySerializable Members

            public byte[] Serialize(BinarySerializationOptions options)
            {
                MemoryStream ms = new MemoryStream();
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(IntProp);
                    bw.Write(StringProp);
                    BinarySerializer.SerializeByWriter(bw, ObjectProp);
                }

                return ms.ToArray();
            }

            public void Deserialize(BinarySerializationOptions options, byte[] serData)
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(serData)))
                {
                    IntProp = br.ReadInt32();
                    StringProp = br.ReadString();
                    ObjectProp = BinarySerializer.DeserializeByReader(br);
                }
            }

            #endregion

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);
        }

        [Serializable]
        public sealed class BinarySerializableSealedClass : BinarySerializableClass
        {
        }

        [Serializable]
        public struct BinarySerializableStruct : IBinarySerializable
        {
            public int IntProp { get; set; }

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)]
            private string stringProp;

            public string StringProp
            {
                get { return stringProp; }
                set { stringProp = value; }
            }

            public Point Point { get; set; }

            public BinarySerializableStruct(int i, string s)
                : this()
            {
                IntProp = i;
                StringProp = s;
                Point = new Point(10, 10);
            }

            public BinarySerializableStruct(BinarySerializationOptions options, byte[] serData)
                : this()
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(serData)))
                {
                    IntProp = br.ReadInt32();
                    StringProp = br.ReadString();
                }
            }

            #region IBinarySerializable Members

            public byte[] Serialize(BinarySerializationOptions options)
            {
                MemoryStream ms = new MemoryStream();
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(IntProp);
                    bw.Write(StringProp);
                }

                return ms.ToArray();
            }

            public void Deserialize(BinarySerializationOptions options, byte[] serData)
            {
                throw new InvalidOperationException("This method never will be called");
            }

            #endregion
        }

        [Serializable]
        public struct SystemSerializableStruct
        {
            public int IntProp { get; set; }

            public string StringProp { get; set; }

            [NonSerialized]
            private int nonSerializedInt;

            [OnDeserializing]
            private void OnDeserializing(StreamingContext ctx)
            {
                IntProp = -1;
            }
        }

        [Serializable]
        public abstract class AbstractClass
        {
        }

        [Serializable]
        public class SystemSerializableClass : AbstractClass
        {
            public int IntProp { get; set; }

            public string StringProp { get; set; }

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);
        }

        [Serializable]
        private sealed class SystemSerializableSealedClass : SystemSerializableClass
        {
        }

        private class ExplicitTypeConverterHolder
        {
            private class MultilineTypeConverter : TypeConverter
            {
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
                    value == null ? null :
                    $"{value.GetType()}{Environment.NewLine}{(value is IFormattable formattable ? formattable.ToString(null, culture) : value.ToString())}";
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => Reflector.CanParseNatively(sourceType);
                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
                {
                    var parts = ((string)value).Split(new[] {Environment.NewLine}, StringSplitOptions.None);
                    Type type = Reflector.ResolveType(parts[0]);
                    return Reflector.Parse(type, parts[1], context, culture);
                }
            }

            [TypeConverter(typeof(MultilineTypeConverter))]
            public object ExplicitTypeConverterProperty { get; set; }

            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);
        }

        private class ConflictNameBase
        {
            public object item;
            public object ConflictingField;
            public object ConflictingProperty { get; set; }

            public ConflictNameBase SetBase(object item, object field, object prop)
            {
                this.item = item;
                ConflictingField = field;
                ConflictingProperty = prop;
                return this;
            }
        }

        private class ConflictNameChild : ConflictNameBase
        {
            public string item;
            public string ConflictingField;
            public string ConflictingProperty { get; set; }

            public ConflictNameChild SetChild(string item, string field, string prop)
            {
                this.item = item;
                ConflictingField = field;
                ConflictingProperty = prop;
                return this;
            }
        }

        class ConflictingCollection<T> : ConflictNameChild, ICollection<T>
        {
            private List<T> list = new List<T>();
            public T item { get; set; }
            public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
            public void Add(T item) => list.Add(item);
            public void Clear() => list.Clear();
            public bool Contains(T item) => throw new NotImplementedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
            public bool Remove(T item) => throw new NotImplementedException();
            public int Count => list.Count;
            public bool IsReadOnly => false;
        }

        class BinaryMembers
        {
            [TypeConverter(typeof(BinaryTypeConverter))]
            public object BinProp { get; set; }

            [TypeConverter(typeof(BinaryTypeConverter))]
            public Queue<string> BinPropReadOnly { get; } = new Queue<string>();

            public BinaryMembers()
            {
            }

            public BinaryMembers(params string[] elements) => BinPropReadOnly = new Queue<string>(elements);
        }

        #endregion

        #region Test Methods

        [TestMethod]
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
                "alma",
                (float)1,
                (double)1,
                (decimal)1,
                DateTime.UtcNow,
                DateTime.Now,
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);

            // These types cannot be serialized with system serializer
            referenceObjects = new object[]
            {
                DBNull.Value,
                new IntPtr(1),
                new UIntPtr(1),
                1.GetType(),
                new DateTimeOffset(DateTime.Now),
                new DateTimeOffset(DateTime.UtcNow),
                new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)),
                new TimeSpan(1, 2, 3, 4, 5),
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);
        }

        [TestMethod]
        public void SerializeFloats()
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
                Decimal.MinValue,
                Decimal.MaxValue
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);
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
                Char.ConvertFromUtf32(0x1D161)[0], // unpaired surrogate
                ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '​', '\u2028', '\u2029', '　', '﻿'
            };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.EscapeNewlineCharacters);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.EscapeNewlineCharacters, false);
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
                "🏯", // paired surrogate
           };

            SystemSerializeObject(referenceObjects);
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);

            // These strings cannot be (de)serialized with system serializer
            referenceObjects = new string[]
            {
                Environment.NewLine,
                @"new

                    lines  ",
                " ",
                 "\t",
                "\n",
                "\n\n",
                "\r",
                "\r\r",
                "\0",
                "\xFDD0", // U+FDD0 - <noncharacter-FDD0>
                "\xffff", // U+FFFF = <noncharacter-FFFF>
                "🏯"[0].ToString(null), // unpaired surrogate
                "<>\\'\"&{}{{}}\0\\0000",
                new string(new char[] { '\t', '\n', '\v', '\f', '\r', ' ', '\x0085', '\x00a0', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '​', '\u2028', '\u2029', '　', '﻿'}),
                "🏯" + "🏯"[0].ToString(null) + " b 🏯 " + "🏯"[1].ToString(null) + "\xffff \0 <>'\"&" // string containing unpaired surrogates
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.EscapeNewlineCharacters);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.EscapeNewlineCharacters, false);
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
                typeof(List<EmptyType>),                    // mixed
                typeof(IntList),                            // custom
                typeof(CustomGenericCollection<int>),       // mixed
                typeof(CustomGenericCollection<EmptyType>), // custom - EmptyType is stored differently when fully qualified
                typeof(List<>),                             // mscorlib, generic template
                typeof(int[]),                              // 1D zero based array
                typeof(int[,]),                             // multi-dim array
                typeof(int[][,]),                           // mixed jagged array
                Array.CreateInstance(typeof(int),new[]{3},new[]{-1}).GetType(), // nonzero based 1D array
                typeof(List<>).GetGenericArguments()[0]     // this can be only binary serialized
            };

            // binary for generic argument, recursive for chose recursion for the array
            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback, false);

            KGySerializeObjects(referenceObjects, XmlSerializationOptions.FullyQualifiedNames | XmlSerializationOptions.BinarySerializationAsFallback, false);
        }

        [TestMethod]
        public void SerializeByTypeConverter()
        {
            Reflector.RegisterTypeConverter<Version, VersionConverter>();
            Reflector.RegisterTypeConverter<Encoding, EncodingConverter>();

            object[] referenceObjects =
            {
                new Guid("ca761232ed4211cebacd00aa0057b223"),
                new Point(13, 13),
            };

            // SystemSerializeObject(referenceObjects); - InvalidOperationException: The type System.Drawing.Point was not expected.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);

            // These objects cannot be (de)serialized with system serializer
            referenceObjects = new object[]
            {
                new Uri(@"x:\teszt"),
                new Uri("ftp://myUrl/%2E%2E/%2E%2E"),
                new Version(1, 2, 3, 4),
                Encoding.UTF7,
                Color.Blue
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);

            // Type converter as property
            referenceObjects = new object[]
            {
                new BinarySerializableClass { ObjectProp = new Point(1, 2)}, // Point has self type converter
                new ExplicitTypeConverterHolder {ExplicitTypeConverterProperty = 13} // converter on property
            };

            // even escape can be omitted if deserialization is by XmlTextReader, which does not normalize newlines
            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback | XmlSerializationOptions.EscapeNewlineCharacters);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback | XmlSerializationOptions.EscapeNewlineCharacters);
        }

        [TestMethod]
        public void SerializeEnums()
        {
            Enum[] referenceObjects = 
            {
                TestEnum.One, // local enum
                TestEnum.Two, // local enum

                ConsoleColor.White, // mscorlib enum
                ConsoleColor.Black, // mscorlib enum

                UriKind.Absolute, // System enum
                UriKind.Relative, // System enum

                HandleInheritability.Inheritable, // System.Core enum

                DataAccessMethod.Random, // Microsoft.VisualStudio.QualityTools.UnitTestFramework enum

                BinarySerializationOptions.RecursiveSerializationAsFallback, // KGySoft.CoreLibraries enum
                BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.IgnoreIObjectReference, // KGySoft.CoreLibraries enum, multiple flags
            };

            // SystemSerializeObject(referenceObjects); - InvalidOperationException: The type _LibrariesTest.Libraries.Serialization.XmlSerializerTest+TestEnum may not be used in this context.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.FullyQualifiedNames); // FullyQualifiedNames: DataAccessMethod.Random: 10.0.0.0 <-> 10.1.0.0 if executed from ReSharper test
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.FullyQualifiedNames, false); // FullyQualifiedNames: DataAccessMethod.Random: 10.0.0.0 <-> 10.1.0.0 if executed from ReSharper test

            // These values cannot be serialized with system serializer
            referenceObjects = new Enum[]
                {
                    BinarySerializationOptions.ForcedSerializationValueTypesAsFallback, // KGySoft.CoreLibraries enum, obsolete element
                    (BinarySerializationOptions)(-1), // KGySoft.CoreLibraries enum, non-existing value
                };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);
        }

        [TestMethod]
        public void SerializeKeyValues()
        {
            ValueType[] referenceObjects =
                {
                    new DictionaryEntry(),
                    new DictionaryEntry(1, "alma"),
                    new DictionaryEntry(new object(), "alma"),
                };

            // SystemSerializeObject(referenceObjects); - NotSupportedException: System.ValueType is an unsupported type.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback);

            // These types cannot be serialized with system serializer
            referenceObjects = new ValueType[]
                {
                    new KeyValuePair<object, string>(),
                    new KeyValuePair<int,string>(1, "alma"),
                    new KeyValuePair<object, object>(1, " "),
                };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);
        }

        [TestMethod]
        public void SerializeComplexTypes()
        {
            object[] referenceObjects =
                {
                    new EmptyType(),
                    new BinarySerializableClass {IntProp = 1, StringProp = "alma", ObjectProp = " . "},
                    new BinarySerializableStruct {IntProp = 2, StringProp = "béka"},
                    new SystemSerializableClass {IntProp = 3, StringProp = "cica"},
                    new NonSerializableStruct {Bytes3 = new byte[] {1, 2, 3}, IntProp = 1, Str10 = "alma"},
                };

            // SystemSerializeObject(referenceObjects); - InvalidOperationException: The type _LibrariesTest.Libraries.Serialization.XmlSerializerTest+EmptyType was not expected.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, NonSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, NonSerializableStruct

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures); // BinarySerializableStruct, NonSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures); // BinarySerializableStruct, NonSerializableStruct

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures | XmlSerializationOptions.OmitCrcAttribute); // BinarySerializableStruct, NonSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures | XmlSerializationOptions.OmitCrcAttribute); // BinarySerializableStruct, NonSerializableStruct

            CheckTestingFramework(); // late ctor invoke
            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback); // everything
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback); // every element

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.OmitCrcAttribute); // everything
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.OmitCrcAttribute); // every element
        }

        [TestMethod]
        public void SerializeByteArrays()
        {
            IList[] referenceObjects = 
            {
                new byte[0], // empty array
                new byte[] { 1, 2, 3}, // single byte array
                new byte[][] { new byte[] {11, 12, 13}, new byte[] {21, 22, 23, 24, 25}, null }, // jagged byte array
            };

            // SystemSerializeObject(referenceObjects); - InvalidOperationException: System.Collections.IList cannot be serialized because it does not have a parameterless constructor.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // simple array, inner array of jagged array
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // simple array, inner array of jagged array

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays // simple array, inner array of jagged array
                | XmlSerializationOptions.OmitCrcAttribute); // compact parts
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays // simple array, inner array of jagged array
                | XmlSerializationOptions.OmitCrcAttribute); // compact parts

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.OmitCrcAttribute);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.OmitCrcAttribute);

            // These arrays cannot be serialized with system serializer
            referenceObjects = new IList[]
            {
                new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, // multidimensional byte array
                new byte[][,] { new byte[,] { { 11, 12, 13 }, { 21, 22, 23 } }, new byte[,] { { 11, 12, 13, 14 }, { 21, 22, 23, 24 }, { 31, 32, 33, 34 } } }, // crazy jagged byte array 1 (2D matrix of 1D arrays)
                new byte[,][] { { new byte[] { 11, 12, 13 }, new byte[] { 21, 22, 23 } }, { new byte[] { 11, 12, 13, 14 }, new byte[] { 21, 22, 23, 24 } } }, // crazy jagged byte array 2 (1D array of 2D matrices)
                new byte[][,,] { new byte[,,] { { { 11, 12, 13 }, { 21, 21, 23 } } }, null }, // crazy jagged byte array containing null reference
                Array.CreateInstance(typeof(byte), new[] { 3 }, new[] { -1 }), // array with -1..1 index interval
                Array.CreateInstance(typeof(byte), new[] { 3, 3 }, new[] { -1, 1 }) // array with [-1..1 and 1..3] index interval
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.OmitCrcAttribute);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback | XmlSerializationOptions.OmitCrcAttribute);
        }

        /// <summary>
        /// String has variable length and can be null.
        /// </summary>
        [TestMethod]
        public void SerializeStringArrays()
        {
            IList[] referenceObjects = 
            {
                new string[] { "Egy", "Kettő" }, // single string array
                new string[][] { new string[] {"Egy", "Kettő", "Három"}, new string[] {"One", "Two", null}, null }, // jagged string array with null values (first null as string, second null as array)
            };

            //SystemSerializeObject(referenceObjects); - InvalidOperationException: System.Collections.IList cannot be serialized because it does not have a parameterless constructor.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);

            referenceObjects = new IList[]
            {
                new string[,] { {"Egy", "Kettő"}, {"One", "Two"} }, // multidimensional string array
                Array.CreateInstance(typeof(string), new int[] {3}, new int[]{-1}) // array with -1..1 index interval
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
        }

        [TestMethod]
        public void SerializeSimpleArrays()
        {
            Reflector.RegisterTypeConverter<Version, VersionConverter>();

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
                };

            // SystemSerializeObject(referenceObjects); - InvalidOperationException: System.Collections.IList cannot be serialized because it does not have a parameterless constructor.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // simple arrays
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // simple arrays

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);  // every element
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);  // every element

            // these types cannot be serialized by system serializer
            referenceObjects = new IList[]
                {
                    new IntPtr[] {new IntPtr(1), IntPtr.Zero},
                    new UIntPtr[] {new UIntPtr(1), UIntPtr.Zero},
                    new Version[] {new Version(1, 2, 3, 4), null},
                };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // simple arrays
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // simple arrays

            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);  // every element
        }

        /// <summary>
        /// Enum types must be described explicitly
        /// </summary>
        [TestMethod]
        public void SerializeEnumArrays()
        {
            object[] referenceObjects = 
            {
                new TestEnum[] { TestEnum.One, TestEnum.Two }, // single enum array
                new TestEnum[][] { new TestEnum[] {TestEnum.One}, new TestEnum[] {TestEnum.Two} }, // jagged enum array
            };

            // SystemSerializeObject(referenceObjects); - InvalidOperationException: The type _LibrariesTest.Libraries.Serialization.XmlSerializerTest+TestEnum[] may not be used in this context.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            referenceObjects = new object[] 
            {
                new TestEnum[,] { {TestEnum.One}, {TestEnum.Two} }, // multidimensional enum array
                new object[] { TestEnum.One, null },
                new IConvertible[] { TestEnum.One, null },
                new Enum[] { TestEnum.One, null },
                new ValueType[] { TestEnum.One, null },
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);
        }

        [TestMethod]
        public void SerializeNullableArrays()
        {
            IList[] referenceObjects = 
            {
                new bool?[] { true, false, null },
                new sbyte?[] { 1, 2, null },
                new byte?[] { 1, 2, null },
                new short?[] { 1, 2, null },
                new ushort?[] { 1, 2, null },
                new int?[] { 1, 2, null },
                new uint?[] { 1, 2, null },
                new long?[] { 1, 2, null },
                new ulong?[] { 1, 2, null },
                new char?[] { 'a', /*Char.ConvertFromUtf32(0x1D161)[0],*/ null },
                new float?[] { 1, 2, null },
                new double?[] { 1, 2, null },
                new decimal?[] { 1, 2, null },
                new DateTime?[] { DateTime.UtcNow, DateTime.Now, null },
                new Guid?[] { new Guid("ca761232ed4211cebacd00aa0057b223"), Guid.NewGuid(), null },

                new TestEnum?[] { TestEnum.One, TestEnum.Two, null },

                new DictionaryEntry?[] { new DictionaryEntry(1, "alma"), null},

                new BinarySerializableStruct?[] { new BinarySerializableStruct{IntProp = 1, StringProp = "alma"}, null },
                new SystemSerializableStruct?[] { new SystemSerializableStruct{IntProp = 1, StringProp = "alma"}, null },
                new NonSerializableStruct?[] { new NonSerializableStruct{ Bytes3 = new byte[] {1,2,3}, IntProp = 10, Str10 = "alma"}, null },
            };

            // SystemSerializeObject(referenceObjects); - InvalidOperationException: System.Collections.IList cannot be serialized because it does not have a parameterless constructor.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, SystemSerializableStruct, NonSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, SystemSerializableStruct, NonSerializableStruct

            CheckTestingFramework(); // late ctor invoke
            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback); // all
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback); // as content, custom structs; otherwise, all

            referenceObjects = new IList[]
            {
                new BinarySerializableStruct?[] { new BinarySerializableStruct{IntProp = 1, StringProp = "alma"}, null },
                new SystemSerializableStruct?[] { new SystemSerializableStruct{IntProp = 1, StringProp = "alma"}, null },
                new NonSerializableStruct?[] { new NonSerializableStruct{ Bytes3 = new byte[] {1,2,3}, IntProp = 10, Str10 = "alma"}, null },
            };

            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback // as content, SystemSerializableStruct; otherwise, all
                | XmlSerializationOptions.CompactSerializationOfStructures); // as content, BinarySerializableStruct, NonSerializableStruct; otherwise, all

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback // SystemSerializableStruct
                | XmlSerializationOptions.CompactSerializationOfStructures); // BinarySerializableStruct, NonSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback // SystemSerializableStruct
                | XmlSerializationOptions.CompactSerializationOfStructures); // // BinarySerializableStruct, NonSerializableStruct

            // these types cannot be serialized by system serializer
            referenceObjects = new IList[]
            {
                new IntPtr?[] { new IntPtr(1), IntPtr.Zero, null },
                new UIntPtr?[] { new UIntPtr(1), UIntPtr.Zero, null },
                new TimeSpan?[] { new TimeSpan(1, 1, 1), new TimeSpan(DateTime.UtcNow.Ticks), null },
                new DateTimeOffset?[] { new DateTimeOffset(DateTime.Now), new DateTimeOffset(DateTime.UtcNow), new DateTimeOffset(DateTime.Now.Ticks, new TimeSpan(1, 1, 0)), null },

                new KeyValuePair<int, string>?[] { new KeyValuePair<int,string>(1, "alma"), null},
                new KeyValuePair<int?, int?>?[] { new KeyValuePair<int?,int?>(1, 2), new KeyValuePair<int?,int?>(2, null), null},
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            // these types cannot be serialized recursively and without a typeconverter are not supported natively
            referenceObjects = new IList[]
            {
                new BitVector32?[] { new BitVector32(13), null },
                new BitVector32.Section?[] { BitVector32.CreateSection(13), null },
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures); // non-null array elements
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures); // non-null array elements
        }


        [TestMethod]
        public void IXmlSerializableTest()
        {
            object[] referenceObjects =
                {
                    new XmlSerializableClass(1, 2, 3),
                    new XmlSerializableStruct(1, 2, 3),
                };

            //SystemSerializeObject(referenceObjects); - InvalidOperationException: The type _LibrariesTest.Libraries.Serialization.XmlSerializerTest+XmlSerializableClass may not be used in this context. To use _LibrariesTest.Libraries.Serialization.XmlSerializerTest+XmlSerializableClass as a parameter, return type, or member of a class or struct, the parameter, return type, or member must be declared as type _LibrariesTest.Libraries.Serialization.XmlSerializerTest+XmlSerializableClass (it cannot be object). Objects of type _LibrariesTest.Libraries.Serialization.XmlSerializerTest+XmlSerializableClass may not be used in un-typed collections, such as ArrayLists.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            referenceObjects = new[]
            {
                new ReadOnlyProperties().Init(xmlSerializableClass: new XmlSerializableClass(3, 2, 1))
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback);
        }

        [TestMethod]
        public void IXmlSerializableCollectionsTest()
        {
            IList<XmlSerializableClass>[] referenceObjects =
                {
                    new XmlSerializableClass[] { new XmlSerializableClass(1, 2, 3) },
                    new List<XmlSerializableClass> { new XmlSerializableClass(1, 2, 3) }
                };

            //SystemSerializeObject(referenceObjects); - NotSupportedException: Cannot serialize interface System.Collections.Generic.IList`1[[_LibrariesTest.Libraries.Serialization.XmlSerializerTest+XmlSerializableClass, _LibrariesTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b45eba277439ddfe]].
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);
        }

        /// <summary>
        /// Arrays of complex types
        /// </summary>
        [TestMethod]
        public void SerializeComplexArrays()
        {
            IList[] referenceObjects =
                {
                    new BinarySerializableStruct[] { new BinarySerializableStruct{IntProp = 1, StringProp = "alma"}, new BinarySerializableStruct{IntProp = 2, StringProp = "béka"} }, // array of a BinarySerializable struct
                    new BinarySerializableClass[] {new BinarySerializableClass {IntProp = 1, StringProp = "alma"}, new BinarySerializableClass{IntProp = 2, StringProp = "béka", ObjectProp = DateTime.Now } }, // array of a BinarySerializable non sealed class
                    new BinarySerializableSealedClass[] { new BinarySerializableSealedClass{IntProp = 1, StringProp = "alma"}, new BinarySerializableSealedClass{IntProp = 2, StringProp = "béka"}, new BinarySerializableSealedClass{IntProp = 3, StringProp = "cica"} }, // array of a BinarySerializable sealed class
                    new SystemSerializableClass[] { new SystemSerializableClass{IntProp = 1, StringProp = "alma"}, new SystemSerializableClass{IntProp = 2, StringProp = "béka"} }, // array of a [Serializable] object - will be serialized by BinaryFormatter
                    new NonSerializableStruct[] { new NonSerializableStruct{IntProp = 1, Str10 = "alma", Bytes3 = new byte[] {1, 2, 3}}, new NonSerializableStruct{IntProp = 2, Str10 = "béka", Bytes3 = new byte[] {3, 2, 1}} }, // array of any struct
                };

            //SystemSerializeObject(referenceObjects); - InvalidOperationException: System.Collections.IList cannot be serialized because it does not have a parameterless constructor.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, NonSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, NonSerializableStruct

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures); // structs
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfStructures); // structs

            CheckTestingFramework(); // late ctor invoke
            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback // everything
                | XmlSerializationOptions.CompactSerializationOfStructures); // nothing
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback // as content, non-structs; otherwise everything
                | XmlSerializationOptions.CompactSerializationOfStructures); // as content, structs; otherwise, nothing

            // These collections cannot be serialized with system serializer
            referenceObjects = new IList[]
                {
                    new BinarySerializableClass[] {new BinarySerializableSealedClass {IntProp = 1, StringProp = "alma"}, new BinarySerializableSealedClass{IntProp = 2, StringProp = "béka"} }, // array of a BinarySerializable non sealed class with derived elements
                    new IBinarySerializable[] {new BinarySerializableStruct {IntProp = 1, StringProp = "alma"}, new BinarySerializableClass {IntProp = 2, StringProp = "béka"}, new BinarySerializableSealedClass{IntProp = 3, StringProp = "cica"} }, // IBinarySerializable array
                    new AbstractClass[] { new SystemSerializableClass{IntProp = 1, StringProp = "alma"}, new SystemSerializableSealedClass{IntProp = 2, StringProp = "béka"} }, // array of a [Serializable] object
                    new AbstractClass[] { new BinarySerializableClass{IntProp = 1, StringProp = "alma"}, new SystemSerializableSealedClass{IntProp = 2, StringProp = "béka"} }, // array of a [Serializable] object, with an IBinarySerializable element
                    new IBinarySerializable[][] {new IBinarySerializable[] {new BinarySerializableStruct { IntProp = 1, StringProp = "alma"}}, null }, // IBinarySerializable array
                    new NonSerializableStruct[] { new NonSerializableStruct { IntProp = 1, Str10 = "alma", Bytes3 = new byte[] {1, 2, 3}}, new NonSerializableStruct{IntProp = 2, Str10 = "béka", Bytes3 = new byte[] {3, 2, 1}} }, // array of any struct

                    new ValueType[] { new BinarySerializableStruct{ IntProp = 1, StringProp = "alma"}, new SystemSerializableStruct {IntProp = 2, StringProp = "béka"}, null, 1},
                    new IConvertible[] { null, 1 },
                    new IConvertible[][] { null, new IConvertible[]{ null, 1},  },
                };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, NonSerializableStruct, SystemSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // BinarySerializableStruct, NonSerializableStruct, SystemSerializableStruct

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback // SystemSerializableStruct
                | XmlSerializationOptions.CompactSerializationOfStructures); // BinarySerializableStruct, NonSerializableStruct
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback // SystemSerializableStruct
                | XmlSerializationOptions.CompactSerializationOfStructures); // BinarySerializableStruct, NonSerializableStruct

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback // everything
                | XmlSerializationOptions.CompactSerializationOfStructures); // nothing
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback // as content, non-structs; otherwise everything
                | XmlSerializationOptions.CompactSerializationOfStructures); // as content, structs; otherwise, nothing
        }

        /// <summary>
        /// Simple generic collections
        /// </summary>
        [TestMethod]
        public void SerializeSimpleGenericCollections()
        {
            IEnumerable[] referenceObjects =
                {
                    new List<int> { 1, 2, 3 },
                    new List<int?> { 1, 2, null},
                    new List<int[]> { new int[]{1, 2, 3}, null },

                    new Collection<int> { 1, 2, 3 },
                    new Collection<int[]> { new int[]{1, 2, 3}, null },

                    new HashSet<int> { 1, 2, 3},
                    new HashSet<int[]> { new int[]{1, 2, 3}, null },
                };

            //SystemSerializeObject(referenceObjects); - NotSupportedException: Cannot serialize interface System.Collections.IEnumerable.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // Collection, HashSet
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // Collection, HashSet

            KGySerializeObject(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays // nested int[]
                | XmlSerializationOptions.RecursiveSerializationAsFallback); // Collection, HashSet
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.CompactSerializationOfPrimitiveArrays // nested int[]
                | XmlSerializationOptions.RecursiveSerializationAsFallback); // Collection, HashSet

            // these collections are not supported by system serializer
            referenceObjects = new IEnumerable[]
            {
                new LinkedList<int>(new[] { 1, 2, 3 }),
                new LinkedList<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),

                new Dictionary<int, string> { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } },
                new Dictionary<int[], string[]> { { new int[] { 1 }, new string[] { "alma" } }, { new int[] { 2 }, null } },
                new Dictionary<object, object> { { 1, "alma" }, { "béka", DateTime.Now }, { new object(), new object() }, { 4, new object[] { 1, "alma", DateTime.Now, null } }, { 5, null } },

                new SortedList<int, string> { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } },
                new SortedList<int, string[]> { { 1, new string[] { "alma" } }, { 2, null } },

                new SortedDictionary<int, string> { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } },
                new SortedDictionary<int, string[]> { { 1, new string[] { "alma" } }, { 2, null } },

#if !NET35
                new ConcurrentDictionary<int, string>(new Dictionary<int, string> { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } }),
#endif

                new Cache<int, string> { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } },
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // everything but LinkedList
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // everything but LinkedList
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // everything but LinkedList

            // these collections are not supported content recursively because they implement neither ICollection<T> nor IList
            referenceObjects = new IEnumerable[]
            {
                // non-populatable
                new Queue<int>(new[] { 1, 2, 3 }),
                new Queue<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),
                new Queue<int>[] { new Queue<int>(new int[] { 1, 2, 3 }) },
#if !NET35
                new ConcurrentQueue<int>(new[] { 1, 2, 3 }),
                new ConcurrentBag<int> { 1, 2, 3 },
#if !NET40
                new ArraySegment<int>(new[] { 1, 2, 3 }),
#endif
#endif

                // non-populatable, reverse
                new Stack<int>(new[] { 1, 2, 3 }),
                new Stack<int[]>(new int[][] { new int[] { 1, 2, 3 }, null }),
#if !NET35
                new ConcurrentStack<int>(new[] { 1, 2, 3 }),
#endif
                // read-only
                new ReadOnlyCollection<int>(new[] { 1, 2, 3 }),

#if !(NET35 || NET40)
                new ReadOnlyDictionary<int, string>(new Dictionary<int, string> { { 1, "One" }, { 2, "Two" } }),
#endif
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // ArraySegment, ReadOnlyCollection, ReadOnlyDictionary
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback, false); // ArraySegment, ReadOnlyCollection, ReadOnlyDictionary

#if !NET35
            // these collections are not supported recursively at all
            referenceObjects = new IEnumerable[]
            {
#if !NET40
                new ArraySegment<int>(new[] { 1, 2, 3 }, 1, 1), // initializer collection has 3 elements, while the segment has only 1
#endif
                new BlockingCollection<int> { 1, 2, 3 }, // no initializer constructor of array or list
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback, false);
#endif
        }

        [TestMethod]
        public void SerializeObjectsWithReadonlyProperties()
        {
            object[] referenceObjects =
            {
                new ReadOnlyProperties().Init(
                    xmlSerializableClass: new XmlSerializableClass(1, 2, 3),
                    array: new object[]{1, "string", DateTime.Now},
                    toCache: new []{1, 2, 3},
                    readOnlyCollection: new ReadOnlyCollection<object>(new object[] {'x', 1, "abc"} )
                    ),
                new PopulatableCollectionWithReadOnlyProperties{"one", "two"}.Init(
                    xmlSerializableClass: new XmlSerializableClass(1, 2, 3),
                    array: new object[]{1, "string", DateTime.Now},
                    toCache: new []{1, 2, 3},
                    readOnlyCollection: new ReadOnlyCollection<object>(new object[] {'x', 1, "abc"} )
                ),
                new ReadOnlyCollectionWithInitCtorAndReadOnlyProperties(new[]{"one", "two"}).Init(
                    xmlSerializableClass: new XmlSerializableClass(1, 2, 3),
                    array: new object[]{1, "string", DateTime.Now},
                    toCache: new []{1, 2, 3},
                    readOnlyCollection: new ReadOnlyCollection<object>(new object[] {'x', 1, "abc"} )),
            };

            //SystemSerializeObject(referenceObjects); // InvalidOperationException: The type _LibrariesTest.Libraries.Serialization.XmlSerializerTest+ReadOnlyProperties was not expected. Use the XmlInclude or SoapInclude attribute to specify types that are not known statically.
            //SystemSerializeObjects(referenceObjects); // InvalidOperationException: There was an error reflecting type '_LibrariesTest.Libraries.Serialization.XmlSerializerTest.ReadOnlyProperties'. ---> System.NotSupportedException: Cannot serialize member _LibrariesTest.Libraries.Serialization.XmlSerializerTest+ReadOnlyProperties.Cache of type KGySoft.CoreLibraries.Collections.Cache`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], because it implements IDictionary.

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback, false); // false for ReadOnlyCollectionWithReadOnlyProperties

            referenceObjects = new[]
            {
                new ReadOnlyCollectionWithoutInitCtorAndReadOnlyProperties().Init(
                    xmlSerializableClass: new XmlSerializableClass(1, 2, 3),
                    array: new object[]{1, "string", DateTime.Now},
                    toCache: new []{1, 2, 3},
                    readOnlyCollection: new ReadOnlyCollection<object>(new object[] {'x', 1, "abc"} ))
            };

            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback), "Serialization of collection \"_LibrariesTest.Tests.Serialization.XmlSerializerTest+ReadOnlyCollectionWithoutInitCtorAndReadOnlyProperties\" is not supported with following options: \"RecursiveSerializationAsFallback\", because it does not implement IList, IDictionary or ICollection<T> interfaces and has no initializer constructor that can accept an array or list.");
            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback, false);
        }

        [TestMethod]
        public void SerializeObjectsWithMemberNameCollision()
        {
            ConflictNameBase[] referenceObjects =
            {
                new ConflictNameBase { item = 13 },
                new ConflictNameChild { ConflictingField = "ChildField", ConflictingProperty = "ChildProp", item = "itemChild" }.SetBase(-13, "BaseField", "BaseProp"),
                new ConflictingCollection<string>{"item", "item2"}.SetChild("ChildItem", "ChildField", "ChildProp").SetBase(-5, "BaseFieldFromCollection", "CollectionBaseProp") 
            };

            //SystemSerializeObject(referenceObjects); // InvalidOperationException: _LibrariesTest.Libraries.Serialization.XmlSerializerTest+ConflictNameBase is inaccessible due to its protection level. Only public types can be processed.
            //SystemSerializeObjects(referenceObjects); // InvalidOperationException: _LibrariesTest.Libraries.Serialization.XmlSerializerTest+ConflictNameBase is inaccessible due to its protection level. Only public types can be processed.

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // ConflictingCollection
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // ConflictingCollection

            referenceObjects = new[]
            {
                new ConflictNameBase { ConflictingProperty = "PropValue" },
                new ConflictNameChild { ConflictingProperty = "ChildProp" }.SetBase(null, null, "BaseProp"),
                new ConflictingCollection<string> { "item", "item2" }.SetChild(null, null, "ChildProp").SetBase(null, null, "CollectionBaseProp")
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback | XmlSerializationOptions.ExcludeFields); // ConflictingCollection
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback | XmlSerializationOptions.ExcludeFields); // ConflictingCollection
        }

        [TestMethod]
        public void SerializeBinaryTypeConverterProperties()
        {
            object[] referenceObjects =
            {
                new BinaryMembers("One", "Two") { BinProp = DateTime.Now }
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.ForcedSerializationOfReadOnlyMembersAndCollections); // Queue as readonly property
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.ForcedSerializationOfReadOnlyMembersAndCollections); // Queue as readonly property
        }

        [TestMethod]
        public void SerializeFields()
        {
            object[] referenceObjects =
            {
                new StrongBox<int>(13)
            };

            //SystemSerializeObject(referenceObjects); // InvalidOperationException: The type System.Runtime.CompilerServices.StrongBox`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]] was not expected. Use the XmlInclude or SoapInclude attribute to specify types that are not known statically.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            Throws<AssertFailedException>(() => KGySerializeObjects(referenceObjects, XmlSerializationOptions.ExcludeFields), "Equality check failed at type System.Int32: 13 <-> 0");
        }

        /// <summary>
        /// Simple non-generic collections
        /// </summary>
        [TestMethod]
        public void SerializeSimpleNonGenericCollections()
        {
            IEnumerable[] referenceObjects =
            {
                new ArrayList { 1, "alma", DateTime.Now },
                new StringCollection { "alma", "béka", "cica" },
            };

            //SystemSerializeObject(referenceObjects); - NotSupportedException: Cannot serialize interface System.Collections.IEnumerable.
            SystemSerializeObjects(referenceObjects);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None);

            // these collections are not supported by system serializer
            referenceObjects = new IEnumerable[]
            {
                new Hashtable { { 1, "alma" }, { (byte)2, "béka" }, { 3m, "cica" } },
                new SortedList { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } },
                new ListDictionary { { 1, "alma" }, { 2, "béka" }, { 3, "cica" } },
                new HybridDictionary(false) { { "alma", 1 }, { "Alma", 2 }, { "ALMA", 3 } },
                new OrderedDictionary { { "alma", 1 }, { "Alma", 2 }, { "ALMA", 3 } },
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // all
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // all

            // these collections cannot be populated but they have supported initializer constructor
            referenceObjects = new IEnumerable[]
            {
                new Queue(new object[] { 1, (byte)2, 3m, new string[] { "alma", "béka", "cica" } }),
                new Stack(new object[] { 1, (byte)2, 3m, new string[] { "alma", "béka", "cica" } }),
                new BitArray(new[] { true, false, true })
            };

            KGySerializeObject(referenceObjects, XmlSerializationOptions.None);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.None, false);

            // these collections are not supported at all, binary fallback needed
            referenceObjects = new IEnumerable[]
            {
                new StringDictionary { { "a", "alma" }, { "b", "béka" }, { "c", "cica" }, { "x", null } },
            };

            Throws<SerializationException>(() => KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback), "Serialization of collection \"System.Collections.Specialized.StringDictionary\" is not supported with following options: \"RecursiveSerializationAsFallback\", because it does not implement IList, IDictionary or ICollection<T> interfaces and has no initializer constructor that can accept an array or list.");
            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback, false);
        }

        /// <summary>
        /// Complex generic collections
        /// </summary>
        [TestMethod]
        public void SerializeComplexGenericCollections()
        {
            Reflector.RegisterTypeConverter<Version, VersionConverter>();

            ICollection[] referenceObjects =
                {
                    new List<byte>[] { new List<byte>{ 11, 12, 13}, new List<byte>{21, 22} }, // array of lists
                    new List<byte[]> { new byte[]{ 11, 12, 13}, new byte[] {21, 22} }, // list of arrays

                    new Collection<KeyValuePair<int, object>> { new KeyValuePair<int, object>(1, "alma"), new KeyValuePair<int, object>(2, DateTime.Now), new KeyValuePair<int, object>(3, new object()), new KeyValuePair<int, object>(4, new object[] {1, "alma", DateTime.Now, null}), new KeyValuePair<int, object>(5, null) } ,

                    // dictionary with dictionary<int, string> value
                    new Dictionary<string, Dictionary<int, string>> { { "hu", new Dictionary<int, string>{ {1, "alma"}, {2, "béka"}, {3, "cica"}}}, {"en", new Dictionary<int, string>{ {1, "apple"}, {2, "frog"}, {3, "cat"}}} },

                    // dictionary with array key
                    new Dictionary<string[], Dictionary<int, string>> { { new string[] {"hu"}, new Dictionary<int, string>{ {1, "alma"}, {2, "béka"}, {3, "cica"}}}, {new string[] {"en"}, new Dictionary<int, string>{ {1, "apple"}, {2, "frog"}, {3, "cat"}}} },

                    // dictionary with dictionary key and value
                    new Dictionary<Dictionary<int[], string>, Dictionary<int, string>> { { new Dictionary<int[], string>{{new int[] {1}, "key.value1"}}, new Dictionary<int, string>{ {1, "alma"}, {2, "béka"}, {3, "cica"}}}, {new Dictionary<int[], string>{{new int[] {2}, "key.value2"}}, new Dictionary<int, string>{ {1, "apple"}, {2, "frog"}, {3, "cat"}}} },

                    // object list vith various elements
                    new List<object> { 1, "alma", new Version(13,0), new object[]{ 3, "cica", null}, new object(), null},

                    // dictionary with object key and value
                    new Dictionary<object, object> { {1, "alma"}, {new object(), "béka"}, {new int[] {3, 4}, null}, { TestEnum.One, "cica"} },

                    // non-sealed collections with base and derived elements
                    new List<BinarySerializableClass> {new BinarySerializableSealedClass {IntProp = 1, StringProp = "alma"}, new BinarySerializableSealedClass{IntProp = 2, StringProp = "béka"} },
                    new Dictionary<object, BinarySerializableClass> { {new object(), new BinarySerializableSealedClass {IntProp = 1, StringProp = "alma"}}, {2, new BinarySerializableSealedClass{IntProp = 2, StringProp = "béka"}} },

                    new IList<int>[] { new int[]{1, 2, 3}, new List<int>{1, 2, 3}},
                    new List<IList<int>> { new int[]{1, 2, 3}, new List<int>{1, 2, 3} } 
                };

            //SystemSerializeObject(referenceObjects); - InvalidOperationException: You must implement a default accessor on System.Collections.ICollection because it inherits from ICollection.
            //SystemSerializeObjects(referenceObjects); - NullReferenceException

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // All but list and arrays
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // All but list and arrays

            CheckTestingFramework(); // late ctor invoke
            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback); // everything
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback); // as content, nested collections and non-simple types; otherwise every element
        }

        /// <summary>
        /// Custom collections
        /// </summary>
        [TestMethod]
        public void SerializeCustomCollections()
        {
            ICollection[] referenceObjects =
                {
                    new CustomGenericCollection<KeyValuePair<int, object>> { new KeyValuePair<int, object>(1, "alma"), new KeyValuePair<int, object>(2, DateTime.Now), new KeyValuePair<int, object>(3, new object()), new KeyValuePair<int, object>(4, new object[] {1, "alma", DateTime.Now, null}), new KeyValuePair<int, object>(5, null) } ,
                    new CustomNonGenericCollection { new KeyValuePair<int, object>(1, "alma"), new KeyValuePair<int, object>(2, DateTime.Now), new KeyValuePair<int, object>(3, new object()), new KeyValuePair<int, object>(4, new object[] {1, "alma", DateTime.Now, null}), new KeyValuePair<int, object>(5, null) } ,
                    new CustomGenericDictionary<string, Dictionary<int, string>> { { "hu", new Dictionary<int, string>{ {1, "alma"}, {2, "béka"}, {3, "cica"}}}, {"en", new Dictionary<int, string>{ {1, "apple"}, {2, "frog"}, {3, "cat"}}} },
                    new CustomNonGenericDictionary { { "hu", new Dictionary<int, string>{ {1, "alma"}, {2, "béka"}, {3, "cica"}}}, {"en", new Dictionary<int, string>{ {1, "apple"}, {2, "frog"}, {3, "cat"}}} },
                };

            // SystemSerializeObject(referenceObjects); // InvalidOperationException: You must implement a default accessor on System.Collections.ICollection because it inherits from ICollection.
            // SystemSerializeObjects(referenceObjects); // InvalidOperationException: _LibrariesTest.Libraries.Serialization.XmlSerializerTest+CustomGenericCollection`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]] is inaccessible due to its protection level. Only public types can be processed.

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // all
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback); // all
        }

        [TestMethod]
        public void FullExtraComponentSerializationTest()
        {
            FullExtraComponent[] referenceObjects =
                {
                     new FullExtraComponent(true),
                     new FullExtraComponent(false),
                };

            //SystemSerializeObject(referenceObjects); // InvalidOperationException: You must implement a default accessor on System.Collections.Generic.LinkedList`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]] because it inherits from ICollection.
            //SystemSerializeObjects(referenceObjects); // InvalidOperationException: You must implement a default accessor on System.Collections.Generic.LinkedList`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]] because it inherits from ICollection.

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback);

            KGySerializeObject(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback  // every non-trusted type
                    | XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback // properties without DefaultAttribute
                    | XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // IntArray
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.RecursiveSerializationAsFallback  // every non-trusted type
                    | XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback // properties without DefaultAttribute
                    | XmlSerializationOptions.CompactSerializationOfPrimitiveArrays); // IntArray

            KGySerializeObject(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
            KGySerializeObjects(referenceObjects, XmlSerializationOptions.BinarySerializationAsFallback);
        }

#endregion

        #region Private methods

        private void SystemSerializeObject(object obj)
        {
            Type type = obj.GetType();
            Console.WriteLine("------------------System XmlSerializer ({0})--------------------", type);
            try
            {
                SystemXmlSerializer serializer = new SystemXmlSerializer(type);
                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb))
                {
                    serializer.Serialize(sw, obj);
                }
                Console.WriteLine(sb);
                object deserializedObject = serializer.Deserialize(new StringReader(sb.ToString()));
                AssertDeepEquals(obj, deserializedObject);
            }
            catch (Exception e)
            {
                Console.WriteLine("System serialization failed: {0}", e);
            }
        }

        private void SystemSerializeObjects(object[] referenceObjects)
        {
            Console.WriteLine("------------------System XmlSerializer (Items Count: {0})--------------------", referenceObjects.Length);
            try
            {
                List<object> deserializedObjects = new List<object>();
                foreach (object item in referenceObjects)
                {
                    if (item == null)
                    {
                        Console.WriteLine("Skipping null");
                        deserializedObjects.Add(null);
                        continue;
                    }

                    SystemXmlSerializer serializer = new SystemXmlSerializer(item.GetType());
                    StringBuilder sb = new StringBuilder();
                    using (StringWriter sw = new StringWriter(sb))
                    {
                        serializer.Serialize(sw, item);
                    }

                    Console.WriteLine(sb);
                    Console.WriteLine();
                    deserializedObjects.Add(serializer.Deserialize(new StringReader(sb.ToString())));
                }
                AssertItemsEqual(referenceObjects, deserializedObjects.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine("System serialization failed: {0}", e);
            }
        }

        private void KGySerializeObject(object obj, XmlSerializationOptions options)
        {
            Type type = obj.GetType();
            Console.WriteLine("------------------KGySoft XmlSerializer ({0} - options: {1})--------------------", type, options.ToString<XmlSerializationOptions>());
            try
            {
                // XElement - as object
                //Console.WriteLine(".....As object.....");
                XElement xElement = KGyXmlSerializer.Serialize(obj, options);
                Console.WriteLine(xElement);
                object deserializedObject = KGyXmlSerializer.Deserialize(xElement);
                AssertDeepEquals(obj, deserializedObject);

                // XmlReader/Writer - as object
                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                {
                    KGyXmlSerializer.Serialize(writer, obj, options);
                }

                // deserialize by reader - if file already contains unescaped newlines: // new XmlTextReader(new StringReader(sb.ToString()));
                using (var reader = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { CloseInput = true }))
                {
                    deserializedObject = KGyXmlSerializer.Deserialize(reader);
                }

                AssertDeepEquals(obj, deserializedObject);
                Assert.AreEqual(xElement.ToString(), sb.ToString(), "XElement and XmlWriter Serialize are not compatible");

                // XElement - as component
                //Console.WriteLine();
                //Console.WriteLine(".....As component.....");
                var xElementComp = new XElement("test");
                KGyXmlSerializer.SerializeContent(xElementComp, obj, options);
                //Console.WriteLine(xElementComp);
                deserializedObject = type.IsArray ? Array.CreateInstance(type.GetElementType(), ((Array)obj).Length) : Reflector.CreateInstance(type);
                KGyXmlSerializer.DeserializeContent(xElementComp, deserializedObject);
                AssertDeepEquals(obj, deserializedObject);

                // XmlReader/Writer - as component
                sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                {
                    writer.WriteStartElement("test");
                    KGyXmlSerializer.SerializeContent(writer, obj, options);
                    writer.WriteFullEndElement();
                    writer.Flush();
                }

                // deserialize by reader - if file already contains unescaped newlines: // new XmlTextReader(new StringReader(sb.ToString()));
                using (var reader = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { CloseInput = true, IgnoreWhitespace = true }))
                {
                    deserializedObject = type.IsArray ? Array.CreateInstance(type.GetElementType(), ((Array)obj).Length) : Reflector.CreateInstance(type);
                    reader.Read(); // to node "test"
                    KGyXmlSerializer.DeserializeContent(reader, deserializedObject);
                    reader.ReadEndElement();
                }

                AssertDeepEquals(obj, deserializedObject);
                Assert.AreEqual(xElementComp.ToString(), sb.ToString(), "XElement and XmlWriter SerializeContent are not compatible");
            }
            catch (Exception e)
            {
                Console.WriteLine("KGySoft serialization failed: {0}", e);
                throw;
            }
        }

        private void KGySerializeObjects(object[] referenceObjects, XmlSerializationOptions options, bool alsoContent = true)
        {
            Console.WriteLine("------------------KGySoft XmlSerializer (Items Count: {0}; options: {1})--------------------", referenceObjects.Length, options.ToString<XmlSerializationOptions>());
            try
            {
                XElement xElement = new XElement("test");
                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                {
                    writer.WriteStartElement("test");

                    foreach (var item in referenceObjects)
                    {
                        xElement.Add(KGyXmlSerializer.Serialize(item, options));
                        KGyXmlSerializer.Serialize(writer, item, options);

                        if (!alsoContent)
                            continue;

                        // content serialization test for element
                        if (item == null)
                        {
                            Console.WriteLine("Skipping null");
                            continue;
                        }

                        XElement xItem = new XElement("itemContent");
                        StringBuilder sbItem = new StringBuilder();
                        using (XmlWriter itemWriter = XmlWriter.Create(sbItem, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                        {
                            KGyXmlSerializer.SerializeContent(xItem, item, options);
                            Console.WriteLine(xItem);
                            Console.WriteLine();
                            itemWriter.WriteStartElement("itemContent");
                            KGyXmlSerializer.SerializeContent(itemWriter, item, options);
                            itemWriter.WriteFullEndElement();
                        }

                        object deserXElement;
                        object deserReader;
                        using (XmlReader itemReader = XmlReader.Create(new StringReader(sbItem.ToString()), new XmlReaderSettings { IgnoreWhitespace = true }))
                        {
                            var itemType = item.GetType();
                            deserXElement = itemType.IsArray ? item.DeepClone() : Reflector.CreateInstance(itemType);
                            KGyXmlSerializer.DeserializeContent(xItem, deserXElement);
                            deserReader = itemType.IsArray ? item.DeepClone() : Reflector.CreateInstance(itemType);
                            itemReader.Read(); // to node "itemContent"
                            KGyXmlSerializer.DeserializeContent(itemReader, deserReader);
                            itemReader.ReadEndElement();
                        }

                        AssertDeepEquals(item, deserXElement);
                        AssertDeepEquals(item, deserReader);
                        Assert.AreEqual(xItem.ToString(), sbItem.ToString(), "XElement and XmlWriter serializers are not compatible");
                    }
                    writer.WriteEndDocument();
                    writer.Flush();
                }

                Console.WriteLine(xElement);

                List<object> deserializedObjects = new List<object>();
                // deserialize by reader - if file already contains unescaped newlines: // new XmlTextReader(new StringReader(sb.ToString()));
                using (XmlReader reader = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { IgnoreWhitespace = true }))
                {
                    try
                    {
                        reader.Read(); // test
                        foreach (XElement element in xElement.Elements())
                        {
                            object deserXElement = KGyXmlSerializer.Deserialize(element);
                            object deserReader = KGyXmlSerializer.Deserialize(reader);
                            AssertDeepEquals(deserXElement, deserReader);

                            deserializedObjects.Add(deserXElement);
                        }

                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                AssertItemsEqual(referenceObjects, deserializedObjects.ToArray());

                Assert.AreEqual(xElement.ToString(), sb.ToString(), "XElement and XmlWriter serializers are not compatible");
            }
            catch (Exception e)
            {
                Console.WriteLine("KGySoft serialization failed: {0}", e);
                throw;
            }
        }

#endregion
    }
}
