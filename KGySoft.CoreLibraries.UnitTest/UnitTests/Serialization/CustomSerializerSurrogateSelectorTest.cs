#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CustomSerializerSurrogateSelectorTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using KGySoft.Reflection;
using KGySoft.Serialization;

using NUnit.Framework;
using NUnit.Framework.Internal;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization
{
    [TestFixture]
    public class CustomSerializerSurrogateSelectorTest : TestBase
    {
        #region Nested classes

        #region ConflictNameBase class

        [Serializable]
        private class ConflictNameBase
        {
            #region Fields

            public string ConflictingField;

            #endregion

            #region Methods

            public ConflictNameBase SetBase(string value)
            {
                ConflictingField = value;
                return this;
            }

            #endregion
        }

        #endregion

        #region ConflictNameChild class

        [Serializable]
        private class ConflictNameChild : ConflictNameBase
        {
            #region Fields

            public new int ConflictingField;

            #endregion
        }

        #endregion

        #region SerializationEventsClass class

        [Serializable]
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        private class SerializationEventsClass : IDeserializationCallback
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
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

            internal int IntField;
            internal string StringField;

            #endregion
        }

        #endregion

        #region OldToNewBinder class

        private class OldToNewBinder : SerializationBinder
        {
            #region Methods

            public override Type BindToType(string assemblyName, string typeName)
                => typeName == typeof(ChangedClassOld).FullName ? typeof(ChangedClassNew) : null;

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private static object[] testCases =
        {
            // primitive types
            1,
            "alpha",

            // normally serialized
            new List<int> { 1 },

            // normal serializable class with serialization events and NonSerialized fields
            new SerializationEventsClass{ Name = "Parent" }.AddChild("Child").Parent, 

            // custom serializable class
            new DataTable("tableName", "tableNamespace")
        };

        #endregion

        #region Methods

        #region Static Methods

        private static void DoTest(IFormatter formatter, ISurrogateSelector surrogate, object obj, bool throwError, bool serialize, bool deserialize)
        {
            Console.Write($"{obj} by {formatter.GetType().Name}: ");
            formatter.SurrogateSelector = serialize ? surrogate : null;
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

                    formatter.SurrogateSelector = deserialize ? surrogate : null;
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
        public void DeserializeDefaultSerializedObjects(object obj)
        {
            ISurrogateSelector surrogate = new CustomSerializerSurrogateSelector();

            DoTest(new BinaryFormatter(), surrogate, obj, false, false, true);
            DoTest(new BinarySerializationFormatter(), surrogate, obj, true, false, true);
            DoTest(new BinarySerializationFormatter(BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes), surrogate, obj, true, false, true);
        }

        [TestCaseSource(nameof(testCases))]
        public void SerializeBySurrogateDeserializeWithoutSurrogate(object obj)
        {
            ISurrogateSelector surrogate = new CustomSerializerSurrogateSelector();

            DoTest(new BinarySerializationFormatter(), surrogate, obj, true, true, false);


            //DoTest(new BinaryFormatter(), surrogate, obj, false, true, false);
            //DoTest(new BinarySerializationFormatter(), surrogate, obj, true, true, false);
            //DoTest(new BinarySerializationFormatter(BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes), surrogate, obj, true, false, true);
        }

        [Test]
        public void SerializeClassWithConflictingFields()
        {
            object obj = new ConflictNameChild { ConflictingField = 13 }.SetBase("base");
            ISurrogateSelector surrogate = new CustomSerializerSurrogateSelector();
            var bf = new BinaryFormatter();
            var bsf = new BinarySerializationFormatter();

            // not using surrogate: tests if the formatter can handle the situation internally
            //DoTest(bf, null, obj, false, false, false); // Deserialization failed: System.ArgumentException: Object of type 'System.String' cannot be converted to type 'System.Int32'.
            DoTest(bsf, null, obj, true, false, false);

            // using surrogate for both ways
            DoTest(bf, surrogate, obj, false, true, true);
            DoTest(bsf, surrogate, obj, true, true, true);

            // default serialization by surrogate: the formatter must add unique names to the serialization info
            // and the surrogate must resolve these names somehow (can be solved by events)
            //DoTest(bf, surrogate, obj, true, false, true); // SerializationException : Cannot add the same member twice to a SerializationInfo object.
            DoTest(bsf, surrogate, obj, true, false, true);

            // surrogate serialization by default: the surrogate must add unique names, which should be
            // resolved by the formatter somehow (not really possible without hard coded handling in the formatter)
            //DoTest(bf, surrogate, obj, false, true, false); // 'base' compared to '<null>'
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
            object obj = new DataTable("tableName", "namespaceName");
            var surrogate = new CustomSerializerSurrogateSelector();
            var formatter = new BinarySerializationFormatter { SurrogateSelector = surrogate };

            DoTest(formatter, surrogate, obj, true, true, true);
            surrogate.IgnoreISerializable = true;
            formatter.Options = BinarySerializationOptions.IgnoreSerializationMethods; // TextInfo.OnDeserialization in .NET Core
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
                Assert.IsNull(e.Field); // due to the reversed names
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
            var formatter = new BinarySerializationFormatter();
            var rawDataOld = formatter.Serialize(objOld);

            using var surrogate = new CustomSerializerSurrogateSelector();
            surrogate.Deserializing += Deserializing;
            formatter.SurrogateSelector = surrogate;
            formatter.Binder = new OldToNewBinder();
            var objNew = (ChangedClassNew)formatter.Deserialize(rawDataOld);

            Assert.AreEqual(objOld.m_IntField, objNew.IntField);
            Assert.AreEqual(objOld.m_StringField, objNew.StringField);
        }

        #endregion

        #endregion
    }
}
