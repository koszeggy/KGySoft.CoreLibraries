using System;
using System.Collections.Generic;
using System.Reflection;
using KGySoft.Reflection;
using NUnit.Framework;

namespace _LibrariesTest.Tests.Reflection
{
    [TestFixture]
    public class ReflectorTest
    {
        #region Nested test types

        private class TestClass
        {
            private int intField;

            private readonly int readOnlyValueField;
            private readonly string readOnlyRefField;

            public int IntProp
            {
                get { return intField; }
                set { intField = value; }
            }

            private static int staticIntField;
            public static int StaticIntProp
            {
                get { return staticIntField; }
                set { staticIntField = value; }
            }

            public void TestAction(int intValue, string stringValue)
            {
                Console.WriteLine("TestClass.TestAction({0},{1}) invoked", intValue, stringValue ?? "null");
                intField = intValue;
            }

            public void ComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestClass.ComplexTestAction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                intField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public static void StaticTestAction(int intValue, string stringValue)
            {
                Console.WriteLine("TestClass.StaticTestAction({0},{1}) invoked", intValue, stringValue ?? "null");
                staticIntField = intValue;
            }

            public static void StaticComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestClass.StaticComplexTestAction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                staticIntField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public int TestFunction(int intValue, string stringValue)
            {
                Console.WriteLine("TestClass.TestFunction({0},{1}) invoked", intValue, stringValue ?? "null");
                intField = intValue;
                return intValue;
            }

            public static int StaticTestFunction(int intValue, string stringValue)
            {
                Console.WriteLine("TestClass.StaticTestFunction({0},{1}) invoked", intValue, stringValue ?? "null");
                staticIntField = intValue;
                return intValue;
            }

            public int ComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestClass.TestFunction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                intField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            public static int StaticComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestClass.StaticComplexTestFunction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                staticIntField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            public TestClass()
            {
                Console.WriteLine("TestClass.DefaultConstructor invoked");
                intField = 1;
            }

            public TestClass(int value)
            {
                Console.WriteLine("TestClass.Constructor({0}) invoked", value);
                intField = value;
            }

            public TestClass(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestClass.Constructor({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                intField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public int this[int intValue, string stringValue]
            {
                get
                {
                    Console.WriteLine("TestClass.IndexerGetter[{0},{1}] invoked", intValue, stringValue);
                    return intField;
                }
                set
                {
                    Console.WriteLine("TestClass.IndexerSetter[{0},{1}] = {2} invoked", intValue, stringValue, value);
                    intField = value;
                }
            }
        }

        private struct TestStruct
        {
            private int intField;
            public int IntProp
            {
                get { return intField; }
                set { intField = value; }
            }

            private static int staticIntField;
            public static int StaticIntProp
            {
                get { return staticIntField; }
                set { staticIntField = value; }
            }

            private readonly int readOnlyValueField;
            private readonly string readOnlyRefField;

            public void TestAction(int intValue, string stringValue)
            {
                Console.WriteLine("TestStruct.TestAction({0},{1}) invoked", intValue, stringValue ?? "null");
                intField = intValue;
            }

            public void ComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestStruct.ComplexTestAction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                intField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public static void StaticTestAction(int intValue, string stringValue)
            {
                Console.WriteLine("TestStruct.StaticTestAction({0},{1}) invoked", intValue, stringValue ?? "null");
                staticIntField = intValue;
            }

            public static void StaticComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestStruct.StaticComplexTestAction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                staticIntField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public int TestFunction(int intValue, string stringValue)
            {
                Console.WriteLine("TestStruct.TestFunction({0},{1}) invoked", intValue, stringValue ?? "null");
                intField = intValue;
                return intValue;
            }

            public static int StaticTestFunction(int intValue, string stringValue)
            {
                Console.WriteLine("TestStruct.StaticTestFunction({0},{1}) invoked", intValue, stringValue ?? "null");
                staticIntField = intValue;
                return intValue;
            }

            public int ComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestStruct.TestFunction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                intField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            public static int StaticComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestStruct.StaticComplexTestFunction({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                staticIntField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            public TestStruct(int value)
            {
                Console.WriteLine("TestStruct.Constructor({0}) invoked", value);
                readOnlyValueField = value;
                intField = value;
                readOnlyRefField = value.ToString();
            }

            public TestStruct(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine("TestClass.Constructor({0},{1},{2},{3}) invoked", intValue, stringValue ?? "null", refBoolValue, refStringValue ?? "null");
                intField = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                readOnlyValueField = intValue;
                readOnlyRefField = stringValue;
            }

            public int this[int intValue, string stringValue]
            {
                get
                {
                    Console.WriteLine("TestStruct.IndexerGetter[{0},{1}] invoked", intValue, stringValue);
                    return intField;
                }
                set
                {
                    Console.WriteLine("TestStruct.IndexerSetter[{0},{1}] = {2} invoked", intValue, stringValue, value);
                    intField = value;
                }
            }
        }

        #endregion

        #region Class method invoke

        [Test]
        public void ClassInstanceSimpleActionMethodInvoke()
        {
            const string memberName = "TestAction";
            object test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma" };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestClass(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
        }

        [Test]
        public void ClassStaticSimpleActionMethodInvoke()
        {
            const string memberName = "StaticTestAction";
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma" };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
        }

        [Test]
        public void ClassInstanceComplexActionMethodInvoke()
        {
            const string memberName = "ComplexTestAction";
            object test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassStaticComplexActionMethodInvoke()
        {
            const string memberName = "StaticComplexTestAction";
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassInstanceSimpleFunctionMethodInvoke()
        {
            const string memberName = "TestFunction";
            object test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma" };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestClass(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
        }

        [Test]
        public void ClassStaticSimpleFunctionMethodInvoke()
        {
            const string memberName = "StaticTestFunction";
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma" };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
        }

        [Test]
        public void ClassInstanceComplexFunctionMethodInvoke()
        {
            const string memberName = "ComplexTestFunction";
            object test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassStaticComplexFunctionMethodInvoke()
        {
            const string memberName = "StaticComplexTestFunction";
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        #endregion

        #region Struct method invoke

        [Test]
        public void StructInstanceSimpleActionMethodInvoke()
        {
            const string memberName = "TestAction";
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma" };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestStruct(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
        }

        [Test]
        public void StructStaticSimpleActionMethodInvoke()
        {
            const string memberName = "StaticTestAction";
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma" };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
        }

        [Test]
        public void StructInstanceComplexActionMethodInvoke()
        {
            const string memberName = "ComplexTestAction";
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexActionMethodInvoke()
        {
            const string memberName = "StaticComplexTestAction";
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructInstanceSimpleFunctionMethodInvoke()
        {
            const string memberName = "TestFunction";
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma" };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestStruct(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
        }

        [Test]
        public void StructStaticSimpleFunctionMethodInvoke()
        {
            const string memberName = "StaticTestFunction";
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma" };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
        }

        [Test]
        public void StructInstanceComplexFunctionMethodInvoke()
        {
            const string memberName = "ComplexTestFunction";
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(memberName);
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(test, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(test, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexFunctionMethodInvoke()
        {
            const string memberName = "StaticComplexTestFunction";
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(memberName);
            PropertyInfo intProp = testType.GetProperty("StaticIntProp");
            object[] args = new object[] { 1, "alma", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Invoker...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.RunMethod(testType, memberName, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], Reflector.GetProperty(null, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            args = new object[] { "10", null };
            Reflector.RunMethod(typeof(Int32), "TryParse", args);
            Assert.AreEqual(10, args[1]);
        }

        #endregion

        #region Class property access

        [Test]
        public void ClassInstancePropertyAccess()
        {
            const string memberName = "IntProp";
            object test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty(memberName);
            object result, value = 1;

            Console.Write("System Reflection...");
            pi.SetValue(test, value, null);
            result = pi.GetValue(test, null);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor...");
            PropertyAccessor.GetAccessor(pi).Set(test, value);
            result = PropertyAccessor.GetAccessor(pi).Get(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, memberName, value);
            result = Reflector.GetProperty(test, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassStaticPropertyAccess()
        {
            const string memberName = "StaticIntProp";
            Type testType = typeof(TestClass);
            PropertyInfo pi = testType.GetProperty(memberName);
            object result, value = 1;

            Console.Write("System Reflection...");
            pi.SetValue(null, value, null);
            result = pi.GetValue(null, null);
            Assert.AreEqual(value, result);

            TestClass.StaticIntProp = 0;
            Console.Write("Property Accessor...");
            PropertyAccessor.GetAccessor(pi).Set(null, value);
            result = PropertyAccessor.GetAccessor(pi).Get(null);
            Assert.AreEqual(value, result);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            Assert.AreEqual(value, result);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, memberName, value);
            result = Reflector.GetProperty(testType, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassInstanceIndexerAccess()
        {
            object test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", new Type[] { typeof(int), typeof(string) });
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma" };
            object result, value = 1;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            pi.SetValue(test, value, parameters);
            result = pi.GetValue(test, parameters);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor...");
            parameters = (object[])args.Clone();
            PropertyAccessor.GetAccessor(pi).Set(test, value, parameters);
            result = PropertyAccessor.GetAccessor(pi).Get(test, parameters);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            parameters = (object[])args.Clone();
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, parameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, parameters);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by parameters match)...");
            parameters = (object[])args.Clone();
            Reflector.SetIndexedMember(test, value, parameters);
            result = Reflector.GetIndexedMember(test, parameters);
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Struct property access

        [Test]
        public void StructInstancePropertyAccess()
        {
            const string memberName = "IntProp";
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty(memberName);
            object result, value = 1;

            Console.Write("System Reflection...");
            pi.SetValue(test, value, null);
            result = pi.GetValue(test, null);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Property Accessor...");
            PropertyAccessor.GetAccessor(pi).Set(test, value);
            result = PropertyAccessor.GetAccessor(pi).Get(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, memberName, value);
            result = Reflector.GetProperty(test, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructStaticPropertyAccess()
        {
            const string memberName = "StaticIntProp";
            Type testType = typeof(TestStruct);
            PropertyInfo pi = testType.GetProperty(memberName);
            object result, value = 1;

            Console.Write("System Reflection...");
            pi.SetValue(null, value, null);
            result = pi.GetValue(null, null);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Property Accessor...");
            PropertyAccessor.GetAccessor(pi).Set(null, value);
            result = PropertyAccessor.GetAccessor(pi).Get(null);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, memberName, value);
            result = Reflector.GetProperty(testType, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructInstanceIndexerAccess()
        {
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", new Type[] { typeof(int), typeof(string) });
            PropertyInfo intProp = test.GetType().GetProperty("IntProp");
            object[] args = new object[] { 1, "alma" };
            object result, value = 1;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            pi.SetValue(test, value, parameters);
            result = pi.GetValue(test, parameters);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Property Accessor...");
            parameters = (object[])args.Clone();
            PropertyAccessor.GetAccessor(pi).Set(test, value, parameters);
            result = PropertyAccessor.GetAccessor(pi).Get(test, parameters);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by PropertyInfo)...");
            parameters = (object[])args.Clone();
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, parameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, parameters);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by parameters match)...");
            parameters = (object[])args.Clone();
            Reflector.SetIndexedMember(test, value, parameters);
            result = Reflector.GetIndexedMember(test, parameters);
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Class field access

        [Test]
        public void ClassInstanceReadOnlyValueFieldAccess()
        {
            const string memberName = "readOnlyValueField";
            object test = new TestClass(0);
            FieldInfo fi = test.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.NonPublic);
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            FieldAccessor.GetAccessor(fi).Set(test, value);
            result = FieldAccessor.GetAccessor(fi).Get(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, memberName, value);
            result = Reflector.GetField(test, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassInstanceReadOnlyRefFieldAccess()
        {
            const string memberName = "readOnlyRefField";
            object test = new TestClass(0);
            FieldInfo fi = test.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.NonPublic);
            object result, value = "trallala";

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            FieldAccessor.GetAccessor(fi).Set(test, value);
            result = FieldAccessor.GetAccessor(fi).Get(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, memberName, value);
            result = Reflector.GetField(test, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassStaticFieldAccess()
        {
            const string memberName = "staticIntField";
            Type testType = typeof(TestClass);
            FieldInfo fi = testType.GetField(memberName, BindingFlags.Static | BindingFlags.NonPublic);
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(null, value);
            result = fi.GetValue(null);
            Assert.AreEqual(value, result);

            TestClass.StaticIntProp = 0;
            Console.Write("Field Accessor...");
            FieldAccessor.GetAccessor(fi).Set(null, value);
            result = FieldAccessor.GetAccessor(fi).Get(null);
            Assert.AreEqual(value, result);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            Assert.AreEqual(value, result);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, memberName, value);
            result = Reflector.GetField(testType, memberName);
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Struct field access

        [Test]
        public void StructInstanceFieldAccess()
        {
            const string memberName = "intField";
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.NonPublic);
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            FieldAccessor.GetAccessor(fi).Set(test, value);
            result = FieldAccessor.GetAccessor(fi).Get(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, memberName, value);
            result = Reflector.GetField(test, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructInstanceReadOnlyValueFieldAccess()
        {
            const string memberName = "readOnlyValueField";
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.NonPublic);
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            FieldAccessor.GetAccessor(fi).Set(test, value);
            result = FieldAccessor.GetAccessor(fi).Get(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, memberName, value);
            result = Reflector.GetField(test, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructInstanceReadOnlyRefFieldAccess()
        {
            const string memberName = "readOnlyRefField";
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.NonPublic);
            object result, value = "trallala";

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            FieldAccessor.GetAccessor(fi).Set(test, value);
            result = FieldAccessor.GetAccessor(fi).Get(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, memberName, value);
            result = Reflector.GetField(test, memberName);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructStaticFieldAccess()
        {
            const string memberName = "staticIntField";
            Type testType = typeof(TestStruct);
            FieldInfo fi = testType.GetField(memberName, BindingFlags.Static | BindingFlags.NonPublic);
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(null, value);
            result = fi.GetValue(null);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Field Accessor...");
            FieldAccessor.GetAccessor(fi).Set(null, value);
            result = FieldAccessor.GetAccessor(fi).Get(null);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, memberName, value);
            result = Reflector.GetField(testType, memberName);
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Class construction

        [Test]
        public void ClassConstructionByType()
        {
            Type testType = typeof(TestClass);
            PropertyInfo intProp = testType.GetProperty("IntProp");
            object result;

            Console.Write("System Activator...");
            result = Activator.CreateInstance(testType);
            Assert.AreEqual(1, Reflector.GetProperty(result, intProp));

            Console.Write("Object Factory...");
            result = CreateInstanceAccessor.GetAccessor(testType).CreateInstance();
            Assert.AreEqual(1, Reflector.GetProperty(result, intProp));

            Console.Write("Reflector...");
            result = Reflector.CreateInstance(testType);
            Assert.AreEqual(1, Reflector.GetProperty(result, intProp));
        }

        [Test]
        public void ClassConstructionByCtorInfo()
        {
            Type testType = typeof(TestClass);
            ConstructorInfo ci = testType.GetConstructor(new Type[] { typeof(int) });
            PropertyInfo intProp = testType.GetProperty("IntProp");
            object[] args = new object[] { 1 };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = ci.Invoke(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));

            Console.Write("Object Factory...");
            parameters = (object[])args.Clone();
            result = CreateInstanceAccessor.GetAccessor(ci).CreateInstance(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
        }

        [Test]
        public void ClassComplexConstructionByCtorInfo()
        {
            Type testType = typeof(TestClass);
            ConstructorInfo ci = testType.GetConstructor(new Type[] { typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType() });
            PropertyInfo intProp = testType.GetProperty("IntProp");
            object[] args = new object[] { 1, "alma", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = ci.Invoke(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("Object Factory...");
            parameters = (object[])args.Clone();
            result = CreateInstanceAccessor.GetAccessor(ci).CreateInstance(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        #endregion

        #region Struct construction

        [Test]
        public void StructConstructionByType()
        {
            Type testType = typeof(TestStruct);
            PropertyInfo intProp = testType.GetProperty("IntProp");
            object result;

            Console.Write("System Activator...");
            result = Activator.CreateInstance(testType);
            Assert.AreEqual(default(TestStruct), result);

            Console.Write("Object Factory...");
            result = CreateInstanceAccessor.GetAccessor(testType).CreateInstance();
            Assert.AreEqual(default(TestStruct), result);

            Console.Write("Reflector...");
            result = Reflector.CreateInstance(testType);
            Assert.AreEqual(default(TestStruct), result);
        }

        [Test]
        public void StructConstructionByCtorInfo()
        {
            Type testType = typeof(TestStruct);
            ConstructorInfo ci = testType.GetConstructor(new Type[] { typeof(int) });
            PropertyInfo intProp = testType.GetProperty("IntProp");
            object[] args = new object[] { 1 };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = ci.Invoke(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));

            Console.Write("Object Factory...");
            parameters = (object[])args.Clone();
            result = CreateInstanceAccessor.GetAccessor(ci).CreateInstance(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
        }

        [Test]
        public void StructComplexConstructionByCtorInfo()
        {
            Type testType = typeof(TestStruct);
            ConstructorInfo ci = testType.GetConstructor(new Type[] { typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType() });
            PropertyInfo intProp = testType.GetProperty("IntProp");
            object[] args = new object[] { 1, "alma", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = ci.Invoke(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("Object Factory...");
            parameters = (object[])args.Clone();
            result = CreateInstanceAccessor.GetAccessor(ci).CreateInstance(parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], Reflector.GetProperty(result, intProp));
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        #endregion

        #region Extensions

        //[Test]
        //public void ExtensionsTest()
        //{
        //    Assert.AreEqual(42, typeof(int).Parse("42"));
        //}

        #endregion

        #region MemberOf

        [Test]
        public void MemberOfTest()
        {
            MemberInfo methodIntParse = Reflector.MemberOf(() => int.Parse(null, null)); // MethodInfo: int.Parse(string, IFormatProvider)
            MemberInfo ctorList = Reflector.MemberOf(() => new List<int>()); // ConstructorInfo: List<int>().ctor()
            MemberInfo fieldEmpty = Reflector.MemberOf(() => string.Empty); // FieldInfo: string.Empty
            MemberInfo propertyLength = Reflector.MemberOf(() => default(string).Length); // PropertyInfo: string.Length

            MethodInfo methodAdd = Reflector.MemberOf(() => default(List<int>).Add(default(int))); // MethodInfo: List<int>.Add()
        }

        #endregion

        // TODO: extensions on object (instance members), on Type (static members (and instance?), construction), on MemberInfos (specific actions instance/static)     
    }
}
