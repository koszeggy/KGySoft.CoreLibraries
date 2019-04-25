using System;
using System.Reflection;
using System.Runtime.InteropServices;
using KGySoft.Reflection;
using NUnit.Framework;

namespace KGySoft.CoreLibraries
{
    [TestFixture]
    public class UnitTest1
    {
        private class TestClass
        {
            public int TestProperty { get; set; }
        }

        static TestClass instance = new TestClass();
        static PropertyInfo property = instance.GetType().GetProperty(nameof(TestClass.TestProperty));
        static PropertyAccessor accessor = PropertyAccessor.GetAccessor(property);
        static int i = 42;

        [Test]
        public void PropertyAccessorTest()
        {
            new PerformanceTest { Iterations = 1000000 }
                .AddCase(() => instance.TestProperty = 0, "Direct set")
                .AddCase(() => i = instance.TestProperty, "Direct get")
                .AddCase(() => accessor.Set(instance, i), "PropertyAccessor.Set")
                .AddCase(() => i = (int)accessor.Get(instance), "PropertyAccessor.Get")
                .AddCase(() => PropertyAccessor.GetAccessor(property).Set(instance, i), "PropertyAccessor.GetAccessor(property).Set")
                .AddCase(() => i = (int)PropertyAccessor.GetAccessor(property).Get(instance), "PropertyAccessor.GetAccessor(property).Get")
                .AddCase(() => property.SetValue(instance, i, null), "PropertyInfo.SetValue")
                .AddCase(() => i = (int)property.GetValue(instance, null), "PropertyInfo.GetValue")
                .DoTest()
                .DumpResults(Console.Out);
            //const int iterations = 1000000;
            //for (int i = 0; i < iterations; i++)
            //{
            //    int result;
            //    using (Profiler.Measure(GetCategory(i), "Direct set"))
            //        instance.TestProperty = i;
            //    using (Profiler.Measure(GetCategory(i), "Direct get"))
            //        result = instance.TestProperty;

            //    using (Profiler.Measure(GetCategory(i), "PropertyAccessor.Set"))
            //        accessor.Set(instance, i);
            //    using (Profiler.Measure(GetCategory(i), "PropertyAccessor.Get"))
            //        result = (int)accessor.Get(instance);

            //    using (Profiler.Measure(GetCategory(i), "PropertyInfo.SetValue"))
            //        property.SetValue(instance, i);
            //    using (Profiler.Measure(GetCategory(i), "PropertyInfo.GetValue"))
            //        result = (int)property.GetValue(instance);
            //}

            //string GetCategory(int i) => i < 1 ? "Warm-up" : "Test";
            //foreach (IMeasureItem item in Profiler.GetMeasurementResults())
            //{
            //    Console.WriteLine($@"[{item.Category}] {item.Operation}: {item.TotalTime.TotalMilliseconds} ms{(item.NumberOfCalls > 1
            //        ? $" (average: {item.TotalTime.TotalMilliseconds / item.NumberOfCalls} ms from {item.NumberOfCalls} calls)" : null)}");
            //}
        }

        [Test]
        public void SerializeStructTest()
        {
            // TODO 1: BinarySerializerben rengeteg ifen belül pattern matching lehet
            new PerformanceTest { Iterations = 10000000 }
                .AddCase(() =>
                {
                    object o = 1;
                    int i;
                    if (o is int)
                        i = (int)o;
                }, "No pattern matching")
                .AddCase(() =>
                {
                    object o = 1;
                    int i;
                    if (o is int intValue)
                        i = intValue;
                }, "Pattern matching")
                .DoTest()
                .DumpResults(Console.Out);

            // TODO 2: Struct serialization versions
            // fast and unsafe solution:
            //public static T ReadUsingMarshalUnsafe<T>(byte[] data) where T : struct
            //{
            //    unsafe
            //    {
            //        fixed (byte* p = &data[0])
            //        {
            //            return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
            //        }
            //    }
            //}

            //public unsafe static byte[] WriteUsingMarshalUnsafe<selectedT>(selectedT structure) where selectedT : struct
            //{
            //    byte[] byteArray = new byte[Marshal.SizeOf(structure)];
            //    fixed (byte* byteArrayPtr = byteArray)
            //    {
            //        Marshal.StructureToPtr(structure, (IntPtr)byteArrayPtr, true);
            //    }
            //    return byteArray;
            //}

            // Továbbá: a marshal nélküli, TypedReference-es verzió (asszem, volt egy olyan is) - megnézni, ugyanaz lesz-e a tartalom (a marshal attribute-okra mondjuk biztos nem)
        }

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
                .DoTest()
                .DumpResults(Console.Out);
        }


        //public struct TestStruct
        //{
        //    public TestStruct(int i) { }
        //}

        //public class TestClass
        //{
        //    public TestClass() { }
        //    public TestClass(int i) { }
        //}

        //[Test]
        //public void CreateInstanceTest()
        //{
        //    Type structType = typeof(TestStruct);
        //    var ctorStructParams = structType.GetConstructor(new[] { typeof(int) });
        //    var accessorByType = CreateInstanceAccessor.GetAccessor(structType);
        //    var accessorByCtor = CreateInstanceAccessor.GetAccessor(ctorStructParams);

        //    new PerformanceTest { TestName = "Create Value Type", Iterations = 100000 }
        //        .AddCase(() => new TestStruct(), "Direct creation, no ctor")
        //        .AddCase(() => new TestStruct(1), "Direct creation, by ctor")
        //        .AddCase(() => Activator.CreateInstance(structType), "Activator.CreateInstance(Type)")
        //        .AddCase(() => Activator.CreateInstance(structType, 1), "Activator.CreateInstance(Type, params)")
        //        .AddCase(() => ctorStructParams.Invoke(new object[] { 1 }), "ConstructorInfo.Invoke(params)")
        //        .AddCase(() => accessorByType.CreateInstance(), "CreateInstanceAccessor(Type).CreateInstance()")
        //        .AddCase(() => accessorByCtor.CreateInstance(1), "CreateInstanceAccessor(ConstructorInfo).CreateInstance(params)")
        //        .AddCase(() => CreateInstanceAccessor.GetAccessor(structType).CreateInstance(), "CreateInstanceAccessor.GetAccessor(Type).CreateInstance()")
        //        .AddCase(() => CreateInstanceAccessor.GetAccessor(ctorStructParams).CreateInstance(1), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance(params)")
        //        .DoTest();

        //    Type classType = typeof(TestClass);
        //    var ctorClassDefault = classType.GetConstructor(Type.EmptyTypes);
        //    var ctorClassParams = classType.GetConstructor(new[] { typeof(int) });
        //    accessorByType = CreateInstanceAccessor.GetAccessor(classType);
        //    var accessorByDefaultCtor = CreateInstanceAccessor.GetAccessor(ctorClassDefault);
        //    accessorByCtor = CreateInstanceAccessor.GetAccessor(ctorClassParams);

        //    new PerformanceTest { TestName = "Create Reference Type", Iterations = 100000 }
        //        .AddCase(() => new TestClass(), "Direct creation, no ctor")
        //        .AddCase(() => new TestClass(1), "Direct creation, by ctor")
        //        .AddCase(() => Activator.CreateInstance(classType), "Activator.CreateInstance(Type)")
        //        .AddCase(() => Activator.CreateInstance(classType, 1), "Activator.CreateInstance(Type, params)")
        //        .AddCase(() => ctorClassDefault.Invoke(null), "ConstructorInfo.Invoke(null)")
        //        .AddCase(() => ctorClassParams.Invoke(new object[] { 1 }), "ConstructorInfo.Invoke(params)")
        //        .AddCase(() => accessorByType.CreateInstance(), "CreateInstanceAccessor(Type).CreateInstance()")
        //        .AddCase(() => accessorByDefaultCtor.CreateInstance(), "CreateInstanceAccessor(ConstructorInfo).CreateInstance()")
        //        .AddCase(() => accessorByCtor.CreateInstance(1), "CreateInstanceAccessor(ConstructorInfo).CreateInstance(params)")
        //        .AddCase(() => CreateInstanceAccessor.GetAccessor(classType).CreateInstance(), "CreateInstanceAccessor.GetAccessor(Type).CreateInstance()")
        //        .AddCase(() => CreateInstanceAccessor.GetAccessor(ctorClassDefault).CreateInstance(), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance()")
        //        .AddCase(() => CreateInstanceAccessor.GetAccessor(ctorClassParams).CreateInstance(1), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance(params)")
        //        .DoTest();

        //}
    }
}
