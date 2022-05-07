#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReflectorTest.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
#if NETFRAMEWORK
using System.Security;
using System.Security.Permissions;
#endif

using KGySoft.Reflection;

using NUnit.Framework;
using NUnit.Framework.Internal;

#endregion

#pragma warning disable 649

namespace KGySoft.CoreLibraries.UnitTests.Reflection
{
    [TestFixture]
    public class ReflectorTest : TestBase
    {
        #region Nested types

        #region Nested classes

        #region TestClass class

        private class TestClass
        {
            #region Fields

            #region Static Fields

            public static int StaticIntField;

            #endregion

            #region Instance Fields

            public readonly int ReadOnlyValueField;

            public readonly string ReadOnlyReferenceField;

            public int IntField;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Static Properties

            public static int StaticIntProp { get; set; }

            #endregion

            #region Instance Properties

            public int IntProp { get; set; }

            #endregion

            #endregion

            #region Indexers

            public int this[int intValue]
            {
                get
                {
                    Console.WriteLine($"{nameof(TestClass)}.IndexerGetter[{intValue}] invoked");
                    return IntProp;
                }
                set
                {
                    Console.WriteLine($"{nameof(TestClass)}.IndexerSetter[{intValue}] = {value} invoked");
                    IntProp = value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            public TestClass()
            {
                Console.WriteLine($"{nameof(TestClass)}.Constructor() invoked");
                IntProp = 1;
            }

            public TestClass(int value)
            {
                Console.WriteLine($"{nameof(TestClass)}.Constructor({value}) invoked");
                IntProp = value;
            }

            public TestClass(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestClass)}.Constructor({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                IntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            #endregion

            #region Methods

            #region Static Methods

            public static void StaticTestAction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestClass)}.{nameof(StaticTestAction)}({intValue},{stringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
            }

            public static void StaticComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestClass)}.{nameof(StaticComplexTestAction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public static int StaticTestFunction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestClass)}.{nameof(StaticTestFunction)}({intValue},{stringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
                return intValue;
            }

            public static int StaticComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestClass)}.{nameof(StaticComplexTestFunction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            #endregion

            #region Instance Methods

            public void TestAction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestClass)}.{nameof(TestAction)}({intValue},{stringValue ?? "null"}) invoked");
                IntProp = intValue;
            }

            public void ComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestClass)}.{nameof(ComplexTestAction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                IntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public int TestFunction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestClass)}.{nameof(TestFunction)}({intValue},{stringValue ?? "null"}) invoked");
                IntProp = intValue;
                return intValue;
            }

            public int ComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestClass)}.{nameof(ComplexTestFunction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                IntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            #endregion

            #endregion
        }

        #endregion

        #region Sandbox class

#if NETFRAMEWORK
        private class Sandbox : MarshalByRefObject
        {
            internal void DoTest()
            {
#if !NET35
                Assert.IsFalse(AppDomain.CurrentDomain.IsFullyTrusted);
#endif
                var test = new ReflectorTest();
                test.ClassStaticFieldAccess();
                test.StructInstancePropertyAccess();

                // this invokes the dynamic method creation
                Console.WriteLine(Reflector<KeyValuePair<int, string>>.SizeOf);
            }
        }
#endif

        #endregion

        #endregion

        #region Nested structs

        #region TestStruct struct

        private struct TestStruct
        {
            #region Fields

            #region Static Fields

            public static int StaticIntField;

            #endregion

            #region Instance Fields

            public readonly int ReadOnlyValueField;

            public readonly string ReadOnlyReferenceField;

            public int IntField;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Static Properties

            public static int StaticIntProp { get; set; }

            #endregion

            #region Instance Properties

            public int IntProp { get; set; }

            #endregion

            #endregion

            #region Indexers

            public int this[int intValue]
            {
                get
                {
                    Console.WriteLine($"{nameof(TestStruct)}.IndexerGetter[{intValue}] invoked");
                    return IntProp;
                }
                set
                {
                    Console.WriteLine($"{nameof(TestStruct)}.IndexerSetter[{intValue}] = {value} invoked");
                    IntProp = value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            public TestStruct(int value)
            {
                Console.WriteLine($"{nameof(TestStruct)}.Constructor({value}) invoked");
                IntField = value;
                ReadOnlyValueField = value;
                IntProp = value;
                ReadOnlyReferenceField = value.ToString();
            }

            public TestStruct(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestStruct)}.Constructor({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                IntField = intValue;
                IntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                ReadOnlyValueField = intValue;
                ReadOnlyReferenceField = stringValue;
            }

            #endregion

            #region Methods

            #region Static Methods

            public static void StaticTestAction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(StaticTestAction)}({intValue},{stringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
            }

            public static void StaticComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(StaticComplexTestAction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public static int StaticTestFunction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(StaticTestFunction)}({intValue},{stringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
                return intValue;
            }

            public static int StaticComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(StaticComplexTestFunction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                StaticIntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            #endregion

            #region Instance Methods

            public void TestAction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(TestAction)}({intValue},{stringValue ?? "null"}) invoked");
                IntProp = intValue;
            }

            public void ComplexTestAction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(ComplexTestAction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                IntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
            }

            public int TestFunction(int intValue, string stringValue)
            {
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(TestFunction)}({intValue},{stringValue ?? "null"}) invoked");
                IntProp = intValue;
                return intValue;
            }

            public int ComplexTestFunction(int intValue, string stringValue, out bool refBoolValue, ref string refStringValue)
            {
                refBoolValue = default(bool);
                Console.WriteLine($"{nameof(TestStruct)}.{nameof(ComplexTestFunction)}({intValue},{stringValue ?? "null"},{refBoolValue},{refStringValue ?? "null"}) invoked");
                IntProp = intValue;
                refBoolValue = intValue != 0;
                refStringValue = stringValue;
                return intValue;
            }

            #endregion

            #endregion
        }

        #endregion

        #region TestStructWithParameterlessCtor struct

        private struct TestStructWithParameterlessCtor
        {
            #region Properties

            public bool Initialized { get; }

            #endregion

            #region Constructors

            public TestStructWithParameterlessCtor()
            {
                Console.WriteLine($"{nameof(TestStructWithParameterlessCtor)}.Constructor() invoked");
                Initialized = true;
            }

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region Class method invoke

        [Test]
        public void ClassInstanceSimpleActionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.TestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParametersInvalid);

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            accessor.InvokeInstanceAction(test, arg1, arg2);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeStaticAction(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestAction), mi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.InvokeStaticAction<TestClass, int, string>(null, arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestAction), mi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.InvokeInstanceAction<TestClass, int, string>(null, arg1, arg2), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.InvokeInstanceAction(test, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestAction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceAction(test, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestAction), mi.DeclaringType));

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.TestAction), parameters);
            Assert.AreEqual(arg1, test.IntProp);
        }

        [Test]
        public void ClassStaticSimpleActionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParametersInvalid);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg1, arg2);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeInstanceAction(new TestClass(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestClass.StaticTestAction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticAction(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestAction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticAction(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestAction), mi.DeclaringType));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestAction), parameters);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
        }

        [Test]
        public void ClassInstanceComplexActionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.ComplexTestAction));
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], test.IntProp);
                Assert.AreNotEqual(args[2], parameters[2]);
            }

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestAction), parameters);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassStaticComplexActionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticComplexTestAction));
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], TestClass.StaticIntProp);
                Assert.AreNotEqual(args[2], parameters[2]);
            }

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestAction), parameters);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassInstanceSimpleFunctionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.TestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParametersInvalid);

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeInstanceFunction<TestClass, int, string, int>(test, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeStaticFunction<int, string, int>(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestFunction), mi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.InvokeStaticFunction<TestClass, int, string, int>(null, arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestFunction), mi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, int>(null, arg1, arg2), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int, int>(test, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, string, int, int>(test, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, object>(test, arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestFunction), mi.DeclaringType));

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.TestFunction), parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);
        }

        [Test]
        public void ClassStaticSimpleFunctionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParametersInvalid);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<int, string, int>(arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, int>(new TestClass(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<int, int>(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<string, int, int>(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<int, string, object>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestFunction), parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
        }

        [Test]
        public void ClassInstanceComplexFunctionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.ComplexTestFunction));
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], test.IntProp);
                Assert.AreNotEqual(args[2], parameters[2]);
            }

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassStaticComplexFunctionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticComplexTestFunction));
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], TestClass.StaticIntProp);
                Assert.AreNotEqual(args[2], parameters[2]); 
            }

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        #endregion

        #region Struct method invoke

        [Test]
        public void StructInstanceSimpleActionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.TestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(arg1, ((TestStruct)test).IntProp);
                Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            }

            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParametersInvalid);

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            accessor.InvokeInstanceAction(testStruct, arg1, arg2);
            Assert.AreEqual(arg1, testStruct.IntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeStaticAction(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestAction), mi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.InvokeStaticAction<TestStruct, int, string>(default, arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestAction), mi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.InvokeInstanceAction(testStruct, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestAction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceAction(testStruct, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestAction), mi.DeclaringType));

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.TestAction), parameters);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);
        }

        [Test]
        public void StructStaticSimpleActionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParametersInvalid);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg1, arg2);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeInstanceAction(new TestStruct(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestStruct.StaticTestAction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticAction(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestAction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticAction(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestAction), mi.DeclaringType));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestAction), parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
        }

        [Test]
        public void StructInstanceComplexActionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.ComplexTestAction));
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
                Assert.AreNotEqual(args[2], parameters[2]); 
            }

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestAction), parameters);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexActionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticComplexTestAction));
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], TestStruct.StaticIntProp);
                Assert.AreNotEqual(args[2], parameters[2]);
            }

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestAction), parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructInstanceSimpleFunctionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.TestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, result);
            if (TestedFramework != TargetFramework.NetStandard20)
                Assert.AreEqual(arg1, ((TestStruct)test).IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParametersInvalid);

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeInstanceFunction<TestStruct, int, string, int>(testStruct, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, testStruct.IntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeStaticFunction<int, string, int>(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.InvokeStaticFunction<TestStruct, int, string, int>(new TestStruct(), arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestStruct, int, int>(testStruct, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestStruct, string, int, int>(testStruct, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestStruct, int, string, object>(testStruct, arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType));

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.TestFunction), parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);
        }

        [Test]
        public void StructStaticSimpleFunctionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = { arg1, arg2 };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParametersInvalid);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<int, string, int>(arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);
            Throws<InvalidOperationException>(() => accessor.InvokeInstanceFunction<TestStruct, int, string, int>(new TestStruct(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<int, int>(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<string, int, int>(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<int, string, object>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestFunction), parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);
        }

        [Test]
        public void StructInstanceComplexFunctionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.ComplexTestFunction));
            object[] args = { 1, "dummy", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
                Assert.AreNotEqual(args[2], parameters[2]); 
            }

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexFunctionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticComplexTestFunction));
            object[] args = { 1, "dummy", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], TestStruct.StaticIntProp);
                Assert.AreNotEqual(args[2], parameters[2]);
            }

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            args = new object[] { "10", null };
            Reflector.InvokeMethod(typeof(Int32), nameof(Int32.TryParse), args);
            Assert.AreEqual(10, args[1]);
        }

        #endregion

        #region Class property access

        [Test]
        public void ClassInstancePropertyAccess()
        {
            object test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty(nameof(TestClass.IntProp));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1;

            Console.Write("System Reflection...");
            pi.SetValue(test, value, null);
            result = pi.GetValue(test, null);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor...");
            accessor.Set(test, value);
            result = PropertyAccessor.GetAccessor(pi).Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue((TestClass)test, (int)value);
            result = PropertyAccessor.GetAccessor(pi).GetInstanceValue<TestClass, int>((TestClass)test);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(TestClass.IntProp), value);
            result = Reflector.GetProperty(test, nameof(TestClass.IntProp));
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestClass.IntProp), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestClass.IntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestClass.IntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.IntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestClass.IntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestClass.IntProp)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestClass.IntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.IntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
        }

        [Test]
        public void ClassStaticPropertyAccess()
        {
            Type testType = typeof(TestClass);
            PropertyInfo pi = testType.GetProperty(nameof(TestClass.StaticIntProp));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
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
            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntProp = 0;
            Console.Write("Property Accessor Generic...");
            PropertyAccessor.GetAccessor(pi).SetStaticValue((int)value);
            result = PropertyAccessor.GetAccessor(pi).GetStaticValue<int>();
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticIntProp), testType));
            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticIntProp), testType));
            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticIntProp), testType));
            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticIntProp), testType));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp), value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticIntProp));
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(TestClass.StaticIntProp), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestClass.StaticIntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
            Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(TestClass.StaticIntProp)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestClass.StaticIntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
        }

        [Test]
        public void ClassInstanceIndexerAccess()
        {
            var test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(int) });
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object[] args = { 1 };
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
            Throws<ArgumentNullException>(() => accessor.Set(null, value, parameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null, parameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Set(new object(), value, parameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", parameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value, Res.ReflectionEmptyIndices));
            Throws<ArgumentException>(() => accessor.Set(test, value, "1"), Res.ReflectionParametersInvalid);
            Assert.DoesNotThrow(() => accessor.Set(test, value, 1, "2"), "More parameters than needed are okay");
            Throws<ArgumentNullException>(() => accessor.Get(null, parameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Get(test, Res.ReflectionEmptyIndices));
            Throws<ArgumentException>(() => accessor.Get(test, "1"), Res.ReflectionParametersInvalid);
            Assert.DoesNotThrow(() => accessor.Get(test, 1, "2"), "More parameters than needed are okay");

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, (int)value, (int)args[0]);
            result = accessor.GetInstanceValue<TestClass, int, int>(test, (int)args[0]);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1, 1), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1", 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int, int>(null, 1), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int, int>(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestClass, int>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestClass, int, string>(test, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            parameters = (object[])args.Clone();
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, parameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, parameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, args), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ReflectionParametersInvalid);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, args), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ReflectionParametersInvalid);

            test = new TestClass(0);
            Console.Write("Reflector (by parameters match)...");
            parameters = (object[])args.Clone();
            Reflector.SetIndexedMember(test, value, parameters);
            result = Reflector.GetIndexedMember(test, parameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, args), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, args), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, "1"), Res.ReflectionIndexerNotFound(test.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, args), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), args), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, "1"), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        #endregion

        #region Struct property access

        [Test]
        public void StructInstancePropertyAccess()
        {
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty(nameof(TestStruct.IntProp));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1;

            Console.Write("System Reflection...");
            pi.SetValue(test, value, null);
            result = pi.GetValue(test, null);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Property Accessor...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value),
                TargetFramework.NetStandard20))
            {
                result = accessor.Get(test);
                Assert.AreEqual(value, result);
                Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
                Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
            }

            var testStruct = new TestStruct(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, (int)value);
            result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));

            test = new TestStruct(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(TestStruct.IntProp), value);
            result = Reflector.GetProperty(test, nameof(TestStruct.IntProp));
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestStruct.IntProp), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestStruct.IntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestStruct.IntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.IntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestStruct.IntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestStruct.IntProp)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestStruct.IntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.IntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
        }

        [Test]
        public void StructStaticPropertyAccess()
        {
            Type testType = typeof(TestStruct);
            PropertyInfo pi = testType.GetProperty(nameof(TestStruct.StaticIntProp));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1;

            Console.Write("System Reflection...");
            pi.SetValue(null, value, null);
            result = pi.GetValue(null, null);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Property Accessor...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntProp = 0;
            var testStruct = new TestStruct();
            Console.Write("Property Accessor Generic...");
            PropertyAccessor.GetAccessor(pi).SetStaticValue((int)value);
            result = PropertyAccessor.GetAccessor(pi).GetStaticValue<int>();
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(testStruct, value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestStruct.StaticIntProp), testType));
            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.StaticIntProp), testType));
            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<TestStruct, int>(testStruct), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestStruct.StaticIntProp), testType));
            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.StaticIntProp), testType));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), value);
            result = Reflector.GetProperty(testType, nameof(TestStruct.StaticIntProp));
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(type:null!, nameof(TestStruct.StaticIntProp), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestStruct.StaticIntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestStruct.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.IntProp), testType));
            Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(type:null!, nameof(TestStruct.StaticIntProp)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestStruct.StaticIntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestStruct.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.IntProp), testType));
        }

        [Test]
        public void StructInstanceIndexerAccess()
        {
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(int) });
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object[] args = { 1 };
            object result, value = 1;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            pi.SetValue(test, value, parameters);
            result = pi.GetValue(test, parameters);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Property Accessor...");
            parameters = (object[])args.Clone();
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, parameters),
                TargetFramework.NetStandard20))
            {
                result = accessor.Get(test, parameters);
                Assert.AreEqual(value, result);
                Throws<ArgumentNullException>(() => accessor.Set(null, value, parameters), Res.ReflectionInstanceIsNull);
                Throws<ArgumentNullException>(() => accessor.Set(test, null, parameters), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.Set(new object(), value, parameters), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, "1", parameters), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, value, Res.ReflectionEmptyIndices));
                Throws<ArgumentException>(() => accessor.Set(test, value, "1"), Res.ReflectionParametersInvalid);
                Assert.DoesNotThrow(() => accessor.Set(test, value, 1, "2"), "More parameters than needed are okay");
                Throws<ArgumentNullException>(() => accessor.Get(null, parameters), Res.ReflectionInstanceIsNull);
                Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Get(test, Res.ReflectionEmptyIndices));
                Throws<ArgumentException>(() => accessor.Get(test, "1"), Res.ReflectionParametersInvalid);
                Assert.DoesNotThrow(() => accessor.Get(test, 1, "2"), "More parameters than needed are okay");
            }

            var testStruct = new TestStruct(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, (int)value, (int)args[0]);
            result = accessor.GetInstanceValue<TestStruct, int, int>(testStruct, (int)args[0]);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1", 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int, int>(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, int>(testStruct), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, int, string>(testStruct, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new TestStruct(0);
            Console.Write("Reflector (by PropertyInfo)...");
            parameters = (object[])args.Clone();
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, parameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, parameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, args), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ReflectionParametersInvalid);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, args), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ReflectionParametersInvalid);

            test = new TestStruct(0);
            Console.Write("Reflector (by parameters match)...");
            parameters = (object[])args.Clone();
            Reflector.SetIndexedMember(test, value, parameters);
            result = Reflector.GetIndexedMember(test, parameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, args), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, args), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", args), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, "1"), Res.ReflectionIndexerNotFound(test.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, args), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), args), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, "1"), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        #endregion

        #region Class field access

        [Test]
        public void ClassInstanceReadOnlyValueFieldAccess()
        {
            object test = new TestClass(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestClass.ReadOnlyValueField));
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => FieldAccessor.GetAccessor(fi).Set(test, value),
                TargetFramework.NetStandard20))
            {
                result = FieldAccessor.GetAccessor(fi).Get(test);
                Assert.AreEqual(value, result);
            }

            test = new TestClass(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestClass.ReadOnlyValueField), value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyValueField));
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassInstanceReadOnlyRefFieldAccess()
        {
            object test = new TestClass(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestClass.ReadOnlyReferenceField));
            object result, value = "dummy";

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => FieldAccessor.GetAccessor(fi).Set(test, value),
                TargetFramework.NetStandard20))
            {
                result = FieldAccessor.GetAccessor(fi).Get(test);
                Assert.AreEqual(value, result);
            }

            test = new TestClass(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestClass.ReadOnlyReferenceField), value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyReferenceField));
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassStaticFieldAccess()
        {
            Type testType = typeof(TestClass);
            FieldInfo fi = testType.GetField(nameof(TestClass.StaticIntField));
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
            Reflector.SetField(testType, nameof(TestClass.StaticIntField), value);
            result = Reflector.GetField(testType, nameof(TestClass.StaticIntField));
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Struct field access

        [Test]
        public void StructInstanceFieldAccess()
        {
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestStruct.IntField));
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => FieldAccessor.GetAccessor(fi).Set(test, value),
                TargetFramework.NetStandard20))
            {
                result = FieldAccessor.GetAccessor(fi).Get(test);
                Assert.AreEqual(value, result);
            }

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestStruct.IntField), value);
            result = Reflector.GetField(test, nameof(TestStruct.IntField));
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructInstanceReadOnlyValueFieldAccess()
        {
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestStruct.ReadOnlyValueField));
            object result, value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => FieldAccessor.GetAccessor(fi).Set(test, value),
                TargetFramework.NetStandard20))
            {
                result = FieldAccessor.GetAccessor(fi).Get(test);
                Assert.AreEqual(value, result);
            }

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyValueField), value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyValueField));
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructInstanceReadOnlyRefFieldAccess()
        {
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestStruct.ReadOnlyReferenceField));
            object result, value = "dummy";

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => FieldAccessor.GetAccessor(fi).Set(test, value),
                TargetFramework.NetStandard20))
            {
                result = FieldAccessor.GetAccessor(fi).Get(test);
                Assert.AreEqual(value, result);
            }

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyReferenceField), value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyReferenceField));
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructStaticFieldAccess()
        {
            Type testType = typeof(TestStruct);
            FieldInfo fi = testType.GetField(nameof(TestStruct.StaticIntField));
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
            Reflector.SetField(testType, nameof(TestStruct.StaticIntField), value);
            result = Reflector.GetField(testType, nameof(TestStruct.StaticIntField));
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Class construction

        [Test]
        public void ClassConstructionByType()
        {
            Type testType = typeof(TestClass);
            var accessor = CreateInstanceAccessor.GetAccessor(testType);

            Console.Write("System Activator...");
            TestClass result = (TestClass)Activator.CreateInstance(testType);
            Assert.AreEqual(1, result.IntProp);

            Console.Write("CreateInstanceAccessor...");
            result = (TestClass)accessor.CreateInstance();
            Assert.AreEqual(1, result.IntProp);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestClass>();
            Assert.AreEqual(1, result.IntProp);
            Throws<ArgumentException>(() => accessor.CreateInstance<TestStruct>(), Res.ReflectionCannotCreateInstanceGeneric(testType));
            Throws<ArgumentException>(() => accessor.CreateInstance<TestClass, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            result = (TestClass)Reflector.CreateInstance(testType);
            Assert.AreEqual(1, result.IntProp);
        }

        [Test]
        public void ClassConstructionByCtorInfo()
        {
            Type testType = typeof(TestClass);
            ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int) });
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            int arg = 1;
            object[] args = { arg };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestClass result = (TestClass)ci.Invoke(parameters);
            Assert.AreEqual(arg, result.IntProp);

            Console.Write("CreateInstanceAccessor...");
            parameters = (object[])args.Clone();
            result = (TestClass)accessor.CreateInstance(parameters);
            Assert.AreEqual(arg, result.IntProp);
            Throws<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.CreateInstance("x"), Res.ReflectionParametersInvalid);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestClass, int>(arg);
            Assert.AreEqual(arg, result.IntProp);
            Throws<ArgumentException>(() => accessor.CreateInstance<TestStruct, int>(arg), Res.ReflectionCannotCreateInstanceGeneric(testType));
            Throws<ArgumentException>(() => accessor.CreateInstance<TestClass, string>(null), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestClass)Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(arg, result.IntProp);
        }

        [Test]
        public void ClassComplexConstructionByCtorInfo()
        {
            Type testType = typeof(TestClass);
            ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType() });
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestClass result = (TestClass)ci.Invoke(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor...");
            parameters = (object[])args.Clone();
            result = (TestClass)CreateInstanceAccessor.GetAccessor(ci).CreateInstance(parameters);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], result.IntProp);
                Assert.AreNotEqual(args[2], parameters[2]);
            }

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestClass)Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        #endregion

        #region Struct construction

        [Test]
        public void StructConstructionByType()
        {
            Type testType = typeof(TestStruct);
            var accessor = CreateInstanceAccessor.GetAccessor(testType);

            Console.Write("System Activator...");
            object result = Activator.CreateInstance(testType);
            Assert.AreEqual(default(TestStruct), result);

            Console.Write("CreateInstanceAccessor...");
            result = accessor.CreateInstance();
            Assert.AreEqual(default(TestStruct), result);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestStruct>();
            Assert.AreEqual(default(TestStruct), result);
            Throws<ArgumentException>(() => accessor.CreateInstance<TestClass>(), Res.ReflectionCannotCreateInstanceGeneric(testType));
            Throws<ArgumentException>(() => accessor.CreateInstance<TestStruct, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            result = Reflector.CreateInstance(testType);
            Assert.AreEqual(default(TestStruct), result);
        }

        [Test]
        public void StructConstructionByCtorInfo()
        {
            Type testType = typeof(TestStruct);
            ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int) });
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            int arg = 1;
            object[] args = { arg };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestStruct result = (TestStruct)ci.Invoke(parameters);
            Assert.AreEqual(args[0], result.IntProp);

            Console.Write("CreateInstanceAccessor...");
            parameters = (object[])args.Clone();
            result = (TestStruct)accessor.CreateInstance(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Throws<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParametersInvalid);
            Throws<ArgumentException>(() => accessor.CreateInstance("x"), Res.ReflectionParametersInvalid);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestStruct, int>(arg);
            Assert.AreEqual(arg, result.IntProp);
            Throws<ArgumentException>(() => accessor.CreateInstance<TestClass, int>(arg), Res.ReflectionCannotCreateInstanceGeneric(testType));
            Throws<ArgumentException>(() => accessor.CreateInstance<TestStruct, string>(null), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestStruct)Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], result.IntProp);
        }

        [Test]
        public void StructComplexConstructionByCtorInfo()
        {
            Type testType = typeof(TestStruct);
            ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType() });
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestStruct result = (TestStruct)ci.Invoke(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor...");
            parameters = (object[])args.Clone();
            result = (TestStruct)CreateInstanceAccessor.GetAccessor(ci).CreateInstance(parameters);
            if (TestedFramework != TargetFramework.NetStandard20)
            {
                Assert.AreEqual(args[0], result.IntProp);
                Assert.AreNotEqual(args[2], parameters[2]); 
            }

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestStruct)Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructConstructionWithDefaultCtorByType()
        {
            Type testType = typeof(TestStructWithParameterlessCtor);

            Console.Write("System Activator...");
            var result = (TestStructWithParameterlessCtor)Activator.CreateInstance(testType);
            Assert.IsTrue(result.Initialized);

            Console.Write("System Activator for the 2nd time...");
            result = (TestStructWithParameterlessCtor)Activator.CreateInstance(testType);
            if (!result.Initialized)
                Console.WriteLine("Constructor was not invoked!");
#if !NETFRAMEWORK // Activator.CreateInstance does not execute the default struct constructor for the 2nd time
            Assert.IsTrue(result.Initialized);
#endif

            Console.Write("Type Descriptor...");
            result = (TestStructWithParameterlessCtor)TypeDescriptor.CreateInstance(null, testType, null, null);
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor...");
            result = (TestStructWithParameterlessCtor)CreateInstanceAccessor.GetAccessor(testType).CreateInstance();
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor Generic...");
            result = CreateInstanceAccessor.GetAccessor(testType).CreateInstance<TestStructWithParameterlessCtor>();
            Assert.IsTrue(result.Initialized);

            Console.Write("Reflector...");
            result = (TestStructWithParameterlessCtor)Reflector.CreateInstance(testType);
            Assert.IsTrue(result.Initialized);
        }

        [Test]
        public void StructConstructionWithDefaultCtorByCtorInfo()
        {
            Type testType = typeof(TestStructWithParameterlessCtor);
            ConstructorInfo ci = testType.GetConstructor(Type.EmptyTypes);

            Console.Write("System Reflection...");
            TestStructWithParameterlessCtor result = (TestStructWithParameterlessCtor)ci.Invoke(null);
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor...");
            result = (TestStructWithParameterlessCtor)CreateInstanceAccessor.GetAccessor(ci).CreateInstance();
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor Generic...");
            result = CreateInstanceAccessor.GetAccessor(ci).CreateInstance<TestStructWithParameterlessCtor>();
            Assert.IsTrue(result.Initialized);

            Console.Write("Reflector...");
            result = (TestStructWithParameterlessCtor)Reflector.CreateInstance(ci);
            Assert.IsTrue(result.Initialized);
        }

        #endregion

        #region MemberOf

        [Test]
        public void MemberOfTest()
        {
            MemberInfo methodIntParse = Reflector.MemberOf(() => int.Parse(null, null)); // MethodInfo: Int32.Parse(string, IFormatProvider)
            Assert.AreEqual(typeof(int).GetMethod(nameof(Int32.Parse), new[] { typeof(string), typeof(IFormatProvider) }), methodIntParse);

            MemberInfo ctorList = Reflector.MemberOf(() => new List<int>()); // ConstructorInfo: List<int>().ctor()
            Assert.AreEqual(typeof(List<int>).GetConstructor(Type.EmptyTypes), ctorList);

            MemberInfo fieldEmpty = Reflector.MemberOf(() => string.Empty); // FieldInfo: String.Empty
            Assert.AreEqual(typeof(string).GetField(nameof(String.Empty)), fieldEmpty);

            MemberInfo propertyLength = Reflector.MemberOf(() => default(string).Length); // PropertyInfo: string.Length
            Assert.AreEqual(typeof(string).GetProperty(nameof(String.Length)), propertyLength);

            MethodInfo methodAdd = Reflector.MemberOf(() => default(List<int>).Add(default(int))); // MethodInfo: List<int>.Add()
            Assert.AreEqual(typeof(List<int>).GetMethod(nameof(List<int>.Add)), methodAdd);
        }

        #endregion

        #region Partially trusted domain test

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical]
        public void ReflectionFromPartiallyTrustedDomain()
        {
            var domain = CreateSandboxDomain(
#if NET35
                new EnvironmentPermission(PermissionState.Unrestricted),
#endif
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess),
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.SerializationFormatter));
            var handle = Activator.CreateInstance(domain, Assembly.GetExecutingAssembly().FullName, typeof(Sandbox).FullName);
            var sandbox = (Sandbox)handle.Unwrap();
            try
            {
                sandbox.DoTest();
            }
            catch (SecurityException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
#endif

        #endregion
    }
}
