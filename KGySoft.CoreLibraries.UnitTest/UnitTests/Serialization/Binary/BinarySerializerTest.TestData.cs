#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializerTest.TestData.cs
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
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Binary
{
    partial class BinarySerializerTest
    {
        #region Nested types
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

        #region Enumerations

        enum TestEnumSByte : sbyte
        {
            Min = SByte.MinValue,
            Max = SByte.MaxValue
        }

        enum TestEnumByte : byte
        {
            Min = Byte.MinValue,
            One = 1,
            Two,
            Max = Byte.MaxValue
        }

        enum TestEnumShort : short
        {
            Min = Int16.MinValue,
            Limit = (1 << 7) - 1,
            Treshold,
            Max = Int16.MaxValue,
        }

        enum TestEnumUShort : ushort
        {
            Min = UInt16.MinValue,
            Limit = (1 << 7) - 1,
            Treshold,
            Max = UInt16.MaxValue,
        }

        enum TestEnumInt : int
        {
            Min = Int32.MinValue,
            Limit = (1 << 21) - 1,
            Treshold,
            Max = Int32.MaxValue,
        }

        enum TestEnumUInt : uint
        {
            Min = UInt32.MinValue,
            Limit = (1 << 21) - 1,
            Treshold,
            Max = UInt32.MaxValue,
        }

        enum TestEnumLong : long
        {
            Min = Int64.MinValue,
            Limit = (1L << 49) - 1,
            Treshold,
            Max = Int64.MaxValue,
        }

        enum TestEnumULong : ulong
        {
            Min = UInt64.MinValue,
            Limit = (1UL << 49) - 1,
            Treshold,
            Max = UInt64.MaxValue,
        }

        #endregion

        #region Nested classes

        #region NonSerializableClass class

        private class NonSerializableClass
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            #endregion

            #region Methods

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is NonSerializableClass))
                    return base.Equals(obj);
                NonSerializableClass other = (NonSerializableClass)obj;
                return StringProp == other.StringProp && IntProp == other.IntProp;
            }

            #endregion
        }

        #endregion

        #region NonSerializableSealedClass class

        private sealed class NonSerializableSealedClass : NonSerializableClass
        {
            #region Fields

            #region Public Fields

            public readonly int PublicDerivedField;

            #endregion

            #region Private Fields

            private readonly string privateDerivedField;

            #endregion

            #endregion

            #region Constructors

            public NonSerializableSealedClass(int i, string s)
            {
                PublicDerivedField = i;
                privateDerivedField = s;
            }

            #endregion

            #region Methods

            public override bool Equals(object obj)
                => obj is NonSerializableSealedClass other
                    && base.Equals(obj)
                    && other.PublicDerivedField == PublicDerivedField
                    && other.privateDerivedField == privateDerivedField;

            #endregion
        }

        #endregion

        #region BinarySerializableClass class

        [Serializable]
        private class BinarySerializableClass : AbstractClass, IBinarySerializable
        {
            #region Fields

            public int PublicField;

            #endregion

            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            #endregion

            #region Methods

            #region Public Methods

            public byte[] Serialize(BinarySerializationOptions options)
            {
                MemoryStream ms = new MemoryStream();
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PublicField);
                    bw.Write(IntProp);
                    bw.Write(StringProp);
                }

                return ms.ToArray();
            }

            public void Deserialize(BinarySerializationOptions options, byte[] serData)
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(serData)))
                {
                    PublicField = br.ReadInt32();
                    IntProp = br.ReadInt32();
                    StringProp = br.ReadString();
                }
            }

            #endregion

            #region Private Methods

            [OnDeserializing]
            private void OnDeserializing(StreamingContext ctx)
            {
                IntProp = -1;
            }

            #endregion

            #endregion
        }

        #endregion

        #region BinarySerializableSealedClass class

        [Serializable]
        private sealed class BinarySerializableSealedClass : BinarySerializableClass
        {
            #region Constructors

            /// <summary>
            /// Non-default constructor so the class will be deserialized without constructor
            /// </summary>
            public BinarySerializableSealedClass(int intProp, string stringProp)
            {
                IntProp = intProp;
                StringProp = stringProp;
            }

            #endregion
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
        private class SystemSerializableClass : AbstractClass
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            public bool? Bool { get; set; }

            #endregion

            #region Methods

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is SystemSerializableClass))
                    return base.Equals(obj);
                SystemSerializableClass other = (SystemSerializableClass)obj;
                return StringProp == other.StringProp && IntProp == other.IntProp && Bool == other.Bool;
            }

            #endregion
        }

        #endregion

        #region NonSerializableClassWithSerializableBase class

        private sealed class NonSerializableClassWithSerializableBase : SystemSerializableClass
        {
            #region Fields

            #region Public Fields

            public readonly int PublicDerivedField;

            #endregion

            #region Private Fields

            private readonly string privateDerivedField;

            #endregion

            #endregion

            #region Constructors

            public NonSerializableClassWithSerializableBase(int i, string s)
            {
                PublicDerivedField = i;
                privateDerivedField = s;
            }

            #endregion

            #region Methods

            public override bool Equals(object obj)
                => obj is NonSerializableClassWithSerializableBase other
                    && base.Equals(obj)
                    && other.PublicDerivedField == PublicDerivedField
                    && other.privateDerivedField == privateDerivedField;

            #endregion
        }

        #endregion

        #region SystemSerializableSealedClass class

        [Serializable]
        private sealed class SystemSerializableSealedClass : SystemSerializableClass
        {
        }

        #endregion

        #region SerializationEventsClass class

        [Serializable]
        private class SerializationEventsClass : IDeserializationCallback
        {
            #region Fields

            #region Static Fields

            private static int idCounter;

            #endregion

            #region Instance Fields

            #region Protected Fields

            protected readonly Collection<SerializationEventsClass> children = new Collection<SerializationEventsClass>();

            #endregion

            #region Private Fields

            [NonSerialized]
            private IntPtr privatePointer;
            [NonSerialized]
            private SerializationEventsClass parent;

            #endregion

            #endregion

            #endregion

            #region Properties

            public int Id { get; set; }

            public string Name { get; set; }

            public SerializationEventsClass Parent => parent;

            public ICollection<SerializationEventsClass> Children => children;

            #endregion

            #region Constructors

            public SerializationEventsClass()
            {
                Id = ++idCounter;
            }

            #endregion

            #region Methods

            #region Public Methods

            public SerializationEventsClass AddChild(string name)
            {
                SerializationEventsClass child = new SerializationEventsClass { Name = name };
                children.Add(child);
                child.parent = this;
                privatePointer = new IntPtr(children.Count);
                return child;
            }

            public virtual void OnDeserialization(object sender)
            {
                //Console.WriteLine("OnDeserialization {0}", this);
                if (children != null)
                {
                    foreach (SerializationEventsClass child in children)
                    {
                        child.parent = this;
                    }
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SerializationEventsClass other))
                    return base.Equals(obj);

                return Id == other.Id
                    && privatePointer == other.privatePointer
                    && (parent == null && other.parent == null || parent != null && other.parent != null && parent.Id == other.parent.Id)
                    && children.SequenceEqual(other.children);
            }

            public override string ToString() => $"{Id} - {Name ?? "<null>"}";

            #endregion

            #region Private Methods

            [OnSerializing]
            private void OnSerializing(StreamingContext ctx)
            {
                //Console.WriteLine("OnSerializing {0}", this);
                privatePointer = IntPtr.Zero;
            }

            [OnSerialized]
            private void OnSerialized(StreamingContext ctx)
            {
                //Console.WriteLine("OnSerialized {0}", this);
                if (children.Count > 0)
                    privatePointer = new IntPtr(children.Count);
            }

            [OnDeserializing]
            private void OnDeserializing(StreamingContext ctx)
            {
                //Console.WriteLine("OnDeserializing {0}", this);
                privatePointer = new IntPtr(-1);
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext ctx)
            {
                //Console.WriteLine("OnDeserialized {0}", this);
                if (children != null)
                    privatePointer = new IntPtr(children.Count);
            }

            #endregion

            #endregion
        }

        #endregion

        #region CustomSerializedClass class

        [Serializable]
        private class CustomSerializedClass : SerializationEventsClass, ISerializable
        {
            #region Properties

            public bool? Bool { get; set; }

            #endregion

            #region Constructors

            #region Public Constructors

            public CustomSerializedClass()
            {
            }

            #endregion

            #region Private Constructors

            private CustomSerializedClass(SerializationInfo info, StreamingContext context)
            {
                Id = info.GetInt32("Id");
                Name = info.GetString("Name");
                Bool = (bool?)info.GetValue("Bool", typeof(bool?));
                ((Collection<SerializationEventsClass>)info.GetValue("Children", typeof(Collection<SerializationEventsClass>))).ForEach(child => children.Add(child));
            }

            #endregion

            #endregion

            #region Methods

            #region Public Methods

            [SecurityCritical]
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Id", Id);
                info.AddValue("Name", Name);
                info.AddValue("Bool", Bool, typeof(bool?));
                info.AddValue("Children", Children);
                info.AddValue("dummy", null, typeof(List<string[]>));
            }

            public override bool Equals(object obj) => !(obj is CustomSerializedClass other) ? base.Equals(obj) : Bool == other.Bool && base.Equals(obj);

            #endregion

            #region Private Methods

            [OnSerialized]
            private void OnSerialized(StreamingContext ctx)
            {
                //Console.WriteLine("OnSerialized derived {0}", this);
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext ctx)
            {
                //Console.WriteLine("OnDeserialized derived {0}", this);
            }

            #endregion

            #endregion
        }

        #endregion

        #region CustomSerializedSealedClass class

        [Serializable]
        private sealed class CustomSerializedSealedClass : CustomSerializedClass, ISerializable
        {
            #region Constructors

            #region Public Constructors

            public CustomSerializedSealedClass(string name)
            {
                Name = name;
            }

            #endregion

            #region Internal Constructors

            internal CustomSerializedSealedClass(int id, string name, IEnumerable<SerializationEventsClass> children, bool? boolean)
            {
                Id = id;
                Name = name;
                children.ForEach(child => this.children.Add(child));
                Bool = boolean;
            }

            #endregion

            #region Private Constructors

            private CustomSerializedSealedClass(SerializationInfo info, StreamingContext context)
            {
                throw new InvalidOperationException("Never executed");
            }

            #endregion

            #endregion

            #region Methods

            [SecurityCritical]
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.SetType(typeof(CustomAdvancedSerializedClassHelper));
                base.GetObjectData(info, context);
            }

            #endregion
        }

        #endregion

        #region CustomAdvancedSerializedClassHelper class

        [Serializable]
        private class CustomAdvancedSerializedClassHelper : IObjectReference, ISerializable, IDeserializationCallback
        {
            #region Fields

            readonly CustomSerializedSealedClass toDeserialize;

            #endregion

            #region Constructors

            private CustomAdvancedSerializedClassHelper(SerializationInfo info, StreamingContext context)
            {
                toDeserialize = new CustomSerializedSealedClass(info.GetInt32("Id"), info.GetString("Name"),
                    (Collection<SerializationEventsClass>)info.GetValue("Children", typeof(Collection<SerializationEventsClass>)),
                    (bool?)info.GetValue("Bool", typeof(bool?)));
            }

            #endregion

            #region Methods

            #region Public Methods

            [SecurityCritical]
            public object GetRealObject(StreamingContext context)
            {
                return toDeserialize;
            }

            [SecurityCritical]
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new NotImplementedException("Never executed");
            }

            public void OnDeserialization(object sender)
            {
                toDeserialize.OnDeserialization(sender);
            }

            #endregion

            #region Private Methods

            [OnDeserialized]
            private void OnDeserialized(StreamingContext ctx)
            {
                //Console.WriteLine("OnDeserialized Helper");
                Reflector.SetField(toDeserialize, "privatePointer", new IntPtr(toDeserialize.Children.Count));
            }

            #endregion

            #endregion
        }

        #endregion

        #region DefaultGraphObjRef class

        [Serializable]
        private class DefaultGraphObjRef : IObjectReference
        {
            #region Fields

            #region Static Fields

            private readonly static DefaultGraphObjRef instance = new DefaultGraphObjRef("singleton instance");

            #endregion

            #region Instance Fields

            private readonly string name;

            #endregion

            #endregion

            #region Constructors

            private DefaultGraphObjRef(string name)
            {
                this.name = name;
            }

            #endregion

            #region Methods

            #region Static Methods

            public static DefaultGraphObjRef Get()
            {
                return instance;
            }

            #endregion

            #region Instance Methods

            public override string ToString()
            {
                return name;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(obj, instance);
            }

            [SecurityCritical]
            public object GetRealObject(StreamingContext context)
            {
                return instance;
            }

            #endregion

            #endregion
        }

        #endregion

        #region CustomGraphDefaultObjRef class

        [Serializable]
        private sealed class CustomGraphDefaultObjRef : ISerializable
        {
            #region Properties

            public string Name { get; set; }

            #endregion

            #region Methods

            [SecurityCritical]
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("name", Name);
                info.SetType(typeof(CustomGraphDefaultObjRefDeserializer));
            }

            #endregion
        }

        #endregion

        #region CustomGraphDefaultObjRefDeserializer class

        [Serializable]
        private class CustomGraphDefaultObjRefDeserializer : IObjectReference
        {
            #region Fields

#pragma warning disable 649
            private readonly string name;
#pragma warning restore 649

            #endregion

            #region Methods

            [SecurityCritical]
            public object GetRealObject(StreamingContext context)
            {
                return new CustomGraphDefaultObjRef { Name = name };
            }

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

        #region ListField class

        [Serializable]
        private class ListField
        {
            #region Fields

            public List<int> IntListField; 
            
            #endregion
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

        #region OpenGenericDictionary class

        private class OpenGenericDictionary<TValue> : Dictionary<string, TValue>
        {
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

        #region MemoryStreamWithEquals class

        [Serializable]
        private sealed class MemoryStreamWithEquals : MemoryStream
        {
            #region Methods

            public override bool Equals(object obj)
            {
                if (!(obj is MemoryStreamWithEquals other))
                    return base.Equals(obj);

                return this.CanRead == other.CanRead && this.CanSeek == other.CanSeek && this.CanTimeout == other.CanTimeout && this.CanWrite == other.CanWrite
                    && this.Capacity == other.Capacity && this.Length == other.Length && this.Position == other.Position && this.GetBuffer().SequenceEqual(other.GetBuffer());
            }

            #endregion
        }

        #endregion

        #region CircularReferenceClass class

        [Serializable]
        private sealed class CircularReferenceClass
        {
            #region Fields

            #region Static Fields

            private static int idCounter;

            #endregion

            #region Instance Fields

            private readonly Collection<CircularReferenceClass> children = new Collection<CircularReferenceClass>();

            private CircularReferenceClass parent;

            #endregion

            #endregion

            #region Properties

            public int Id { get; private set; }

            public string Name { get; set; }

            public CircularReferenceClass Parent => parent;

            public Collection<CircularReferenceClass> Children => children;

            #endregion

            #region Constructors

            public CircularReferenceClass()
            {
                Id = ++idCounter;
            }

            #endregion

            #region Methods

            public CircularReferenceClass AddChild(string name)
            {
                CircularReferenceClass child = new CircularReferenceClass { Name = name };
                children.Add(child);
                child.parent = this;
                return child;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CircularReferenceClass other))
                    return base.Equals(obj);

                return Id == other.Id
                    && (parent == null && other.parent == null || parent != null && other.parent != null && parent.Id == other.parent.Id)
                    && children.SequenceEqual(other.children); // can cause stack overflow
            }

            public override string ToString()
            {
                return $"{Id} - {Name ?? "<null>"}";
            }

            #endregion
        }

        #endregion

        #region Box<T> class

        [Serializable]
        private sealed class Box<T>
        {
            #region Fields

            #region Internal Fields

            internal readonly T Value;

            #endregion

            #region Private Fields

            private readonly T[] valueArray;
            private readonly List<T> valueList;
            private readonly Dictionary<T, T> keyValueUsageDictionary;
            private readonly Dictionary<T, string> keyUsageDictionary;
            private readonly Dictionary<string, T> valueUsageDictionary;
            private readonly object valueKeyValuePair;
            private readonly object valueDictionaryEntry;
            private readonly LinkedList<T> valueLinkedList;
            private readonly HashSet<T> valueHashSet;
            private readonly OrderedDictionary valueOrderedDictionary;

            #endregion

            #endregion

            #region Constructors

            internal Box(T value)
            {
                Value = value;
                valueArray = new[] { value };
                valueList = new List<T>(1) { value };
                keyValueUsageDictionary = new Dictionary<T, T>(1) { { value, value } };
                keyUsageDictionary = new Dictionary<T, string>(1) { { value, value.ToString() } };
                valueUsageDictionary = new Dictionary<string, T>(1) { { value.ToString(), value } };
                valueKeyValuePair = new KeyValuePair<T, string>(value, value.ToString());
                valueDictionaryEntry = new DictionaryEntry(value.ToString(), value);
                valueLinkedList = new LinkedList<T>(new[] { value });
                valueHashSet = new HashSet<T> { value };
                valueOrderedDictionary = new OrderedDictionary { { value, value } };
            }

            #endregion

            #region Methods

            public override bool Equals(object obj) =>
                obj is Box<T> other
                && Equals(Value, other.Value)
                && Equals(valueArray[0], other.valueArray[0])
                && Equals(valueList[0], other.valueList[0])
                && Equals(keyValueUsageDictionary[Value], other.keyValueUsageDictionary[Value])
                && Equals(keyUsageDictionary[Value], other.keyUsageDictionary[Value])
                && Equals(valueUsageDictionary[Value.ToString()], other.valueUsageDictionary[Value.ToString()])
                && Equals(((KeyValuePair<T, string>)valueKeyValuePair).Key, ((KeyValuePair<T, string>)other.valueKeyValuePair).Key)
                && Equals(((DictionaryEntry)valueDictionaryEntry).Value, ((DictionaryEntry)other.valueDictionaryEntry).Value)
                && Equals(valueLinkedList.First.Value, other.valueLinkedList.First.Value)
                && Equals(valueHashSet.First(), other.valueHashSet.First())
                && Equals(valueOrderedDictionary[Value], other.valueOrderedDictionary[Value]);

            public override int GetHashCode() => Value?.GetHashCode() ?? 0;

            public override string ToString() => Value?.ToString() ?? base.ToString();

            #endregion
        }

        #endregion

        #region SelfReferencerDirect class

        [Serializable]
        private class SelfReferencerDirect : ISerializable
        {
            #region Fields

            private readonly Box<SelfReferencerDirect> selfBox;

            #endregion

            #region Properties

            public string Name { get; set; }

            public SelfReferencerDirect Self { get; set; }

            #endregion

            #region Constructors

            #region Public Constructors

            public SelfReferencerDirect(string name)
            {
                Name = name;
                Self = this;
                selfBox = new Box<SelfReferencerDirect>(this);
            }

            #endregion

            #region Private Constructors

            private SelfReferencerDirect(SerializationInfo info, StreamingContext context)
            {
                Name = info.GetString("name");
                Self = (SelfReferencerDirect)info.GetValue("self", typeof(SelfReferencerDirect));
                selfBox = (Box<SelfReferencerDirect>)info.GetValue("selfBox", typeof(Box<SelfReferencerDirect>));
            }

            #endregion

            #endregion

            #region Methods

            [SecurityCritical]
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("name", Name);
                info.AddValue("self", Self);
                info.AddValue("selfBox", selfBox);
            }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(SelfReferencerDirect))
                    return false;

                var other = (SelfReferencerDirect)obj;
                return other.Name == this.Name && ReferenceEquals(other, other.Self) && ReferenceEquals(this, this.Self);
            }

            #endregion
        }

        #endregion

        #region SelfReferencerIndirect class

        [Serializable]
        private class SelfReferencerIndirect : ISerializable
        {
            #region Nested Classes

            #region SelfReferencerIndirectDefaultDeserializer class

            [Serializable]
            private class SelfReferencerIndirectDefaultDeserializer : IObjectReference
            {
                #region Fields

#pragma warning disable 649
                public string Name;
                public Box<SelfReferencerIndirect> SelfRef;
                public bool UseValidWay;
                public bool UseCustomDeserializer;
#pragma warning restore 649

                #endregion

                #region Methods

                [SecurityCritical]
                public object GetRealObject(StreamingContext context)
                {
                    return UseValidWay
                        ? new SelfReferencerIndirect(Name)
                        {
                            UseCustomDeserializer = UseCustomDeserializer,
                            UseValidWay = UseValidWay
                        }
                        : SelfRef.Value;
                }

                #endregion
            }

            #endregion

            #region SelfReferencerIndirectCustomDeserializer class

            [Serializable]
            private class SelfReferencerIndirectCustomDeserializer : ISerializable, IObjectReference
            {
                #region Fields

                private readonly string name;
                private readonly Box<SelfReferencerIndirect> selfRef;
                private readonly bool useValidWay;
                private readonly bool useCustomDeserializer;

                #endregion

                #region Constructors

                private SelfReferencerIndirectCustomDeserializer(SerializationInfo info, StreamingContext context)
                {
                    name = info.GetString(nameof(SelfReferencerIndirect.Name));
                    selfRef = (Box<SelfReferencerIndirect>)info.GetValue(nameof(SelfReferencerIndirect.SelfRef), typeof(Box<SelfReferencerIndirect>));
                    useValidWay = info.GetBoolean(nameof(SelfReferencerIndirect.UseValidWay));
                    useCustomDeserializer = info.GetBoolean(nameof(SelfReferencerIndirect.UseCustomDeserializer));
                }

                #endregion

                #region Methods

                [SecurityCritical]
                public object GetRealObject(StreamingContext context)
                {
                    return useValidWay
                        ? new SelfReferencerIndirect(name)
                        {
                            UseCustomDeserializer = useCustomDeserializer,
                            UseValidWay = useValidWay
                        }
                        : selfRef.Value;
                }

                public void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                    throw new InvalidOperationException("Should not be executed");
                }

                #endregion
            }

            #endregion

            #endregion

            #region Properties

            public string Name { get; }
            public Box<SelfReferencerIndirect> SelfRef { get; }
            public bool UseValidWay { get; set; }
            public bool UseCustomDeserializer { get; set; }

            #endregion

            #region Constructors

            public SelfReferencerIndirect(string name)
            {
                Name = name;
                SelfRef = new Box<SelfReferencerIndirect>(this);
            }

            #endregion

            #region Methods

            [SecurityCritical]
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(Name), Name);
                info.AddValue(nameof(SelfRef), SelfRef);
                info.AddValue(nameof(UseValidWay), UseValidWay);
                info.AddValue(nameof(UseCustomDeserializer), UseCustomDeserializer);
                info.SetType(UseCustomDeserializer ? typeof(SelfReferencerIndirectCustomDeserializer) : typeof(SelfReferencerIndirectDefaultDeserializer));
            }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(SelfReferencerIndirect))
                    return false;

                var other = (SelfReferencerIndirect)obj;
                return other.Name == this.Name && ReferenceEquals(other, other.SelfRef.Value) && ReferenceEquals(this, this.SelfRef.Value);
            }

            #endregion
        }

        #endregion

        #region SelfReferencerInvalid class

        [Serializable]
        private class SelfReferencerInvalid : SelfReferencerDirect
        {
            #region Nested Classes

            #region SelfReferencerInvalidDefaultDeserializer class

            [Serializable]
            private class SelfReferencerInvalidDefaultDeserializer : IObjectReference
            {
                #region Fields

#pragma warning disable 169, IDE0051
                private readonly string name;
#pragma warning disable 649
                private readonly SelfReferencerDirect self;
#pragma warning restore 649
                private readonly Box<SelfReferencerDirect> selfBox;
#pragma warning restore 169, IDE0051

                #endregion

                #region Methods

                [SecurityCritical]
                public object GetRealObject(StreamingContext context)
                {
                    return self;
                }

                #endregion
            }

            #endregion

            #region SelfReferencerInvalidCustomDeserializer class

            [Serializable]
            private class SelfReferencerInvalidCustomDeserializer : IObjectReference, ISerializable
            {
                #region Fields

#pragma warning disable IDE0052
                private readonly SelfReferencerDirect instance;
#pragma warning restore IDE0052
                private readonly string name;

                #endregion

                #region Constructors

                protected SelfReferencerInvalidCustomDeserializer(SerializationInfo info, StreamingContext context)
                {
                    name = info.GetString("name");
                    instance = (SelfReferencerDirect)info.GetValue("self", typeof(SelfReferencerDirect));
                }

                #endregion

                #region Methods

                [SecurityCritical]
                public object GetRealObject(StreamingContext context)
                {
                    return new SelfReferencerInvalid(name);
                }

                [SecurityCritical]
                public void GetObjectData(SerializationInfo info, StreamingContext context)
                {
                    throw new NotImplementedException();
                }

                #endregion
            }

            #endregion

            #endregion

            #region Properties

            public bool UseCustomDeserializer { get; set; }

            #endregion

            #region Constructors

            public SelfReferencerInvalid(string name)
                : base(name)
            {
            }

            #endregion

            #region Methods

            [SecurityCritical]
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                base.GetObjectData(info, context);
                info.SetType(UseCustomDeserializer ? typeof(SelfReferencerInvalidCustomDeserializer) : typeof(SelfReferencerInvalidDefaultDeserializer));
            }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != typeof(SelfReferencerInvalid))
                    return false;

                var other = (SelfReferencerInvalid)obj;
                return other.Name == this.Name && ReferenceEquals(other, other.Self) && ReferenceEquals(this, this.Self);
            }

            #endregion
        }

        #endregion

        #region GetObjectDataSetsUnknownType class

        [Serializable]
        private class GetObjectDataSetsUnknownType : ISerializable
        {
            #region Constructors

            public GetObjectDataSetsUnknownType()
            {
            }

            private GetObjectDataSetsUnknownType(SerializationInfo si, StreamingContext ctx)
            {
            }

            #endregion

            #region Methods
            
            [SecurityCritical]
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.FullTypeName = nameof(GetObjectDataSetsUnknownType);
            }
            
            #endregion
        }

        #endregion

        #region GetObjectDataSetsInvalidType class

        [Serializable]
        private class GetObjectDataSetsInvalidType : ISerializable
        {
            #region Constructors

            public GetObjectDataSetsInvalidType()
            {
            }
            private GetObjectDataSetsInvalidType(SerializationInfo si, StreamingContext ctx)
            {
            }

            #endregion

            #region Methods

            [SecurityCritical]
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.SetType(typeof(List<>).GetGenericArguments()[0]);
            }

            #endregion
        }

        #endregion

        #region TestSurrogateSelector class

        private class TestSurrogateSelector : ISurrogateSelector, ISerializationSurrogate
        {
            #region Fields

            private ISurrogateSelector next;

            #endregion

            #region Methods

            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
            public void ChainSelector(ISurrogateSelector selector) => next = selector;

            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
            public ISurrogateSelector GetNextSelector() => next;

            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
            public ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                if (!type.IsPrimitive && !type.IsArray /*&& !typeof(ISerializable).IsAssignableFrom(type)*/ && type != typeof(string))
                {
                    selector = this;
                    return this;
                }

                if (next != null)
                    return next.GetSurrogate(type, context, out selector);

                selector = null;
                return null;

            }

            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
            public virtual void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));
                if (info == null)
                    throw new ArgumentNullException(nameof(info));

                Type type = obj.GetType();

                for (Type t = type; t != typeof(object); t = t.BaseType)
                {
                    FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(f => !f.IsNotSerialized).ToArray();
                    foreach (FieldInfo field in fields)
                        info.AddValue(field.Name, field.Get(obj));
                }
            }

            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
            public virtual object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));
                if (info == null)
                    throw new ArgumentNullException(nameof(info));

                foreach (SerializationEntry entry in info)
                    Reflector.SetField(obj, entry.Name, entry.Value);

                return obj;
            }

            #endregion
        }

        #endregion

        #region TestSurrogateSelector class

        private class TestCloningSurrogateSelector : TestSurrogateSelector
        {
            #region Methods


            [SecurityCritical]
            public override object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) 
                => base.SetObjectData(Reflector.InvokeMethod(obj, nameof(MemberwiseClone)), info, context, selector);

            #endregion
        }

        #endregion

        #region Singleton1 class

        [Serializable]
        private class Singleton1 : ISerializable
        {
            #region Properties

            public static Singleton1 Instance { get; } = new Singleton1();

            #endregion

            #region Constructors

            private Singleton1()
            {
            }

            #endregion

            #region Methods

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // by SetType
                info.AddValue("id", 1);
                info.SetType(typeof(SingletonDeserializer));
            }

            public override bool Equals(object obj) => obj is Singleton1;

            #endregion
        }

        #endregion

        #region Singleton2 class

        [Serializable]
        private class Singleton2 : ISerializable
        {
            #region Properties

            public static Singleton2 Instance { get; } = new Singleton2();

            #endregion

            #region Constructors

            private Singleton2()
            {
            }

            #endregion

            #region Methods

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // By string, different assembly name
                info.AddValue("id", 2);
                info.AssemblyName = typeof(SingletonDeserializer).Assembly.GetName().Name;
                info.FullTypeName = typeof(SingletonDeserializer).GetName(TypeNameKind.FullName);
            }

            public override bool Equals(object obj) => obj is Singleton2;

            #endregion
        }

        #endregion

        #region Singleton3 class

        [Serializable]
        private class Singleton3 : ISerializable
        {
            #region Properties

            public static Singleton3 Instance { get; } = new Singleton3();

            #endregion

            #region Constructors

            private Singleton3()
            {
            }

            #endregion

            #region Methods

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // By string, same name as by SetType
                info.AddValue("id", 3);
                info.AssemblyName = typeof(SingletonDeserializer).Assembly.FullName;
                info.FullTypeName = typeof(SingletonDeserializer).GetName(TypeNameKind.FullName);
            }

            public override bool Equals(object obj) => obj is Singleton3;

            #endregion
        }

        #endregion

        #region SingletonDeserializer class

        [Serializable]
        private class SingletonDeserializer : ISerializable, IObjectReference
        {
            #region Fields

            private readonly int i;

            #endregion

            #region Constructors

            private SingletonDeserializer(SerializationInfo info, StreamingContext context) => i = info.GetInt32("id");

            #endregion

            #region Methods

            public object GetRealObject(StreamingContext context) => i switch
            {
                1 => Singleton1.Instance,
                2 => Singleton2.Instance,
                _ => (object)Singleton3.Instance
            };

            public void GetObjectData(SerializationInfo info, StreamingContext context) => throw new NotImplementedException();

            #endregion
        }

        #endregion

        #region NullReference class

        [Serializable]
        private class NullReference : IObjectReference
        {
            #region Methods

            public object GetRealObject(StreamingContext context) => null;

            #endregion
        }

        #endregion

        #endregion

        #region Nested structs

        #region NonSerializableStruct struct

        private struct NonSerializableStruct
        {
            #region Fields

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            private string str10;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private byte[] bytes3;

            #endregion

            #region Properties

            public int IntProp { get; set; }

            public string Str10
            {
                get => str10;
                set => str10 = value;
            }

            public byte[] Bytes3
            {
                get => bytes3;
                set => bytes3 = value;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Overridden for the test equality check
            /// </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is NonSerializableStruct))
                    return base.Equals(obj);
                NonSerializableStruct other = (NonSerializableStruct)obj;
                return str10 == other.str10 && IntProp == other.IntProp
                    && ((bytes3 == null && other.bytes3 == null) || (bytes3 != null && other.bytes3 != null
                                && bytes3[0] == other.bytes3[0] && bytes3[1] == other.bytes3[1] && bytes3[2] == other.bytes3[2]));
            }

            #endregion
        }

        #endregion

        #region BinarySerializableStruct struct

        [Serializable]
        private struct BinarySerializableStruct : IBinarySerializable
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            #endregion

            #region Constructors

            [SuppressMessage("VS", "IDE0060:Remove unused parameter", Justification = "Special constructor")]
            public BinarySerializableStruct(BinarySerializationOptions options, byte[] serData)
                : this()
            {
                using (BinaryReader br = new BinaryReader(new MemoryStream(serData)))
                {
                    IntProp = br.ReadInt32();
                    if (br.ReadBoolean())
                        StringProp = br.ReadString();
                }
            }

            #endregion

            #region Methods

            #region Public Methods

            public byte[] Serialize(BinarySerializationOptions options)
            {
                MemoryStream ms = new MemoryStream();
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(IntProp);
                    bw.Write(StringProp != null);
                    if (StringProp != null)
                        bw.Write(StringProp);
                }

                return ms.ToArray();
            }

            public void Deserialize(BinarySerializationOptions options, byte[] serData)
            {
                throw new InvalidOperationException("This method never will be called");
            }

            #endregion

            #region Private Methods

            [OnDeserializing]
            private void OnDeserializing(StreamingContext ctx)
            {
                IntProp = -1;
            }

            #endregion

            #endregion
        }

        #endregion

        #region SystemSerializableStruct struct

        [Serializable]
        private struct SystemSerializableStruct
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

        #region CustomSerializableStruct struct

        [Serializable]
        private struct CustomSerializableStruct : ISerializable
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

            #endregion

            #region Constructors

            private CustomSerializableStruct(SerializationInfo info, StreamingContext context)
                : this()
            {
                IntProp = info.GetInt32("Int");
                StringProp = info.GetString("String");
            }

            #endregion

            #region Methods

            #region Public Methods

            [SecurityCritical]
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("Int", IntProp);
                info.AddValue("String", StringProp);
            }

            #endregion

            #region Private Methods

            [OnDeserializing]
            private void OnDeserializing(StreamingContext ctx)
            {
                IntProp = -1;
            }

            #endregion

            #endregion
        }

        #endregion

        #region BinarySerializableStructNoCtor struct

        [Serializable]
        private struct BinarySerializableStructNoCtor : IBinarySerializable
        {
            #region Properties

            public int IntProp { get; set; }

            public string StringProp { get; set; }

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
                using (BinaryReader br = new BinaryReader(new MemoryStream(serData)))
                {
                    IntProp = br.ReadInt32();
                    StringProp = br.ReadString();
                }
            }

            #endregion
        }

        #endregion

        #region UnsafeStruct struct

        [Serializable]
        private unsafe struct UnsafeStruct
        {
            #region Fields

            public void* VoidPointer;
            public int* IntPointer;
            public Point* StructPointer;
            public int*[] PointerArray;
            public void** PointerOfPointer;

            #endregion
        }

        #endregion

        #region LargeStructToBeMarshaled

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LargeStructToBeMarshaled
        {
            #region Fields

#pragma warning disable 649 // Field is never assigned to, and will always have its default value - false alarm, values are random generated
            [MarshalAs(UnmanagedType.U1)]
            internal bool Bool;
            internal byte Byte;
            internal sbyte SByte;
            internal short Short;
            internal ushort UShort;
            internal int Int;
            internal uint UInt;
            internal long Long;
            internal ulong ULong;
            internal float Float;
            internal double Double;
            internal decimal Decimal;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            internal string ValueString;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] ValueBytes;
#pragma warning restore 649

            #endregion
        }

        #endregion

        #region LargeUnmanagedStruct

        private unsafe struct LargeUnmanagedStruct
        {
            #region Fields

#pragma warning disable 649 // Field is never assigned to, and will always have its default value - false alarm, values are random generated
            internal bool Bool;
            internal byte Byte;
            internal sbyte SByte;
            internal short Short;
            internal ushort UShort;
            internal int Int;
            internal uint UInt;
            internal long Long;
            internal ulong ULong;
            internal float Float;
            internal double Double;
            internal decimal Decimal;
            internal ConsoleColor Color;
            internal Guid Guid;
            internal fixed char ValueString[16];
            internal fixed byte ValueBytes[16];
#pragma warning restore 649

            #endregion
        }

        #endregion

        #endregion

#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        #endregion
    }
}
