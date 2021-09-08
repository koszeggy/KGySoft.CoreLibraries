#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlSerializerTest.TestData.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

using KGySoft.Collections;
using KGySoft.ComponentModel;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode() - test types

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Xml
{
    partial class XmlSerializerTest
    {
        #region Nested types

        #region Enumerations

        public enum TestEnum
        {
            One,
            Two
        }

        #endregion

        #region Nested classes

        #region EmptyType class

        public class EmptyType
        {
            #region Methods

            public override bool Equals(object obj) => true;

            public override int GetHashCode() => 0;

            #endregion
        }

        #endregion

        #region IntList class

        public class IntList : List<int>, ICollection<int>
        {
            #region Properties

            #region Public Properties

            public bool IsReadOnly => false;

            #endregion

            #region Explicitly Implemented Interface Properties

            bool ICollection<int>.IsReadOnly => (this as ICollection<int>).IsReadOnly;

            #endregion

            #endregion
        }

        #endregion

        #region CustomGenericCollection class

        [Serializable]
        private class CustomGenericCollection<T> : List<T>
        {
        }

        #endregion

        #region CustomNonGenericCollection class

        [Serializable]
        private class CustomNonGenericCollection : ArrayList
        {
        }

        #endregion

        #region CustomGenericDictionary class

        [Serializable]
        private class CustomGenericDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            #region Constructors

            public CustomGenericDictionary()
            {
            }

            public CustomGenericDictionary(SerializationInfo info, StreamingContext context) :
                base(info, context)
            {
            }

            #endregion
        }

        #endregion

        #region CustomNonGenericDictionary class

        [Serializable]
        private class CustomNonGenericDictionary : Hashtable
        {
            #region Constructors

            public CustomNonGenericDictionary()
            {
            }

            public CustomNonGenericDictionary(SerializationInfo info, StreamingContext context) :
                base(info, context)
            {
            }

            #endregion
        }

        #endregion

        #region OpenGenericDictionary class

        private class OpenGenericDictionary<TValue> : Dictionary<string, TValue>
        {
        }

        #endregion

        #region XmlSerializableClass class

        [XmlRoot("root")]
        public class XmlSerializableClass : IXmlSerializable
        {
            #region Fields

            private int backingFieldOfRealReadOnlyProperty;

            #endregion

            #region Properties

            public int ReadWriteProperty { get; set; }

            public int SemiReadOnlyProperty { get; private set; }

            public int RealReadOnlyProperty => backingFieldOfRealReadOnlyProperty;

            #endregion

            #region Constructors

            #region Public Constructors

            public XmlSerializableClass(int realProp, int semiReadOnlyProp, int realReadOnlyProp)
            {
                ReadWriteProperty = realProp;
                SemiReadOnlyProperty = semiReadOnlyProp;
                backingFieldOfRealReadOnlyProperty = realReadOnlyProp;
            }

            #endregion

            #region Internal Constructors

            internal XmlSerializableClass()
            {/*needed for deserialization*/
            }

            #endregion

            #endregion

            #region Methods

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

            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            public override int GetHashCode() => backingFieldOfRealReadOnlyProperty.GetHashCode() ^ ReadWriteProperty.GetHashCode() ^ SemiReadOnlyProperty.GetHashCode();

            #endregion
        }

        #endregion

        #region ReadOnlyProperties class

        public class ReadOnlyProperties
        {
            #region Properties

            public XmlSerializableClass XmlSerializable { get; } = new XmlSerializableClass();

            public object[] Array3 { get; } = new object[3];

            public Cache<int, string> Cache { get; } =
#if NETFRAMEWORK
                new Cache<int, string>(i => i.ToString()); 
#else
                new Cache<int, string>(); // .NET Core does not support delegate serialization
#endif

            public ReadOnlyCollection<object> ReadOnlyCollection { get; set; }

            public ReadOnlyCollection<object> ConstReadOnlyCollection { get; } = new ReadOnlyCollection<object>(new object[] { 42, 'x' });

            #endregion

            #region Methods

            public ReadOnlyProperties Init(XmlSerializableClass xmlSerializableClass = null, object[] array = null, int[] toCache = null, ReadOnlyCollection<object> readOnlyCollection = null)
            {
                CopyContent(XmlSerializable, xmlSerializableClass);
                CopyContent(Array3, array);
#if NETFRAMEWORK
                toCache?.ForEach(i => { var dummy = Cache[i]; });
#else
                toCache?.ForEach(i => Cache[i] = i.ToString());
#endif
                ReadOnlyCollection = readOnlyCollection;
                return this;
            }

            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            #endregion
        }

        #endregion

        #region PopulatableCollectionWithReadOnlyProperties class

        public class PopulatableCollectionWithReadOnlyProperties : ReadOnlyProperties, ICollection<string>
        {
            #region Fields

            private readonly List<string> list = new List<string>();

            #endregion

            #region Properties

            public int Count => list.Count;

            public bool IsReadOnly => false;

            #endregion

            #region Methods

            #region Public Methods

            public IEnumerator<string> GetEnumerator() => list.GetEnumerator();

            public void Add(string item) => list.Add(item);

            public void Clear() => list.Clear();

            public bool Contains(string item) => throw new NotImplementedException();

            public void CopyTo(string[] array, int arrayIndex) => throw new NotImplementedException();

            public bool Remove(string item) => throw new NotImplementedException();

            #endregion

            #region Explicitly Implemented Interface Methods

            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

            #endregion

            #endregion
        }

        #endregion

        #region ReadOnlyCollectionWithInitCtorAndReadOnlyProperties class

        public class ReadOnlyCollectionWithInitCtorAndReadOnlyProperties : ReadOnlyProperties, IEnumerable<string>
        {
            #region Fields

            private readonly List<string> list;

            #endregion

            #region Constructors

            public ReadOnlyCollectionWithInitCtorAndReadOnlyProperties(IEnumerable<string> collection) => list = new List<string>(collection);

            #endregion

            #region Methods

            #region Public Methods

            public IEnumerator<string> GetEnumerator() => list.GetEnumerator();

            #endregion

            #region Explicitly Implemented Interface Methods

            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

            #endregion

            #endregion
        }

        #endregion

        #region ReadOnlyCollectionWithoutInitCtorAndReadOnlyProperties class

        public class ReadOnlyCollectionWithoutInitCtorAndReadOnlyProperties : ReadOnlyProperties, IEnumerable<string>
        {
            #region Fields

            private readonly List<string> list;

            #endregion

            #region Constructors

            public ReadOnlyCollectionWithoutInitCtorAndReadOnlyProperties() => list = new List<string> { "1", "2" };

            #endregion

            #region Methods

            #region Public Methods

            public IEnumerator<string> GetEnumerator() => list.GetEnumerator();

            #endregion

            #region Explicitly Implemented Interface Methods

            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

            #endregion

            #endregion
        }

        #endregion

        #region FullExtraComponent class

        public class FullExtraComponent
        {
            #region Nested types

            #region Nested classes

            #region TestInner class

            public class TestInner
            {
                #region Properties

                public string InnerString { get; set; }

                public int InnerInt { get; set; }

                #endregion

                #region Constructors

                public TestInner()
                {
                    InnerString = "InnerStringValue";
                    InnerInt = 15;
                }

                #endregion
            }

            #endregion

            #endregion

            #region Nested structs

            #region InnerStructure struct

            public struct InnerStructure
            {
                #region Properties

                public string InnerString { get; set; }

                public int InnerInt { get; set; }

                #endregion

                #region Constructors

                public InnerStructure(string s, int i)
                    : this()
                {
                    InnerString = s;
                    InnerInt = i;
                }

                #endregion
            }

            #endregion

            #endregion

            #endregion

            #region Fields

            readonly int[] readOnlyIntArray = new int[5];
            private readonly List<TestInner> innerList = new List<TestInner>();

            #endregion

            #region Properties

            [DefaultValue(0)]
            public int IntProp { get; set; }

            [DefaultValue(null)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public IntList IntList { get; set; }

            public int[] IntArray { get; set; }

            public int[] ReadOnlyIntArray
            {
                get { return readOnlyIntArray; }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public TestInner Inner { get; set; }

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

            #endregion

            #region Constructors

            public FullExtraComponent()
            {
            }

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
                        new TestInner {InnerInt = 1, InnerString = "One"},
                        new TestInner {InnerInt = 2, InnerString = "Two"},
                        null
                    };
                    InnerXmlSerializable = new XmlSerializableClass(1, 2, 3);
                    IntLinkedList = new LinkedList<int>(new[] { 1, 2 });
                    Point = new Point(13, 13);
                    PointArray = new Point[] { new Point(1, 2), new Point(3, 4) };
                    InnerArray = new TestInner[] { new TestInner { InnerInt = 1, InnerString = "One" }, new TestInner { InnerInt = 2, InnerString = "Two" } };
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

            #endregion
        }

        #endregion

        #region BinarySerializableClass class

        [Serializable]
        public class BinarySerializableClass : AbstractClass, IBinarySerializable
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            public object ObjectProp { get; set; }

            #endregion

            #region Methods

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

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            #endregion
        }

        #endregion

        #region BinarySerializableSealedClass class

        [Serializable]
        public sealed class BinarySerializableSealedClass : BinarySerializableClass
        {
        }

        #endregion

        #region AbstractClass class

        [Serializable]
        public abstract class AbstractClass
        {
        }

        #endregion

        #region SystemSerializableClass class

        [Serializable]
        public class SystemSerializableClass : AbstractClass
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            #endregion

            #region Methods

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            #endregion
        }

        #endregion

        #region SystemSerializableSealedClass class

        [Serializable]
        private sealed class SystemSerializableSealedClass : SystemSerializableClass
        {
        }

        #endregion

        #region ExplicitTypeConverterHolder class

        private class ExplicitTypeConverterHolder
        {
            #region Nested classes

            #region MultilineTypeConverter class

            private class MultilineTypeConverter : TypeConverter
            {
                #region Methods

                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
                    value == null ? null :
                    $"{value.GetType()}{Environment.NewLine}{(value is IFormattable formattable ? formattable.ToString(null, culture) : value.ToString())}";

                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType.CanBeParsedNatively();

                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
                {
                    var parts = ((string)value).Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    Type type = Reflector.ResolveType(parts[0]);
                    return parts[1].Parse(type, culture);
                }

                #endregion
            }

            #endregion

            #endregion

            #region Properties

            [TypeConverter(typeof(MultilineTypeConverter))]
            public object ExplicitTypeConverterProperty { get; set; }

            #endregion

            #region Methods

            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            #endregion
        }

        #endregion

        #region ConflictNameBase class

        private class ConflictNameBase
        {
            #region Fields

            public object item;

            public object ConflictingField;

            #endregion

            #region Properties

            public object ConflictingProperty { get; set; }

            #endregion

            #region Methods

            public ConflictNameBase SetBase(object item, object field, object prop)
            {
                this.item = item;
                ConflictingField = field;
                ConflictingProperty = prop;
                return this;
            }

            #endregion
        }

        #endregion

        #region ConflictNameChild class

        private class ConflictNameChild : ConflictNameBase
        {
            #region Fields

            public new string item;

            public new string ConflictingField;

            #endregion

            #region Properties

            public new string ConflictingProperty { get; set; }

            #endregion

            #region Methods

            public ConflictNameChild SetChild(string item, string field, string prop)
            {
                this.item = item;
                ConflictingField = field;
                ConflictingProperty = prop;
                return this;
            }

            #endregion
        }

        #endregion

        #region ConflictingCollection class

        class ConflictingCollection<T> : ConflictNameChild, ICollection<T>
        {
            #region Fields

            private List<T> list = new List<T>();

            #endregion

            #region Properties

            public new T item { get; set; }

            public int Count => list.Count;

            public bool IsReadOnly => false;

            #endregion

            #region Methods

            #region Public Methods

            public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

            public void Add(T item) => list.Add(item);

            public void Clear() => list.Clear();

            public bool Contains(T item) => throw new NotImplementedException();

            public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();

            public bool Remove(T item) => throw new NotImplementedException();

            #endregion

            #region Explicitly Implemented Interface Methods

            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

            #endregion

            #endregion
        }

        #endregion

        #region BinaryMembers class

        class BinaryMembers
        {
            #region Properties

            [TypeConverter(typeof(BinaryTypeConverter))]
            public object BinProp { get; set; }

            [TypeConverter(typeof(BinaryTypeConverter))]
            public Queue<string> BinPropReadOnly { get; } = new Queue<string>();

            #endregion

            #region Constructors

            public BinaryMembers()
            {
            }

            public BinaryMembers(params string[] elements) => BinPropReadOnly = new Queue<string>(elements);

            #endregion
        }

        #endregion

        #endregion

        #region Nested structs

        #region XmlSerializableStruct struct

        public struct XmlSerializableStruct : IXmlSerializable
        {
            #region Fields

            private int backingFieldOfRealReadOnlyProperty;

            #endregion

            #region Properties

            public int ReadWriteProperty { get; set; }

            public int SemiReadOnlyProperty { get; private set; }

            public int RealReadOnlyProperty => backingFieldOfRealReadOnlyProperty;

            #endregion

            #region Constructors

            public XmlSerializableStruct(int realProp, int semiReadOnlyProp, int realReadOnlyProp)
                : this()
            {
                ReadWriteProperty = realProp;
                SemiReadOnlyProperty = semiReadOnlyProp;
                backingFieldOfRealReadOnlyProperty = realReadOnlyProp;
            }

            #endregion

            #region Methods

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
        }

        #endregion

        #region NonSerializableStruct struct

        public struct NonSerializableStruct
        {
            #region Properties

            public int IntProp { get; set; }
            public bool Bool { get; set; }
            public Point Point { get; set; }

            #endregion

            #region Methods

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            #endregion
        }

        #endregion

        #region BinarySerializableStruct struct

        [Serializable]
        public struct BinarySerializableStruct : IBinarySerializable
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            public Point Point { get; set; }

            #endregion

            #region Constructors

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

            #endregion

            #region Methods

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

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj) => MembersAndItemsEqual(this, obj);

            #endregion
        }

        #endregion

        #region SystemSerializableStruct struct

        [Serializable]
        public struct SystemSerializableStruct
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            #endregion

            #region Methods

            [OnDeserializing]
            private void OnDeserializing(StreamingContext ctx)
            {
                IntProp = -1;
            }

            #endregion
        }

        #endregion

        #region UnsafeStruct struct

        public unsafe struct UnsafeStruct
        {
            #region Fields

            public void* VoidPointer;
            public int* IntPointer;

            #endregion

            #region Properties

            public int*[] PointerArray { get; set; }
            public void** PointerOfPointer { get; set; }

            #endregion
        }

        #endregion

        #endregion

        #endregion
    }
}
