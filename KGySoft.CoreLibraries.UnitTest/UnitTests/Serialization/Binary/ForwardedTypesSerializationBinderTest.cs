#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ForwardedTypesSerializationBinderTest.cs
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

using System.Reflection;
using System.Runtime.Serialization;

using KGySoft.Serialization.Binary;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Binary
{
    [TestFixture]
    public class ForwardedTypesSerializationBinderTest : TestBase
    {
        #region Enumerations

        private enum TestEnum
        {
            Value = 1
        }

        #endregion

        #region Methods

        [Test]
        public void DeserializeTypeFromUnknownAssembly()
        {
            object testObject = TestEnum.Value;
            var binder = new ForwardedTypesSerializationBinder { WriteLegacyIdentity = true };
            binder.AddType(typeof(TestEnum), new AssemblyName("SomeUnknownAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=b45eba277439ddfe"));

            byte[] oldAssemblyData = new BinarySerializationFormatter { Binder = binder }.Serialize(testObject);

            // without a binder
            Throws<SerializationException>(() => new BinarySerializationFormatter().Deserialize(oldAssemblyData));

            // with a binder from the very specific assembly
            Assert.AreEqual(testObject, new BinarySerializationFormatter { Binder = binder }.Deserialize(oldAssemblyData));

            // the binder does not have the correct assembly
            binder = new ForwardedTypesSerializationBinder();
            binder.AddType(typeof(TestEnum), new AssemblyName("SomeIrrelevantAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=b45eba277439ddfe"));
            Throws<SerializationException>(() => new BinarySerializationFormatter { Binder = binder }.Deserialize(oldAssemblyData));

            // the binder does not have the correct version
            binder.AddType(typeof(TestEnum), new AssemblyName("SomeUnknownAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b45eba277439ddfe"));
            Throws<SerializationException>(() => new BinarySerializationFormatter { Binder = binder }.Deserialize(oldAssemblyData));

            // from any assembly
            binder.AddType(typeof(TestEnum)); // allow resolving from any assemblies
            Assert.AreEqual(testObject, new BinarySerializationFormatter { Binder = binder }.Deserialize(oldAssemblyData));

            // only from specified assembly but allowing any version
            binder = new ForwardedTypesSerializationBinder();
            binder.AddType(typeof(TestEnum), new AssemblyName("SomeUnknownAssembly"));
            Assert.AreEqual(testObject, new BinarySerializationFormatter { Binder = binder }.Deserialize(oldAssemblyData));
        }

        #endregion
    }
}
