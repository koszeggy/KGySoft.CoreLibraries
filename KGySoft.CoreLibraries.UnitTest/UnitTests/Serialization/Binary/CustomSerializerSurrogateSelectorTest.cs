#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CustomSerializerSurrogateSelectorTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
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
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

using NUnit.Framework;
using NUnit.Framework.Internal;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Binary
{
    [TestFixture]
    [Obsolete]
    public class CustomSerializerSurrogateSelectorTest : TestBase
    {
        #region Nested classes
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

        #region ConflictNameBase class

        [Serializable]
        private class ConflictNameBase
        {
            #region Fields

            public readonly string ConflictingFieldPublic;
            internal readonly string ConflictingFieldInternal;
            private readonly string conflictingFieldPrivate;

            #endregion

            #region Constructors

            protected ConflictNameBase(string valuePublic, string valueInternal, string valuePrivate)
            {
                ConflictingFieldPublic = valuePublic;
                ConflictingFieldInternal = valueInternal;
                conflictingFieldPrivate = valuePrivate;
            }

            #endregion

            #region Methods

            public override bool Equals(object obj)
            {
                ConflictNameBase other;
                return obj?.GetType() == GetType()
                    && (other = (ConflictNameBase)obj).ConflictingFieldPublic == ConflictingFieldPublic
                    && other.ConflictingFieldInternal == ConflictingFieldInternal
                    && other.conflictingFieldPrivate == conflictingFieldPrivate;
            }

            #endregion
        }

        #endregion

        #region ConflictNameChild class

        [Serializable]
        private class ConflictNameChild : ConflictNameBase
        {
            #region Fields

            public readonly new int ConflictingFieldPublic;
            internal readonly new int ConflictingFieldInternal;
            private readonly int conflictingFieldPrivate;

            #endregion

            #region Constructors
            
            internal ConflictNameChild(int valuePublic, int valueInternal, int valuePrivate, string valuePublicBase, string valueInternalBase, string valuePrivateBase) : base(valuePublicBase, valueInternalBase, valuePrivateBase)
            {
                ConflictingFieldPublic = valuePublic;
                ConflictingFieldInternal = valueInternal;
                conflictingFieldPrivate = valuePrivate;
            }

            #endregion

            #region Methods

            public override bool Equals(object obj)
            {
                ConflictNameChild other;
                return base.Equals(obj) &&
                    (other = (ConflictNameChild)obj).ConflictingFieldPublic == ConflictingFieldPublic
                    && other.ConflictingFieldInternal == ConflictingFieldInternal
                    && other.conflictingFieldPrivate == conflictingFieldPrivate;
            }

            #endregion
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

            private readonly Collection<SerializationEventsClass> children = new Collection<SerializationEventsClass>();

            [NonSerialized]
            private IntPtr privatePointer;
            [NonSerialized]
            private SerializationEventsClass parent;

            #endregion

            #endregion

            #region Properties

            public int Id { get; }

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
                privatePointer = IntPtr.Zero;
            }

            [OnSerialized]
            private void OnSerialized(StreamingContext ctx)
            {
                if (children.Count > 0)
                    privatePointer = new IntPtr(children.Count);
            }

            [OnDeserializing]
            private void OnDeserializing(StreamingContext ctx)
            {
                privatePointer = new IntPtr(-1);
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext ctx)
            {
                if (children != null)
                    privatePointer = new IntPtr(children.Count);
            }

            #endregion

            #endregion
        }

        #endregion

        #region UnsafeStruct struct

        [Serializable]
        private unsafe struct UnsafeStruct
        {
            #region Fields

#pragma warning disable 649
            public void* VoidPointer;
            public int* IntPointer;
            public int*[] PointerArray;
            public void** PointerOfPointer;
#pragma warning restore 649

            #endregion
        }

        #endregion

        #region ChangedClassOld class

        [Serializable]
        private class ChangedClassOld
        {
            #region Fields

            // ReSharper disable InconsistentNaming
            internal int m_IntField;
            internal string m_StringField;
            // ReSharper restore InconsistentNaming

            #endregion
        }

        #endregion

        #region ChangedClassNew class

        [Serializable]
        private class ChangedClassNew
        {
            #region Fields

#pragma warning disable 649
            internal int IntField;
            internal string StringField;
#pragma warning restore 649

            #endregion
        }

        #endregion

        #region BinarySerializable class

        [Serializable]
        private class BinarySerializable : IBinarySerializable
        {
            #region Properties

            public int IntProp { get; set; }

            #endregion

            #region Methods

            public byte[] Serialize(BinarySerializationOptions options) => BitConverter.GetBytes(IntProp);

            public void Deserialize(BinarySerializationOptions options, byte[] serData) => IntProp = BitConverter.ToInt32(serData, 0);

            #endregion
        }

        #endregion

        #region BinarySerializable class

        private class NonSerializableClass
        {
            #region Properties

            public int IntProp { get; set; }

            #endregion
        }

        #endregion

#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        #endregion

        #region Constants

        private const bool dumpSerContent = false;

        #endregion

        #region Fields

        private static readonly object[] testCases =
        {
            // primitive types
            1,
            "alpha",

            // normally serialized
             new List<int> { 1 },

            // pointer fields
            //new UnsafeStruct(), // - CustomizationTest uses reflector

            // normal serializable class with serialization events and NonSerialized fields
            new SerializationEventsClass { Name = "Parent" }.AddChild("Child").Parent,

            // ISerializable class
            new Exception("message"),

            // IBinarySerializable
            new BinarySerializable { IntProp = 42 },

            // Compact serializable
            new Point(1, 2),

            // contains primitive, optionally customizable, always recursive and self type
            new List<object>
            {
                1,
                DateTime.Today,
                ConsoleColor.Blue,
                new BinarySerializable { IntProp = 42 },
                new Point(1, 2),
                new List<object> { 1 }
            },
        };

        #endregion

        #region Methods

        #region Static Methods

        private static void DoTest(IFormatter formatter, ISurrogateSelector surrogate, object obj, bool throwError, bool forWriting, bool forReading)
        {
            Console.Write($"{obj} by {formatter.GetType().Name}: ");
            formatter.SurrogateSelector = forWriting ? surrogate : null;
            TestExecutionContext.IsolatedContext context = throwError ? null : new TestExecutionContext.IsolatedContext();

            try
            {
                using (var ms = new MemoryStream())
                {
                    try
                    {
                        formatter.Serialize(ms, obj);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Serialization failed: {e}");
                        if (throwError)
                            throw;
                        return;
                    }

                    Console.WriteLine($"{ms.Length} bytes.");
                    if (dumpSerContent)
#pragma warning disable 162
                        Console.WriteLine(ms.ToArray().ToRawString());
#pragma warning restore 162

                    formatter.SurrogateSelector = forReading ? surrogate : null;
                    ms.Position = 0L;
                    try
                    {
                        object result = formatter.Deserialize(ms);
                        AssertDeepEquals(obj, result);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Deserialization failed: {e}");
                        if (throwError)
                            throw;
                    }
                }
            }
            finally
            {
                context?.Dispose();
            }
        }

        #endregion

        #region Instance Methods

        [TestCaseSource(nameof(testCases))]
        public void BaselineTestWithoutUsingSurrogate(object obj)
        {
#if !NET9_0_OR_GREATER
            DoTest(new BinaryFormatter(), null, obj, false, false, false);
#endif
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.None), null, obj, true, false, false);
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes), null, obj, true, false, false);
        }

        [TestCaseSource(nameof(testCases))]
        public void ReadAndWriteWithSurrogate(object obj)
        {
            ISurrogateSelector surrogate = new CustomSerializerSurrogateSelector();

#if !NET9_0_OR_GREATER
            DoTest(new BinaryFormatter(), surrogate, obj, false, true, true);
#endif
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.None), surrogate, obj, true, true, true);
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes), surrogate, obj, true, true, true);
        }


        [TestCaseSource(nameof(testCases))]
        public void ReadWithSurrogate(object obj)
        {
            ISurrogateSelector surrogate = new CustomSerializerSurrogateSelector();

#if !NET9_0_OR_GREATER
            DoTest(new BinaryFormatter(), surrogate, obj, false, false, true);
#endif
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.None), surrogate, obj, true, false, true);
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.TryUseSurrogateSelectorForAnyType), surrogate, obj, true, false, true);
        }

        [TestCaseSource(nameof(testCases))]
        public void WriteWithSurrogate(object obj)
        {
            ISurrogateSelector surrogate = new CustomSerializerSurrogateSelector();

#if !NET9_0_OR_GREATER
            DoTest(new BinaryFormatter(), surrogate, obj, false, true, false);
#endif
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.None), surrogate, obj, true, true, false);
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.TryUseSurrogateSelectorForAnyType), surrogate, obj, true, false, true);
        }

        [Test]
        public void SerializeClassWithConflictingFields()
        {
            object obj = new ConflictNameChild(1, 2, 3, "Public Base", "Protected Base", "Private Base");
            ISurrogateSelector surrogate = new CustomSerializerSurrogateSelector();
#if !NET9_0_OR_GREATER
            var bf = new BinaryFormatter();
#endif
            var bsf = new BinarySerializationFormatter(BinarySerializationOptions.None);

            // not using surrogate: tests if the formatter can handle the situation internally
#if !NET9_0_OR_GREATER
            DoTest(bf, null, obj, false, false, false);
#endif
            DoTest(bsf, null, obj, true, false, false);

            // using surrogate for both ways
#if !NET9_0_OR_GREATER
            DoTest(bf, surrogate, obj, false, true, true);
#endif
            DoTest(bsf, surrogate, obj, true, true, true);

            // default serialization by surrogate: the formatter must add unique names to the serialization info
            // and the surrogate must resolve these names somehow (can be solved by events)
            //DoTest(bf, surrogate, obj, false, false, true); // SerializationException : Cannot add the same member twice to a SerializationInfo object (BF tries to add the same field names for public fields)
            DoTest(bsf, surrogate, obj, true, false, true);

            // surrogate serialization by default: the surrogate must add unique names, which should be
            // resolved by the formatter somehow (not really possible without hard coded handling in the formatter)
            //DoTest(bf, surrogate, obj, false, true, false); // Equality check failed for base public field (BF uses class name prefix for non-public fields only)
            DoTest(bsf, surrogate, obj, true, true, false);
        }

        [Test]
        public void IgnoreNonSerializedAttributeTest()
        {
            object obj = new SerializationEventsClass { Name = "Parent" }.AddChild("Child").Parent;
            var surrogate = new CustomSerializerSurrogateSelector();

            // Formatter now omits serialization methods. If the surrogate selector skips non-serialized fields, it will cause a problem.
            var formatter = new BinarySerializationFormatter(BinarySerializationOptions.IgnoreSerializationMethods) { SurrogateSelector = surrogate };
            Throws<AssertionException>(() => DoTest(formatter, surrogate, obj, true, true, true),
                "Equality check failed");

            // But if we force to serialize all fields, even non-serialized ones, the clones will be identical.
            surrogate.IgnoreNonSerializedAttribute = true;
            DoTest(formatter, surrogate, obj, true, true, true);
        }

        [Test]
        public void IgnoreISerializableTest()
        {
            var obj = new Exception("message");
            var surrogate = new CustomSerializerSurrogateSelector();
            var formatter = new BinarySerializationFormatter(BinarySerializationOptions.None) { SurrogateSelector = surrogate };

            DoTest(formatter, surrogate, obj, true, true, true);
            surrogate.IgnoreISerializable = true;
            surrogate.IgnoreNonSerializedAttribute = true;
            DoTest(formatter, surrogate, obj, true, true, true);
        }

        [TestCaseSource(nameof(testCases))]
        public void CustomizationTest(object obj)
        {
            #region Local Methods

            static void Serializing(object sender, SerializingEventArgs e)
            {
                var instance = (CustomSerializerSurrogateSelector)sender;
                Assert.AreEqual(instance.IgnoreISerializable, e.IgnoreISerializable);
                e.IgnoreISerializable = true;
            }

            static void GettingField(object sender, GettingFieldEventArgs e)
            {
                var instance = (CustomSerializerSurrogateSelector)sender;
                Assert.AreEqual(!instance.IgnoreNonSerializedAttribute && e.Field.IsNotSerialized, e.Handled);
                e.Handled = false; // forcing to save non-serialized fields, too
                e.Name = e.Name.Reverse().Convert<string>();
            }

            static void Deserializing(object sender, DeserializingEventArgs e)
            {
                var instance = (CustomSerializerSurrogateSelector)sender;
                Assert.AreEqual(instance.IgnoreISerializable, e.IgnoreISerializable);
                e.IgnoreISerializable = true;
            }

            static void SettingField(object sender, SettingFieldEventArgs e)
            {
                Assert.IsFalse(e.Handled);
                Assert.IsTrue(e.Field == null || e.Field.Name.Reverse().Convert<string>() == e.Field.Name);
                string name = e.Entry.Name.Reverse().Convert<string>();
                Reflector.SetField(e.Object, name, e.Value);
                e.Handled = true;
            }

            #endregion

            using var surrogate = new CustomSerializerSurrogateSelector();
            surrogate.Serializing += Serializing;
            surrogate.GettingField += GettingField;
            surrogate.Deserializing += Deserializing;
            surrogate.SettingField += SettingField;
            var bf = new BinaryFormatter();
            var bsf = new BinarySerializationFormatter(BinarySerializationOptions.TryUseSurrogateSelectorForAnyType | BinarySerializationOptions.IgnoreSerializationMethods);

            DoTest(bf, surrogate, obj, false, true, true);
            DoTest(bsf, surrogate, obj, true, true, true);
        }

        [Test]
        public void UpdatingSerializationInfoTest()
        {
            #region Local Methods

            static void Deserializing(object sender, DeserializingEventArgs e)
            {
                foreach (SerializationEntry entry in e.SerializationInfo)
                {
                    Assert.IsTrue(entry.Name.StartsWith("m_", StringComparison.Ordinal));
                    e.SerializationInfo.ReplaceValue(entry.Name, entry.Name.Substring(2), entry.Value, entry.ObjectType);
                }
            }

            #endregion

            var objOld = new ChangedClassOld { m_IntField = 42, m_StringField = "alpha" };
            var formatter = new BinarySerializationFormatter(BinarySerializationOptions.None);
            var rawDataOld = formatter.Serialize(objOld);

            using var surrogate = new CustomSerializerSurrogateSelector();
            surrogate.Deserializing += Deserializing;
            formatter.SurrogateSelector = surrogate;
            formatter.Binder = new CustomSerializationBinder
            {
                TypeResolver = (asmName, typeName) => typeName == typeof(ChangedClassOld).FullName ? typeof(ChangedClassNew) : null
            };
            var objNew = (ChangedClassNew)formatter.Deserialize(rawDataOld);

            Assert.AreEqual(objOld.m_IntField, objNew.IntField);
            Assert.AreEqual(objOld.m_StringField, objNew.StringField);
        }

        [Test]
        public void SafeModeTest()
        {
#if !NET9_0_OR_GREATER
            var bf = new BinaryFormatter();
#endif
            var bsf = new BinarySerializationFormatter(BinarySerializationOptions.None);
            using var surrogate = new CustomSerializerSurrogateSelector();
            var obj = new NonSerializableClass { IntProp = 42 };

            // in non-safe mode everything works
#if !NET9_0_OR_GREATER
            DoTest(bf, surrogate, obj, true, true, true);
#endif
            DoTest(bsf, surrogate, obj, true, true, true);

            surrogate.SafeMode = true; // so the surrogate denies support

            // in safe mode the surrogate denies serialization so it is passed back to the formatter
#if !NET9_0_OR_GREATER
            // BinaryFormatter: denies serialization as it is not serializable
            Throws<SerializationException>(() => DoTest(bf, surrogate, obj, true, true, true), "is not marked as serializable");
#endif
            // BinarySerializationFormatter: serialization is not supported with the provided options
            Throws<NotSupportedException>(() => DoTest(bsf, surrogate, obj, true, true, true), $"is not supported with following serialization options: None.{Environment.NewLine}You can try to enable the RecursiveSerializationAsFallback flag, though the serialized data will possibly not be able to be deserialized using the SafeMode flag.");

            // Enabling RecursiveSerializationAsFallback: the serialization works indeed, so the SafeMode setting on the surrogate selector didn't matter
            bsf.Options |= BinarySerializationOptions.RecursiveSerializationAsFallback;
            DoTest(bsf, surrogate, obj, true, true, true);

            // Enabling SafeMode on the formatter itself: not allowing any surrogate even if they would happily do the serialization
            bsf.Options |= BinarySerializationOptions.SafeMode;
            surrogate.SafeMode = false; // so the surrogate would support it again
            Throws<SerializationException>(() => DoTest(bsf, surrogate, obj, true, true, true), "In safe mode no serialization surrogate is allowed to be used.");
        }

        #endregion

        #endregion
    }
}
