using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KGySoft.Collections;
using KGySoft.Diagnostics;
using KGySoft.Libraries;
using KGySoft.Reflection;
using KGySoft.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest
{
    [TestClass]
    public class UnitTest1 : TestBase
    {
        [TestMethod]
        public void TestMethod1()
        {
            //CheckTestingFramework();
            //new TestOperation
            //{
            //    RefOpName = "Cast with boxing",
            //    ReferenceOperation = () => CastNormal<int>(1),
            //    TestOpName = "Cast with typed ref",
            //    TestOperation = () => CastTyperef<int>(1),
            //    Iterations = 10000000,
            //    Repeat = 5
            //}.DoTest();

            //new TestOperation
            //{
            //    TestOpName = "Dynamic on object",
            //    TestOperation = DynamicOnObject,
            //    Iterations = 10000,
            //    Repeat = 5
            //}.DoTest();

            //new TestOperation
            //{
            //    TestOpName = "ExpandoObject",
            //    TestOperation = ExpandoObject,
            //    Iterations = 10000,
            //    Repeat = 5
            //}.DoTest();

            //new TestOperation
            //{
            //    TestOpName = "DynamicObject",
            //    TestOperation = DynamicObject,
            //    Iterations = 10000,
            //    Repeat = 5
            //}.DoTest();

            //new TestOperation
            //{
            //    TestOpName = "Reflector",
            //    TestOperation = ByReflector,
            //    Iterations = 10000,
            //    Repeat = 5
            //}.DoTest();

            //new TestOperation
            //{
            //    TestOpName = "Accessor",
            //    TestOperation = ByAccessor,
            //    Iterations = 10000,
            //    Repeat = 5
            //}.DoTest();


            ConstructorInfo ctor = typeof(MyClass).GetConstructor(Type.EmptyTypes);
            var obj = new MyClass();

            new TestOperation
            {
                TestOpName = "MethodBase.Invoke",
                TestOperation = () => ctor.Invoke(obj, new object[0]),
                Iterations = 1,
                Repeat = 5,
                WarmUpTime = 0
            }.DoTest();

            new TestOperation
            {
                TestOpName = "Reflector.InvokeCtor",
                TestOperation = () => Reflector.InvokeCtor(obj, ctor),
                Iterations = 1,
                Repeat = 5,
                WarmUpTime = 0
            }.DoTest();

            var invoker = new ActionInvoker(ctor);
            new TestOperation
            {
                TestOpName = "ActionInvoker(ctor)",
                TestOperation = () => invoker.Invoke(obj),
                Iterations = 1,
                Repeat = 5,
                WarmUpTime = 0
            }.DoTest();

            Console.WriteLine(obj.IntProp);
        }

        public class MyClass
        {
            public int IntProp { get; set; }

            public MyClass()
            {
                IntProp++;
            }
        }

        //private void ByReflector()
        //{
        //    Reflector.SetInstancePropertyByName(new TestClass(), nameof(TestClass.Prop), 1);
        //}

        //private static PropertyInfo prop;
        //private void ByAccessor()
        //{
        //    PropertyAccessor.GetPropertyAccessor(prop ?? (prop = typeof(TestClass).GetProperty(nameof(TestClass.Prop)))).Set(new TestClass(), 1);
        //}

        //private void DynamicObject()
        //{
        //    ((dynamic)new TestDynamic()).Prop = 1;
        //}

        //private void ExpandoObject()
        //{
        //    ((dynamic)new ExpandoObject()).Prop = 1;
        //}

        //private void DynamicOnObject()
        //{
        //    ((dynamic)new TestClass()).Prop = 1;
        //}

        //private class TestClass
        //{
        //    public int Prop { get; set; }
        //}

        //private class TestDynamic : DynamicObject
        //{
        //    private Dictionary<string, object> properties = new Dictionary<string, object>();

        //    public override bool TrySetMember(SetMemberBinder binder, object value)
        //    {
        //        properties[binder.Name] = value;
        //        return true;
        //    }
        //}

        //private static T CastNormal<T>(int i)
        //{
        //    return (T)(object)i;
        //}

        //private static T CastTyperef<T>(int i)
        //{
        //    return __refvalue(__makeref(i), T);
        //}
    }
}
