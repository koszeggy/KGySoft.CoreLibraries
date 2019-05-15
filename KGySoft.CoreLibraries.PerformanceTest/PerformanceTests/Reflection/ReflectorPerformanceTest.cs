using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using KGySoft.Collections;
using KGySoft.Reflection;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.PerformanceTests.Reflection
{
    [TestFixture]
    public class ReflectorPerformanceTest
    {
        #region Nested Types

        class IntList : List<int>, ICollection<int>
        {
            public bool IsReadOnly { get { return false; } }

            #region ICollection<int> Members

            bool ICollection<int>.IsReadOnly
            {
                get { return (this as ICollection<int>).IsReadOnly; }
            }

            #endregion
        }

        class TestBase
        {
            private int privateField;

            private int PrivateProperty { get; set; }

            private int PrivateMethod(int p1, int p2)
            {
                privateField = p1;
                PrivateProperty = p2;
                return p1 + p2;
            }

            private static int PrivateStaticMethod(int p1, int p2)
            {
                return p1 + p2;
            }

            private int this[string index]
            {
                get { return privateField; }
                set { privateField = 0; }
            }
        }

        class Test : TestBase
        {
            internal int field;

            public int Value { get; set; }

            public IntList IntList { get; set; }

            public static int StaticProperty { get; set; }

            public static string StaticField;

            [IndexerName("Indexer")]
            public int this[int index]
            {
                get { return 0; }
                set { field = value * index; }
            }

            [Browsable(false)]
            public void SetValue(int p1, int p2)
            {
                Value = p1 + p2;
            }

            public static void StaticMethod(int p1, int p2)
            {
                StaticProperty = p1;
            }

            public Test()
            {
            }

            public Test(int field, int value)
            {
                this.field = field;
                this.Value = value;
                IntList = new IntList { field, value };
            }
        }

        #endregion

        static Cache<MemberInfo, MemberAccessor> GetCache()
        {
            return (Cache<MemberInfo, MemberAccessor>)Reflector.GetField(Reflector.GetField(null, typeof(MemberAccessor).GetField("accessorCache", BindingFlags.NonPublic | BindingFlags.Static), ReflectionWays.SystemReflection), "cache");
        }

        private static void ResetCache(int size)
        {
            var cache = GetCache();
            cache.Capacity = size;
            cache.Reset();
        }

        private static void DumpStatistics()
        {
            var cache = GetCache();
            Console.WriteLine(cache.GetStatistics());
        }

        private static void ResetCache()
        {
            ResetCache(1024);
        }

        private static int GetCacheSize()
        {
            var cache = GetCache();
            return cache.Count;
        }

        [Test]
        public void TestMethodInvoke()
        {
            // Obtaining information            
            const int p1 = 1;
            const int p2 = 10;
            Test t = new Test();
            const string methodName = "SetValue";

            // a new uncached invoker
            MethodInfo mi = t.GetType().GetMethod(methodName);
            MethodAccessor invokerAction = new ActionMethodAccessor(mi);

            int cacheSize = 1024;
            int iterations = 1000000;

            ResetCache(cacheSize);
            Console.WriteLine("Number of iterations: {0:N0}", iterations);
            // Direct execution
            Console.WriteLine("==================Method executions: (int,int): void==================");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t.SetValue(p1, p2);
                //Test.StaticMethod(p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Direct invocation (instance.(...)): " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                invokerAction.Invoke(t, new object[] { p1, p2 });
            }
            watch.Stop();
            Console.WriteLine("Prefetched invoker (MethodInvoker.Invoke(instance, ...)): " + watch.ElapsedMilliseconds.ToString());

            // Method invoker
            ResetCache(cacheSize);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                MethodAccessor.GetAccessor(mi).Invoke(t, new object[] { p1, p2 });
            }
            watch.Stop();
            Console.WriteLine("Re-fetched invoker (MethodInvoker.GetMethodInvoker(MethodInfo).Invoke(intance, ...)): " + watch.ElapsedMilliseconds.ToString());

            // MethodInfo.Invoke
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi.Invoke(t, new object[] { p1, p2 });
            }
            watch.Stop();
            Console.WriteLine("MethodInfo.Invoke: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by method info
            ResetCache(cacheSize);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.InvokeMethod(t, mi, ReflectionWays.DynamicDelegate, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.InvokeMethod(instance, MethodInfo, ReflectionWays.DynamicDelegate, ...): " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by method info
            ResetCache(cacheSize);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.InvokeMethod(t, mi, ReflectionWays.SystemReflection, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.InvokeMethod(instance, MethodInfo, ReflectionWays.SystemReflection, ...): " + watch.ElapsedMilliseconds.ToString());

            // Lambda by name
            ResetCache(cacheSize);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.InvokeMethod(t, methodName, ReflectionWays.DynamicDelegate, p1, p2);
                //Reflector.RunStaticMethodByName(typeof(Test), methodName, ReflectionWays.DynamicDelegate, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.InvokeMethod(instance, \"MethodName\", ReflectionWays.DynamicDelegate, ...): " + watch.ElapsedMilliseconds.ToString());

            // Reflection by name
            ResetCache(cacheSize);
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.InvokeMethod(t, methodName, ReflectionWays.SystemReflection, p1, p2);
                //Reflector.RunStaticMethodByName(typeof(Test), methodName, ReflectionWays.SystemReflection, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.InvokeMethod(instance, \"MethodName\", ReflectionWays.SystemReflection, ...): " + watch.ElapsedMilliseconds.ToString());

            return;
            //Console.WriteLine("****************************************************");

            //ResetCache(cacheSize);
            //Console.WriteLine(">>>>>> Cache Size: " + cacheSize);
            //MethodInvoker.GetMethodInvoker(mi); // cache generic accessor
            //Console.WriteLine("   Pre-Cached accessor, cache is on");
            //ReFetchedAccessors(t, p1, p2, mi, iterations, watch, null);

            //ResetCache(cacheSize);
            //foreach (var item in typeof(Console).GetMethods())
            //    MethodInvoker.GetMethodInvoker(item);
            //Console.WriteLine("   " + GetCacheSize() + " items in cache, Behavior = " + MemberAccessor.CacheBehavior);
            //ReFetchedAccessors(t, p1, p2, mi, iterations, watch, null);

            //ResetCache(cacheSize);
            //foreach (var item in typeof(Console).GetMethods())
            //    MethodInvoker.GetMethodInvoker(item);
            //MemberAccessor.CacheBehavior = CacheBehavior.RemoveLeastRecentUsedElement;
            //Console.WriteLine("   " + GetCacheSize() + " items in cache, Behavior = " + MemberAccessor.CacheBehavior);
            //ReFetchedAccessors(t, p1, p2, mi, iterations, watch, null);

            //MethodInfo[] methods = typeof(Math).GetMethods();
            //ResetCache(cacheSize);
            //MemberAccessor.CacheBehavior = CacheBehavior.RemoveOldestElement;
            //Console.WriteLine("   Semi-random invokes, Behavior = " + MemberAccessor.CacheBehavior);
            //ReFetchedAccessors(t, p1, p2, mi, iterations, watch, methods);

            //ResetCache(cacheSize);
            //MemberAccessor.CacheBehavior = CacheBehavior.RemoveLeastRecentUsedElement;
            //Console.WriteLine("   Semi-random invokes, Behavior = " + MemberAccessor.CacheBehavior);
            //ReFetchedAccessors(t, p1, p2, mi, iterations, watch, methods);

            //cacheSize = 32;
            //ResetCache(cacheSize);
            //Console.WriteLine(">>>>>> Cache Size: " + cacheSize);

            //MemberAccessor.CacheBehavior = CacheBehavior.RemoveOldestElement;
            //Console.WriteLine("   Semi-random invokes, Behavior = " + MemberAccessor.CacheBehavior);
            //ReFetchedAccessors(t, p1, p2, mi, iterations, watch, methods);
            //DumpStatistics();

            //ResetCache(cacheSize);
            //MemberAccessor.CacheBehavior = CacheBehavior.RemoveLeastRecentUsedElement;
            //Console.WriteLine("   Semi-random invokes, Behavior = " + MemberAccessor.CacheBehavior);
            //ReFetchedAccessors(t, p1, p2, mi, iterations, watch, methods);
            //DumpStatistics();

            //Console.WriteLine("****************************************************");
            //iterations = 10000;
            //Console.WriteLine("Number of iterations: {0:N0}", iterations);

            //// MethodInfo.Invoke
            //watch.Reset();
            //watch.Start();
            //for (int i = 0; i < iterations; i++)
            //{
            //    mi.Invoke(t, new object[] { p1, p2 });
            //}
            //watch.Stop();
            //Console.WriteLine("MethodInfo.Invoke: " + watch.ElapsedMilliseconds.ToString());

            //// MethodInfo.Invoke from name
            //watch.Reset();
            //watch.Start();
            //for (int i = 0; i < iterations; i++)
            //{
            //    t.GetType().GetMethod(methodName).Invoke(t, new object[] { p1, p2 });
            //}
            //watch.Stop();
            //Console.WriteLine("MethodInfo.Invoke from name: " + watch.ElapsedMilliseconds.ToString());

            //// Dynamic accessor from name, cache enabled
            //MemberAccessor.CacheBehavior = CacheBehavior.RemoveOldestElement;
            //ResetCache();
            //watch.Reset();
            //watch.Start();
            //for (int i = 0; i < iterations; i++)
            //{
            //    MethodInvoker.GetMethodInvoker(t.GetType().GetMethod(methodName)).Invoke(t, new object[] { p1, p2 });
            //}
            //watch.Stop();
            //Console.WriteLine("Dynamic accessor from name, cache enabled: " + watch.ElapsedMilliseconds.ToString());

            //// Dynamic accessor from name, cache disabled
            //MemberAccessor.CachingEnabled = false;
            //watch.Reset();
            //watch.Start();
            //for (int i = 0; i < iterations; i++)
            //{
            //    MethodInvoker.GetMethodInvoker(t.GetType().GetMethod(methodName)).Invoke(t, new object[] { p1, p2 });
            //}
            //watch.Stop();
            //Console.WriteLine("Dynamic accessor from name, cache disabled: " + watch.ElapsedMilliseconds.ToString());
            /*Circular list cache:
Number of iterations: 1,000,000
==================Method executions: (int,int): void==================
Direct invocation (instance.(...)): 0
Prefetched invoker (MethodInvoker.Invoke(instance, ...)): 90
Re-fetched invoker (MethodInvoker.GetMethodInvoker(MethodInfo).Invoke(intance, ...)): 189
MethodInfo.Invoke: 2867
Reflector.InvokeMethod(instance, MethodInfo, ReflectionWays.DynamicDelegate, ...): 143
Reflector.InvokeMethod(instance, MethodInfo, ReflectionWays.SystemReflection, ...): 3226
Reflector.InvokeMethod(instance, "MethodName", ReflectionWays.DynamicDelegate, ...): 888
Reflector.InvokeMethod(instance, "MethodName", ReflectionWays.SystemReflection, ...): 4591
****************************************************
>>>>>> Cache Size: 1024
   Pre-Cached accessor, cache is on
Re-fetched accessors from 1 elements: 129 
   106 items in cache, Behavior = RemoveOldestElement
Re-fetched accessors from 1 elements: 133 
   106 items in cache, Behavior = RemoveLeastRecentUsedElement
Re-fetched accessors from 1 elements: 156 
   Semi-random invokes, Behavior = RemoveOldestElement
Re-fetched accessors from 15 elements: 333 
   Semi-random invokes, Behavior = RemoveLeastRecentUsedElement
Re-fetched accessors from 15 elements: 579 
>>>>>> Cache Size: 32
   Semi-random invokes, Behavior = RemoveOldestElement
Re-fetched accessors from 15 elements: 317 
Cache<MemberInfo, MemberAccessor> cache statistics:
Count: 14
Capacity: 32
Number of writes: 14
Number of reads: 1000000
Number of cache hits: 999986
Number of deletes: 0
Hit rate: 100,00 %

   Semi-random invokes, Behavior = RemoveLeastRecentUsedElement
Re-fetched accessors from 15 elements: 597 
Cache<MemberInfo, MemberAccessor> cache statistics:
Count: 14
Capacity: 32
Number of writes: 14
Number of reads: 1000000
Number of cache hits: 999986
Number of deletes: 0
Hit rate: 100,00 %
             */
        }

        //private static void ReFetchedAccessors(Test t, int p1, int p2, MethodInfo miInstanceAction, int iterations, Stopwatch watch, MethodInfo[] randomMethods)
        //{
        //    Random rnd = new Random(0);
        //    watch.Reset();
        //    watch.Start();
        //    for (int i = 0; i < iterations; i++)
        //    {
        //        if (randomMethods != null && (i & 1) == 1)
        //        {
        //            int j;
        //            j = rnd.Next(100);
        //            // a legutóbbiak gyakori használatának szimulálása: 100-ból egyszer ritka eset is jöhet, egyébként ugyanaz a 32 elem
        //            if (j != 0)
        //                j = rnd.Next(Math.Min(32, randomMethods.Length));
        //            else
        //                j = rnd.Next(randomMethods.Length);

        //            object[] args = new object[randomMethods[j].GetParameters().Length];
        //            ParameterInfo[] pi = randomMethods[j].GetParameters();
        //            for (int k = 0; k < args.Length; k++)
        //            {
        //                if (!pi[k].ParameterType.IsEnum && typeof(IConvertible).IsAssignableFrom(pi[k].ParameterType))
        //                    args[k] = Convert.ChangeType(1, pi[k].ParameterType);
        //                else if (pi[k].ParameterType.IsValueType)
        //                    args[k] = Activator.CreateInstance(pi[k].ParameterType);
        //                else if (pi[k].ParameterType == typeof(string))
        //                    args[k] = String.Empty;
        //                else if (pi[k].ParameterType.IsArray)
        //                    args[k] = Activator.CreateInstance(pi[k].ParameterType, 0);
        //            }
        //            MethodInvoker.GetMethodInvoker(randomMethods[j]).Invoke(t, args);
        //        }
        //        else
        //            MethodInvoker.GetMethodInvoker(miInstanceAction).Invoke(t, new object[] { p1, p2 });
        //    }
        //    watch.Stop();
        //    Console.WriteLine("MethodInvoker.GetMethodInvoker from {0} elements (capacity: {2}): {1} ms", randomMethods == null ? 1 : randomMethods.Length + 1, watch.ElapsedMilliseconds, MemberAccessor.CacheSize);

        //}

        [Test]
        public void TestFieldAccess()
        {
            // Obtaining information
            Test t = new Test();
            int fieldValue = 1;
            const string fieldName = "field";

            // new uncached accessor
            FieldInfo fi = t.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            FieldAccessor accessor = new FieldAccessor(fi);

            const int iterations = 1000000;

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);

            // Direct access - set
            Console.WriteLine("==================Field access: (int)==================");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t.field = fieldValue;
                //Test.StaticField = propValue;
            }
            watch.Stop();
            Console.WriteLine("Direct set: " + watch.ElapsedMilliseconds.ToString());

            // Direct access - get
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = t.field;
                //fieldValue = Test.StaticField;
            }
            watch.Stop();
            Console.WriteLine("Direct get: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - set
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                accessor.Set(t, fieldValue);
            }
            watch.Stop();
            Console.WriteLine("Prefetched accessor - set: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - get
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = (int)accessor.Get(t);
            }
            watch.Stop();
            Console.WriteLine("Prefetched accessor - get: " + watch.ElapsedMilliseconds.ToString());

            // FieldAccessor.GetAccessor - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                FieldAccessor.GetAccessor(fi).Set(t, fieldValue);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched accessor - set: " + watch.ElapsedMilliseconds.ToString());

            // FieldAccessor.GetAccessor - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = (int)FieldAccessor.GetAccessor(fi).Get(t);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched accessor - get: " + watch.ElapsedMilliseconds.ToString());

            // FieldInfo.SetValue
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fi.SetValue(t, fieldValue);
            }
            watch.Stop();
            Console.WriteLine("FieldInfo.SetValue: " + watch.ElapsedMilliseconds.ToString());

            // FieldInfo.GetValue
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = (int)fi.GetValue(t);
            }
            watch.Stop();
            Console.WriteLine("FieldInfo.GetValue: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by field info - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetField(t, fi, fieldValue, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetField - Lambda by FieldInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by field info - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = (int)Reflector.GetField(t, fi, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetField - Lambda by FieldInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by field info - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetField(t, fi, fieldValue, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetField - System.Reflection by FieldInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by field info - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = (int)Reflector.GetField(t, fi, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetField - System.Reflection by FieldInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by name - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetField(t, fieldName, fieldValue, ReflectionWays.DynamicDelegate);
                //Reflector.SetStaticFieldByName(t.GetType(), fieldName, fieldValue, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetField - Lambda by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by name - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = (int)Reflector.GetField(t, fieldName, ReflectionWays.DynamicDelegate);
                //propValue = (int)Reflector.GetStaticFieldByName(t.GetType(), fieldName, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetField - Lambda by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by name - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetField(t, fieldName, fieldValue, ReflectionWays.SystemReflection);
                //Reflector.SetStaticFieldByName(t.GetType(), fieldName, fieldValue, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetField - System.Reflection by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by name - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                fieldValue = (int)Reflector.GetField(t, fieldName, ReflectionWays.SystemReflection);
                //propValue = (int)Reflector.GetStaticFieldByName(t.GetType(), fieldName, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetField - System.Reflection by name: " + watch.ElapsedMilliseconds.ToString());
        }

        [Test]
        public void TestPropertyAccess()
        {
            // Obtaining information
            Test t = new Test();
            int propValue = 1;
            string propName = "Value";

            // new uncached accessor
            PropertyInfo pi = t.GetType().GetProperty(propName);
            PropertyAccessor accessor = new SimplePropertyAccessor(pi);

            int iterations = 1000000;

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);

            // Direct access - set
            Console.WriteLine("==================Property access: (int)==================");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t.Value = propValue;
                //Test.StaticProperty = propValue;
            }
            watch.Stop();
            Console.WriteLine("Direct set: " + watch.ElapsedMilliseconds.ToString());

            // Direct access - get
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = t.Value;
                //propValue = Test.StaticProperty;
            }
            watch.Stop();
            Console.WriteLine("Direct get: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - set
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                accessor.Set(t, propValue);
            }
            watch.Stop();
            Console.WriteLine("Prefetched accessor - set: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - get
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)accessor.Get(t);
            }
            watch.Stop();
            Console.WriteLine("Prefetched accessor - get: " + watch.ElapsedMilliseconds.ToString());

            // PropertyAccessor.GetAccessor - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                PropertyAccessor.GetAccessor(pi).Set(t, propValue);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched accessor - set: " + watch.ElapsedMilliseconds.ToString());

            // PropertyAccessor.GetAccessor - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)PropertyAccessor.GetAccessor(pi).Get(t);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched accessor - get: " + watch.ElapsedMilliseconds.ToString());

            // PropertyInfo.SetValue
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                pi.SetValue(t, propValue, null);
            }
            watch.Stop();
            Console.WriteLine("PropertyInfo.SetValue: " + watch.ElapsedMilliseconds.ToString());

            // PropertyInfo.GetValue
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)pi.GetValue(t, null);
            }
            watch.Stop();
            Console.WriteLine("PropertyInfo.GetValue: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by property info - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, pi, propValue, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - Lambda by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by property info - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, pi, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - Lambda by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by property info - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, pi, propValue, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - System.Reflection by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by property info - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, pi, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - System.Reflection by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by name - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, propName, propValue, ReflectionWays.DynamicDelegate);
                //Reflector.SetStaticPropertyByName(t.GetType(), propName, propValue, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - Lambda by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by name - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, propName, ReflectionWays.DynamicDelegate);
                //propValue = (int)Reflector.GetStaticPropertyByName(t.GetType(), propName, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - Lambda by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by name - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, propName, propValue, ReflectionWays.SystemReflection);
                //Reflector.SetStaticPropertyByName(t.GetType(), propName, propValue, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - System.Reflection by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by name - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, propName, ReflectionWays.SystemReflection);
                //propValue = (int)Reflector.GetStaticPropertyByName(t.GetType(), propName, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - System.Reflection by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: TypeDescriptor by name - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, propName, propValue, ReflectionWays.TypeDescriptor);
                // not supported - Reflector.SetStaticPropertyByName(t.GetType(), propName, propValue, ReflectionWays.TypeDescriptor);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - TypeDescriptor by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: TypeDescriptor by name - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, propName, ReflectionWays.TypeDescriptor);
                // not supported - propValue = (int)Reflector.GetStaticPropertyByName(t.GetType(), propName, ReflectionWays.TypeDescriptor);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - TypeDescriptor by name: " + watch.ElapsedMilliseconds.ToString());
        }

        [Test]
        public void TestIndexerAccess()
        {
            // Obtaining information
            Test t = new Test();
            int propValue = 1;
            int ind = 1;
            string propName = "Indexer"; // Item is renamed by IndexerNameAttribute

            // new uncached accessor
            PropertyInfo pi = t.GetType().GetProperty(propName);
            PropertyAccessor accessor = new IndexerAccessor(pi);

            int iterations = 1000000;

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);

            // Direct access - set
            Console.WriteLine("==================Indexer access: ([int]: int)==================");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t[ind] = propValue;
            }
            watch.Stop();
            Console.WriteLine("Direct set: " + watch.ElapsedMilliseconds.ToString());

            // Direct access - get
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = t[ind];
            }
            watch.Stop();
            Console.WriteLine("Direct get: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - set
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                accessor.Set(t, propValue, ind);
            }
            watch.Stop();
            Console.WriteLine("Prefetched accessor - set: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - get
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)accessor.Get(t, ind);
            }
            watch.Stop();
            Console.WriteLine("Prefetched accessor - get: " + watch.ElapsedMilliseconds.ToString());

            // PropertyAccessor.GetAccessor - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                PropertyAccessor.GetAccessor(pi).Set(t, propValue, ind);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched accessor - set: " + watch.ElapsedMilliseconds.ToString());

            // PropertyAccessor.GetAccessor - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)PropertyAccessor.GetAccessor(pi).Get(t, ind);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched accessor - get: " + watch.ElapsedMilliseconds.ToString());

            // PropertyInfo.SetValue
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                pi.SetValue(t, propValue, new object[] { ind });
            }
            watch.Stop();
            Console.WriteLine("PropertyInfo.SetValue: " + watch.ElapsedMilliseconds.ToString());

            // PropertyInfo.GetValue
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)pi.GetValue(t, new object[] { ind });
            }
            watch.Stop();
            Console.WriteLine("PropertyInfo.GetValue: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by property info - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, pi, propValue, ReflectionWays.DynamicDelegate, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - Lambda by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by property info - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, pi, ReflectionWays.DynamicDelegate, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - Lambda by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by property info - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, pi, propValue, ReflectionWays.SystemReflection, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - System.Reflection by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by property info - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, pi, ReflectionWays.SystemReflection, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - System.Reflection by PropertyInfo: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by name - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, propName, propValue, ReflectionWays.DynamicDelegate, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - Lambda by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by name - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, propName, ReflectionWays.DynamicDelegate, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - Lambda by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by name - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetProperty(t, propName, propValue, ReflectionWays.SystemReflection, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetProperty - System.Reflection by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by name - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetProperty(t, propName, ReflectionWays.SystemReflection, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetProperty - System.Reflection by name: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by default members - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetIndexedMember(t, propValue, ReflectionWays.DynamicDelegate, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetIndexer - Lambda: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: Lambda by default members - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetIndexedMember(t, ReflectionWays.DynamicDelegate, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetIndexer - Lambda: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by default members - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetIndexedMember(t, propValue, ReflectionWays.SystemReflection, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetIndexer - System.Reflection: " + watch.ElapsedMilliseconds.ToString());

            // Reflector: System.Reflection by default members - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                propValue = (int)Reflector.GetIndexedMember(t, ReflectionWays.SystemReflection, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetIndexer - System.Reflection: " + watch.ElapsedMilliseconds.ToString());
        }

        [Test]
        public void TestArrayAccess()
        {
            byte[] array = new byte[100];
            byte value = 1;
            int ind = 1;

            int iterations = 1000000;

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);

            // Direct access - set
            Console.WriteLine("==================Array access: (byte[100])==================");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                array[ind] = value;
            }
            watch.Stop();
            Console.WriteLine("Direct set: " + watch.ElapsedMilliseconds.ToString());

            // Direct access - get
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                value = array[ind];
            }
            watch.Stop();
            Console.WriteLine("Direct get: " + watch.ElapsedMilliseconds.ToString());

            // Reflector - set
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                Reflector.SetIndexedMember(array, value, ReflectionWays.Auto, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.SetIndexer - SetValue: " + watch.ElapsedMilliseconds.ToString());

            // Reflector - get
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                value = (byte)Reflector.GetIndexedMember(array, ReflectionWays.Auto, ind);
            }
            watch.Stop();
            Console.WriteLine("Reflector.GetIndexer - GetValue: " + watch.ElapsedMilliseconds.ToString());

        }

        [Test]
        public void TestClassCreation()
        {
            // Obtaining information
            Type type = typeof(Test);

            // new uncached accessors
            ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
            CreateInstanceAccessor factoryByType = new DefaultCreateInstanceAccessor(type);
            CreateInstanceAccessor factoryByCtor = new ParameterizedCreateInstanceAccessor(ci);
            Test t;

            const int iterations = 1000000;

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);
            // Direct execution
            Console.WriteLine("==================Class creation: default constructor==================");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = new Test();
            }
            watch.Stop();
            Console.WriteLine("Direct invocation: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - by type
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)factoryByType.CreateInstance();
            }
            watch.Stop();
            Console.WriteLine("Prefetched factory - by type: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - by ctor
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)factoryByCtor.CreateInstance();
            }
            watch.Stop();
            Console.WriteLine("Prefetched factory - by ctor: " + watch.ElapsedMilliseconds.ToString());

            // Object factory - type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)CreateInstanceAccessor.GetAccessor(type).CreateInstance();
            }
            watch.Stop();
            Console.WriteLine("Re-fetched factory - by type: " + watch.ElapsedMilliseconds.ToString());

            // Object factory - ctor
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)CreateInstanceAccessor.GetAccessor(ci).CreateInstance();
            }
            watch.Stop();
            Console.WriteLine("Re-fetched factory - by ctor: " + watch.ElapsedMilliseconds.ToString());

            // Activator.CreateInstance
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Activator.CreateInstance(type);
            }
            watch.Stop();
            Console.WriteLine("Activator.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // ConstructorInfo.Invoke
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)ci.Invoke(null);
            }
            watch.Stop();
            Console.WriteLine("ConstructorInfo.Invoke: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor.CreateInstance
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)TypeDescriptor.CreateInstance(null, type, null, null);
            }
            watch.Stop();
            Console.WriteLine("TypeDescriptor.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by Type: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by constructor info
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(ci, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by ConstructorInfo: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.DynamicDelegate, new object[] { });
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by Type (Activator): " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by constructor info
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(ci, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by ConstructorInfo (Invoke): " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.SystemReflection, new object[] { });
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by parameters match (Invoke): " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor by type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.TypeDescriptor);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - TypeDescriptor by Type: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.TypeDescriptor, new object[] { });
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - TypeDescriptor by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // Obtaining information
            const int p1 = 1;
            const int p2 = 10;
            ci = type.GetConstructor(new Type[] { typeof(int), typeof(int) });
            factoryByCtor = new ParameterizedCreateInstanceAccessor(ci);

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);
            // Direct execution
            Console.WriteLine("==============Class creation: non-default constructor (int; int)==============");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = new Test(p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Direct invocation: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - by ctor
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)factoryByCtor.CreateInstance(p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Prefetched factory: " + watch.ElapsedMilliseconds.ToString());

            // Object factory - ctor
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)CreateInstanceAccessor.GetAccessor(ci).CreateInstance(p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched factory: " + watch.ElapsedMilliseconds.ToString());

            // Activator.CreateInstance
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Activator.CreateInstance(type, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Activator.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // ConstructorInfo.Invoke
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)ci.Invoke(new object[] { p1, p2 });
            }
            watch.Stop();
            Console.WriteLine("ConstructorInfo.Invoke: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor.CreateInstance
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)TypeDescriptor.CreateInstance(null, type, new Type[] { typeof(int), typeof(int) }, new object[] { p1, p2 });
            }
            watch.Stop();
            Console.WriteLine("TypeDescriptor.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by constructor info
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(ci, ReflectionWays.DynamicDelegate, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by ConstructorInfo: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.DynamicDelegate, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by constructor info
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(ci, ReflectionWays.SystemReflection, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by ConstructorInfo: " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.SystemReflection, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                t = (Test)Reflector.CreateInstance(type, ReflectionWays.TypeDescriptor, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - TypeDescriptor by parameters match: " + watch.ElapsedMilliseconds.ToString());
        }

        [Test]
        public void TestStructCreation()
        {
            // Obtaining information
            Type type = typeof(Point);
            CreateInstanceAccessor factoryByType = new DefaultCreateInstanceAccessor(type);
            Point p;

            const int iterations = 1000000;

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);
            // Direct execution
            Console.WriteLine("==================Struct creation without constructor==================");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = new Point();
            }
            watch.Stop();
            Console.WriteLine("Direct invocation: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - by type
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)factoryByType.CreateInstance();
            }
            watch.Stop();
            Console.WriteLine("Prefetched factory: " + watch.ElapsedMilliseconds.ToString());

            // Object factory - type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)CreateInstanceAccessor.GetAccessor(type).CreateInstance();
            }
            watch.Stop();
            Console.WriteLine("Re-fetched factory: " + watch.ElapsedMilliseconds.ToString());

            // Activator
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Activator.CreateInstance(type);
            }
            watch.Stop();
            Console.WriteLine("Activator.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor.CreateInstance
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)TypeDescriptor.CreateInstance(null, type, null, null);
            }
            watch.Stop();
            Console.WriteLine("TypeDescriptor.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.DynamicDelegate);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by Type: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.DynamicDelegate, new object[] { });
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.SystemReflection);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by Type (Activator): " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.SystemReflection, new object[] { });
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by parameters match (Activator): " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor by type
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.TypeDescriptor);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - TypeDescriptor by Type: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.TypeDescriptor, new object[] { });
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - TypeDescriptor by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // Obtaining information
            const int p1 = 1;
            const int p2 = 10;
            ConstructorInfo ci = type.GetConstructor(new Type[] { typeof(int), typeof(int) });
            CreateInstanceAccessor factoryByCtor = new ParameterizedCreateInstanceAccessor(ci);

            ResetCache();
            Console.WriteLine("Number of iterations: {0:N0}", iterations);
            // Direct execution
            Console.WriteLine("==============Struct creation by constructor (int; int)==============");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = new Point(p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Direct invocation: " + watch.ElapsedMilliseconds.ToString());

            // Lambda accessor - by ctor
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)factoryByCtor.CreateInstance(p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Prefetched factory: " + watch.ElapsedMilliseconds.ToString());

            // Object factory - ctor
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)CreateInstanceAccessor.GetAccessor(ci).CreateInstance(p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Re-fetched factory: " + watch.ElapsedMilliseconds.ToString());

            // Activator.CreateInstance
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Activator.CreateInstance(type, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Activator.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // ConstructorInfo.Invoke
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)ci.Invoke(new object[] { p1, p2 });
            }
            watch.Stop();
            Console.WriteLine("ConstructorInfo.Invoke: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor.CreateInstance
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)TypeDescriptor.CreateInstance(null, type, new Type[] { typeof(int), typeof(int) }, new object[] { p1, p2 });
            }
            watch.Stop();
            Console.WriteLine("TypeDescriptor.CreateInstance: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by constructor info
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(ci, ReflectionWays.DynamicDelegate, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by ConstructorInfo: " + watch.ElapsedMilliseconds.ToString());

            // Lambda by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.DynamicDelegate, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - Lambda by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by constructor info
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(ci, ReflectionWays.SystemReflection, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by ConstructorInfo: " + watch.ElapsedMilliseconds.ToString());

            // System.Reflection by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.SystemReflection, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - System.Reflection by parameters match: " + watch.ElapsedMilliseconds.ToString());

            // TypeDescriptor by parameters match
            ResetCache();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                p = (Point)Reflector.CreateInstance(type, ReflectionWays.TypeDescriptor, p1, p2);
            }
            watch.Stop();
            Console.WriteLine("Reflector.Construct - TypeDescriptor by parameters match: " + watch.ElapsedMilliseconds.ToString());
        }

        [Test]
        public void TestMemberOf()
        {
            MemberInfo mi;
            const int iterations = 1000000;
            string typeName = "System.Int32";
            ResetCache();

            Console.WriteLine("==========Reflect type: {0}===========", typeName);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Type.GetType(typeName);
            }
            watch.Stop();
            Console.WriteLine("Type.GetType(\"{0}\"): {1} ms", typeName, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Reflector.ResolveType(typeName);
            }
            watch.Stop();
            Console.WriteLine("Reflector.ResolveType(\"{0}\"): {1} ms", typeName, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(int);
            }
            watch.Stop();
            Console.WriteLine("typeof(int): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            typeName = "System.Collections.Generic.List`1[System.Int32]";
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Type.GetType(typeName);
            }
            watch.Stop();
            Console.WriteLine("Type.GetType(\"{0}\"): {1} ms", typeName, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Reflector.ResolveType(typeName);
            }
            watch.Stop();
            Console.WriteLine("Reflector.ResolveType(\"{0}\"): {1} ms", typeName, watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(List<int>);
            }
            watch.Stop();
            Console.WriteLine("typeof(List<int>): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();
            Console.WriteLine("==========Reflect function method===========");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(int).GetMethod("Parse", new Type[] { typeof(string), typeof(IFormatProvider) });
            }
            watch.Stop();
            Console.WriteLine("typeof(int).GetMethod(\"Parse\", new Type[] {{ typeof(string), typeof(IFormatProvider) }}): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Reflector.MemberOf(() => int.Parse(null, null));
            }
            watch.Stop();
            Console.WriteLine("Reflector.MemberOf(() => int.Parse(null, null)): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = ((Func<string, IFormatProvider, int>)int.Parse).Method;
            }
            watch.Stop();
            Console.WriteLine("((Func<string, IFormatProvider, int>)int.Parse).Method: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(List<int>).GetMethod("Find");
            }
            watch.Stop();
            Console.WriteLine("typeof(List<int>).GetMethod(\"Find\"): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(List<int>).GetMember("Find", MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public)[0];
            }
            watch.Stop();
            Console.WriteLine("typeof(List<int>).GetMember(\"Find\", MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public)[0]: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
#pragma warning disable CS1720 // Expression will always cause a System.NullReferenceException because the type's default value is null - false alarm, this is an Expression
                mi = Reflector.MemberOf(() => default(List<int>).Find(default(Predicate<int>)));
#pragma warning restore CS1720 // Expression will always cause a System.NullReferenceException because the type's default value is null
            }
            watch.Stop();
            Console.WriteLine("Reflector.MemberOf(() => default(List<int>).Find(default(Predicate<int>))): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = ((Func<Predicate<int>, int>)new List<int>().Find).Method;
            }
            watch.Stop();
            Console.WriteLine("((Func<int, int>)new List<int>().Find).Method: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            Console.WriteLine("==========Reflect action method===========");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            }
            watch.Stop();
            Console.WriteLine("typeof(Console).GetMethod(\"WriteLine\", new Type[] {{ typeof(string) }}): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Reflector.MemberOf(() => Console.WriteLine(default(string)));
            }
            watch.Stop();
            Console.WriteLine("Reflector.MemberOf(() => Console.WriteLine(default(string))): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = ((Action<string>)Console.WriteLine).Method;
            }
            watch.Stop();
            Console.WriteLine("((Action<string>)Console.WriteLine).Method: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(List<int>).GetMethod("Add");
            }
            watch.Stop();
            Console.WriteLine("typeof(List<int>).GetMethod(\"Add\"): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(List<int>).GetMember("Add", MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public)[0];
            }
            watch.Stop();
            Console.WriteLine("typeof(List<int>).GetMember(\"Add\", MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public)[0]: {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
#pragma warning disable CS1720 // Expression will always cause a System.NullReferenceException because the type's default value is null - false alarm, this is an Expression
                mi = Reflector.MemberOf(() => default(List<int>).Add(default(int)));
#pragma warning restore CS1720 // Expression will always cause a System.NullReferenceException because the type's default value is null
            }
            watch.Stop();
            Console.WriteLine("Reflector.MemberOf(() => default(List<int>).Add(default(int))): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = ((Action<int>)new List<int>().Add).Method;
            }
            watch.Stop();
            Console.WriteLine("((Action<int>)new List<int>().Add).Method: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            Console.WriteLine("==========Reflect constructor===========");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(List<int>).GetConstructor(Type.EmptyTypes);
            }
            watch.Stop();
            Console.WriteLine("typeof(List<int>).GetConstructor(Type.EmptyTypes): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Reflector.MemberOf(() => new List<int>());
            }
            watch.Stop();
            Console.WriteLine("Reflector.MemberOf(() => new List<int>()): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            Console.WriteLine("==========Reflect field===========");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(string).GetField("Empty");
            }
            watch.Stop();
            Console.WriteLine("typeof(string).GetField(\"Empty\"): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = Reflector.MemberOf(() => string.Empty);
            }
            watch.Stop();
            Console.WriteLine("Reflector.MemberOf(() => string.Empty): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();

            Console.WriteLine("==========Reflect field===========");
            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                mi = typeof(string).GetProperty("Length");
            }
            watch.Stop();
            Console.WriteLine("typeof(string).GetProperty(\"Length\"): {0} ms", watch.ElapsedMilliseconds);

            watch.Reset();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
#pragma warning disable CS1720 // Expression will always cause a System.NullReferenceException because the type's default value is null - false alarm, this is an Expression
                mi = Reflector.MemberOf(() => default(string).Length);
#pragma warning restore CS1720 // Expression will always cause a System.NullReferenceException because the type's default value is null
            }
            watch.Stop();
            Console.WriteLine("Reflector.MemberOf(() => default(string).Length): {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine();
        }

    }
}
