using KGySoft.Serialization.Binary;

using NUnit.Framework;

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Binary
{
    [TestFixture]
    public class CtorInvocationTests
    {
        [Test]
        public void InvokeCtor()
        {
            TestClass.CtorInvocationCounter = 0;

            var obj = new TestClass();

            var data = KGySoft.Serialization.Binary.BinarySerializer
                .Serialize(obj, BinarySerializationOptions.RecursiveSerializationAsFallback);

            var result = KGySoft.Serialization.Binary.BinarySerializer.Deserialize<TestClass>(data, 0,
                BinarySerializationOptions.AlwaysTryInvokeCtorWhenDeserializing);

            Assert.AreEqual(2, TestClass.CtorInvocationCounter);
        }

        [Test]
        public void DoNotInvokeIBinarySerializableCtorWithoutInterface()
        {
            TestClassWithIBinarySerializableCtor.CtorInvocationCounter = 0;

            var obj = new TestClassWithIBinarySerializableCtor(BinarySerializationOptions.None, new byte[0]);

            var data = KGySoft.Serialization.Binary.BinarySerializer
                .Serialize(obj, BinarySerializationOptions.RecursiveSerializationAsFallback);

            var result = KGySoft.Serialization.Binary.BinarySerializer.Deserialize<TestClassWithIBinarySerializableCtor>(data, 0,
                BinarySerializationOptions.AlwaysTryInvokeCtorWhenDeserializing);

            Assert.AreEqual(1, TestClassWithIBinarySerializableCtor.CtorInvocationCounter);
        }

        [Test]
        public void DoNotInvokeCtor()
        {
            TestClass.CtorInvocationCounter = 0;

            var obj = new TestClass();

            var data = KGySoft.Serialization.Binary.BinarySerializer
                .Serialize(obj, BinarySerializationOptions.RecursiveSerializationAsFallback);

            var result = KGySoft.Serialization.Binary.BinarySerializer.Deserialize<TestClass>(data, 0,
                BinarySerializationOptions.None);

            Assert.AreEqual(1, TestClass.CtorInvocationCounter);
        }

        [Test]
        public void DoNotCrashWhenClassDoesNotHaveParameterlessCtor()
        {
            TestCalssWithBadCtor.CtorInvocationCounter = 0;

            var obj = new TestCalssWithBadCtor("", 0);

            var data = KGySoft.Serialization.Binary.BinarySerializer
                .Serialize(obj, BinarySerializationOptions.RecursiveSerializationAsFallback);

            var result = KGySoft.Serialization.Binary.BinarySerializer.Deserialize<TestCalssWithBadCtor>(data, 0,
                BinarySerializationOptions.AlwaysTryInvokeCtorWhenDeserializing);

            Assert.AreEqual(1, TestCalssWithBadCtor.CtorInvocationCounter);
        }

        class TestClass
        {
            public static int CtorInvocationCounter = 0;

            public TestClass()
            {
                CtorInvocationCounter++;
            }
        }

        class TestCalssWithBadCtor
        {
            public static int CtorInvocationCounter = 0;

            public TestCalssWithBadCtor(string foo, int bar)
            {
                CtorInvocationCounter++;
            }
        }

        // NOT an IBinarySerializable, but with matching ctor
        class TestClassWithIBinarySerializableCtor
        {
            public static int CtorInvocationCounter = 0;

            public TestClassWithIBinarySerializableCtor(BinarySerializationOptions options, byte[] serData)
            {
                CtorInvocationCounter++;
            }
        }
    }

}
