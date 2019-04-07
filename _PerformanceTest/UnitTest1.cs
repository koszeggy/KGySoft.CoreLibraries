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
        private static readonly MethodInfo methodAdd = typeof(CollectionExtensions).GetMethod(nameof(CollectionExtensions.AddRange));

        private static readonly IDictionary<Type, MethodInfo> methodCache = new LockingDictionary<Type, MethodInfo>();

        [Test]
        public unsafe void TestMethod1()
        {
            //var parameter = Expression.Parameter(typeof(long), "value");
            //var dynamicMethod = Expression.Lambda<Func<long, ConsoleColor>>(
            //    Expression.Convert(parameter, typeof(ConsoleColor)),
            //    parameter);
            //Func<long, ConsoleColor> converter = dynamicMethod.Compile();

            //new PerformanceTest<ConsoleColor> { WarmUpTime = 0, Iterations = 10000, Repeat = 3 }
            //    .AddCase(() => (ConsoleColor)Enum.ToObject(typeof(ConsoleColor), 0L), "Enum.ToObject")
            //    .AddCase(() => (ConsoleColor)(object)(int)0L, "(TEnum)(object)(underlyingPrimitive)longValue")
            //    .AddCase(() => converter.Invoke(0L), "ConverterExpression")
            //    .DoTest();

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
