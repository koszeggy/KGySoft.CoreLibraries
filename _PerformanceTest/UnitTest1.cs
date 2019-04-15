using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using KGySoft;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using NUnit.Framework;

namespace _PerformanceTest
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public unsafe void SetBoxedIntTest()
        {
            object o = 1;
            FieldInfo intmValue = typeof(int).GetType().GetField("m_value", BindingFlags.Instance | BindingFlags.NonPublic);
            var accessor = FieldAccessor.GetAccessor(intmValue);

            new PerformanceTest { Repeat = 1, Iterations = 10000 }
                .AddCase(() => intmValue.SetValue(o, 2), "FieldInfo.SetValue")
                .AddCase(() => intmValue.SetValueDirect(__makeref(o), 2), "FieldInfo.SetValueDirect")
                .AddCase(() => accessor.Set(o, 2), "FieldAccessor.Set")
                .AddCase(() =>
                {
                    var objectPinned = GCHandle.Alloc(o, GCHandleType.Pinned);
                    try
                    {
                        TypedReference objRef = __makeref(o);
                        int* rawContent = (int*)*(IntPtr*)*(IntPtr*)&objRef;
                        int* boxedInt = rawContent + IntPtr.Size;
                        (*boxedInt)++;
                    }
                    finally
                    {
                        objectPinned.Free();
                    }
                }, "TypedReference with pinning")
                .AddCase(() =>
                {
                    TypedReference objRef = __makeref(o);
                    int* rawBoxedInstance = (int*)*(IntPtr*)*(IntPtr*)&objRef;
                    if (IntPtr.Size == 4)
                        rawBoxedInstance[1]++;
                    else
                        rawBoxedInstance[2]++;
                }, "TypedReference no pinning")
                .DoTest();
        }


        public struct TestStruct
        {
            public TestStruct(int i) { }
        }

        public class TestClass
        {
            public TestClass() { }
            public TestClass(int i) { }
        }

        [Test]
        public void CreateInstanceTest()
        {
            Type structType = typeof(TestStruct);
            var ctorStructParams = structType.GetConstructor(new[] { typeof(int) });
            var accessorByType = CreateInstanceAccessor.GetAccessor(structType);
            var accessorByCtor = CreateInstanceAccessor.GetAccessor(ctorStructParams);

            new PerformanceTest { TestName = "Create Value Type", Iterations = 100000 }
                .AddCase(() => new TestStruct(), "Direct creation, no ctor")
                .AddCase(() => new TestStruct(1), "Direct creation, by ctor")
                .AddCase(() => Activator.CreateInstance(structType), "Activator.CreateInstance(Type)")
                .AddCase(() => Activator.CreateInstance(structType, 1), "Activator.CreateInstance(Type, params)")
                .AddCase(() => ctorStructParams.Invoke(new object[] { 1 }), "ConstructorInfo.Invoke(params)")
                .AddCase(() => accessorByType.CreateInstance(), "CreateInstanceAccessor(Type).CreateInstance()")
                .AddCase(() => accessorByCtor.CreateInstance(1), "CreateInstanceAccessor(ConstructorInfo).CreateInstance(params)")
                .AddCase(() => CreateInstanceAccessor.GetAccessor(structType).CreateInstance(), "CreateInstanceAccessor.GetAccessor(Type).CreateInstance()")
                .AddCase(() => CreateInstanceAccessor.GetAccessor(ctorStructParams).CreateInstance(1), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance(params)")
                .DoTest();

            Type classType = typeof(TestClass);
            var ctorClassDefault = classType.GetConstructor(Type.EmptyTypes);
            var ctorClassParams = classType.GetConstructor(new[] { typeof(int) });
            accessorByType = CreateInstanceAccessor.GetAccessor(classType);
            var accessorByDefaultCtor = CreateInstanceAccessor.GetAccessor(ctorClassDefault);
            accessorByCtor = CreateInstanceAccessor.GetAccessor(ctorClassParams);

            new PerformanceTest { TestName = "Create Reference Type", Iterations = 100000 }
                .AddCase(() => new TestClass(), "Direct creation, no ctor")
                .AddCase(() => new TestClass(1), "Direct creation, by ctor")
                .AddCase(() => Activator.CreateInstance(classType), "Activator.CreateInstance(Type)")
                .AddCase(() => Activator.CreateInstance(classType, 1), "Activator.CreateInstance(Type, params)")
                .AddCase(() => ctorClassDefault.Invoke(null), "ConstructorInfo.Invoke(null)")
                .AddCase(() => ctorClassParams.Invoke(new object[] { 1 }), "ConstructorInfo.Invoke(params)")
                .AddCase(() => accessorByType.CreateInstance(), "CreateInstanceAccessor(Type).CreateInstance()")
                .AddCase(() => accessorByDefaultCtor.CreateInstance(), "CreateInstanceAccessor(ConstructorInfo).CreateInstance()")
                .AddCase(() => accessorByCtor.CreateInstance(1), "CreateInstanceAccessor(ConstructorInfo).CreateInstance(params)")
                .AddCase(() => CreateInstanceAccessor.GetAccessor(classType).CreateInstance(), "CreateInstanceAccessor.GetAccessor(Type).CreateInstance()")
                .AddCase(() => CreateInstanceAccessor.GetAccessor(ctorClassDefault).CreateInstance(), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance()")
                .AddCase(() => CreateInstanceAccessor.GetAccessor(ctorClassParams).CreateInstance(1), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance(params)")
                .DoTest();

        }
    }
}
