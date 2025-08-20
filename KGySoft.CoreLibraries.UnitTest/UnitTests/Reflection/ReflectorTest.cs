#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReflectorTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.IO;
using System.Reflection;
#if NETFRAMEWORK
using System.Security;
using System.Security.Permissions;
#endif

using KGySoft.Reflection;

using NUnit.Framework;

#endregion

#region Suppressions

#pragma warning disable 649 // Fields never assigned to, and will always have their default value - reflection access
// ReSharper disable AssignNullToNotNullAttribute - intended test cases
// ReSharper disable AccessToModifiedClosure
// ReSharper disable UnusedMember.Local - reflection access

#endregion

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
            public string StringField;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Static Properties

            public static int StaticIntProp { get; set; }
            public static ref int StaticRefProperty => ref StaticIntField;
            public static ref readonly int StaticRefReadonlyProperty => ref StaticIntField;

            #endregion

            #region Instance Properties

            public int IntProp { get; set; }
            public ref int RefIntProperty => ref IntField;
            public ref readonly int RefReadonlyProperty => ref ReadOnlyValueField;

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

            public ref string this[string i] => ref StringField;

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

            public void LongTestAction(int p1, string p2, long p3, char p4, decimal p5) { }
            public bool LongTestFunction(int p1, string p2, long p3, char p4, decimal p5) => true;

            public ref int TestRefFunction(int intValue)
            {
                Console.WriteLine($"{nameof(TestClass)}.{nameof(TestRefFunction)}({intValue}) invoked");
                IntField = intValue;
                return ref IntField;
            }

            #endregion

            #endregion
        }

        #endregion

        #region UnsafeTestClass class

        private unsafe class UnsafeTestClass
        {
            #region Fields

            #region Static Fields

            public static void* StaticField;

            #endregion

            #region Instance Fields

            public readonly void* ReadOnlyInstanceField;

            public void* InstanceField;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Static Properties

            public static void* StaticProperty { get; set; }
            public static ref void* StaticRefProperty => ref StaticField;
            public static ref readonly void* StaticRefReadonlyProperty => ref StaticField;

            #endregion

            #region Instance Properties

            public void* InstanceProperty { get; set; }
            public ref void* RefInstanceProperty => ref InstanceField;
            public ref readonly void* RefReadonlyProperty => ref ReadOnlyInstanceField;

            #endregion

            #endregion

            #region Indexers

            public void* this[int* i]
            {
                get
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerGetter[{(IntPtr)i}] invoked");
                    return InstanceField;
                }
                set
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerSetter[{(IntPtr)i}] = {(IntPtr)value} invoked");
                    InstanceField = value;
                }
            }

            public ref void* this[void* i] => ref InstanceField;

            #endregion

            #endregion

            #region Constructors

            public UnsafeTestClass()
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.Constructor() invoked");
                InstanceProperty = (void*)new IntPtr(1);
            }

            public UnsafeTestClass(void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.Constructor({(IntPtr)ptr}) invoked");
                InstanceProperty = ptr;
            }

            public UnsafeTestClass(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.Constructor({(IntPtr)ptr},{(IntPtr)intPtr},out int*,{(IntPtr)refPtr}) invoked");
                InstanceField = ptr;
                InstanceProperty = intPtr;
                outIntPtr = intPtr;
                refPtr = ptr;
            }

            #endregion

            #region Methods

            #region Static Methods

            public static void StaticTestAction(void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(StaticTestAction)}({(IntPtr)ptr}) invoked");
                StaticProperty = ptr;
            }

            public static void StaticComplexTestAction(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(StaticComplexTestAction)}({(IntPtr)ptr},out int*,{(IntPtr)intPtr},{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                StaticField = ptr;
                StaticProperty = intPtr;
            }

            public static int* StaticTestFunction(int* intPtr, void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(StaticTestFunction)}({(IntPtr)intPtr},{(IntPtr)ptr}) invoked");
                StaticProperty = ptr;
                return intPtr;
            }

            public static int* StaticComplexTestFunction(int* intPtr, void* ptr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(StaticComplexTestFunction)}({(IntPtr)intPtr},{(IntPtr)ptr},out int*,{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                StaticField = ptr;
                StaticProperty = intPtr;
                return intPtr;
            }

            #endregion

            #region Instance Methods

            public void TestAction(void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestAction)}({(IntPtr)ptr}) invoked");
                InstanceProperty = ptr;
            }

            public void ComplexTestAction(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(ComplexTestAction)}({(IntPtr)ptr},out int*,{(IntPtr)intPtr},{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                InstanceField = ptr;
                InstanceProperty = intPtr;
            }

            public int* TestFunction(int* intPtr, void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestFunction)}({(IntPtr)intPtr},{(IntPtr)ptr}) invoked");
                InstanceProperty = ptr;
                return intPtr;
            }

            public int* ComplexTestFunction(int* intPtr, void* ptr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(ComplexTestFunction)}({(IntPtr)intPtr},{(IntPtr)ptr},out int*,{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                InstanceField = ptr;
                InstanceProperty = intPtr;
                return intPtr;
            }

            public void LongTestAction(int* p1, float* p2, long* p3, char* p4, decimal* p5) { }
            public bool* LongTestFunction(int* p1, float* p2, long* p3, char* p4, decimal* p5) => (bool*)p1;

            public ref void* TestRefFunction(int* intPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestRefFunction)}({(IntPtr)intPtr}) invoked");
                InstanceField = intPtr;
                return ref InstanceField;
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
            public static string StaticStringField;

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
            public ref int RefIntProperty => ref StaticIntField; // returning IntField would cause CS8170
            public ref readonly int RefReadonlyProperty => ref StaticIntField; // returning IntField would cause CS8170

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

            public ref string this[string str] => ref StaticStringField;

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

        #region UnsafeTestStruct struct

        private unsafe struct UnsafeTestStruct
        {
            #region Fields

            #region Static Fields

            public static int* StaticField;

            #endregion

            #region Instance Fields

            public readonly void* ReadOnlyField;

            public int* InstanceField;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Static Properties

            public static int* StaticProperty { get; set; }

            #endregion

            #region Instance Properties

            public int* InstanceProperty { get; set; }
            public ref int* RefProperty => ref StaticField;
            public ref readonly int* RefReadonlyProperty => ref StaticField;

            #endregion

            #endregion

            #region Indexers

            public int* this[void* index]
            {
                get
                {
                    Console.WriteLine($"{nameof(UnsafeTestStruct)}.IndexerGetter[{(IntPtr)index}] invoked");
                    return InstanceProperty;
                }
                set
                {
                    Console.WriteLine($"{nameof(UnsafeTestStruct)}.IndexerSetter[{(IntPtr)index}] = {(IntPtr)value} invoked");
                    InstanceProperty = value;
                }
            }

            #endregion

            #endregion

            #region Constructors

            public UnsafeTestStruct(int* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.Constructor({(IntPtr)ptr}) invoked");
                InstanceField = ptr;
                ReadOnlyField = ptr;
                InstanceProperty = ptr;
            }

            public UnsafeTestStruct(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.Constructor({(IntPtr)ptr},{(IntPtr)intPtr},out int*,{(IntPtr)refPtr}) invoked");
                InstanceField = intPtr;
                ReadOnlyField = ptr;
                InstanceProperty = intPtr;
                outIntPtr = intPtr;
                refPtr = ptr;
            }

            #endregion

            #region Methods

            #region Static Methods

            public static void StaticTestAction(int* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(StaticTestAction)}({(IntPtr)ptr}) invoked");
                StaticProperty = ptr;
            }

            public static void StaticComplexTestAction(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(StaticComplexTestAction)}({(IntPtr)ptr},out int*,{(IntPtr)intPtr},{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                StaticField = (int*)ptr;
                StaticProperty = intPtr;
            }

            public static int* StaticTestFunction(int* intPtr, void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(StaticTestFunction)}({(IntPtr)intPtr},{(IntPtr)ptr}) invoked");
                StaticProperty = (int*)ptr;
                return intPtr;
            }

            public static int* StaticComplexTestFunction(int* intPtr, void* ptr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(StaticComplexTestFunction)}({(IntPtr)intPtr},{(IntPtr)ptr},out int*,{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                StaticField = (int*)ptr;
                StaticProperty = intPtr;
                return intPtr;
            }

            #endregion

            #region Instance Methods

            public void TestAction(void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(TestAction)}({(IntPtr)ptr}) invoked");
                InstanceProperty = (int*)ptr;
            }

            public void ComplexTestAction(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(ComplexTestAction)}({(IntPtr)ptr},out int*,{(IntPtr)intPtr},{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                InstanceField = (int*)ptr;
                InstanceProperty = intPtr;
            }

            public int* TestFunction(void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(TestFunction)}({(IntPtr)ptr}) invoked");
                InstanceProperty = (int*)ptr;
                return (int*)ptr;
            }

            public int* ComplexTestFunction(int* intPtr, void* ptr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestStruct)}.{nameof(ComplexTestFunction)}({(IntPtr)intPtr},{(IntPtr)ptr},out int*,{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                InstanceField = (int*)ptr;
                InstanceProperty = intPtr;
                return intPtr;
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg1 }), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg1, arg2);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.TestAction).ToLowerInvariant(), true, parameters);
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            MethodAccessor.GetAccessor(mi).Invoke(null, arg1, arg2);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
        }

        [Test]
        public void ClassInstanceComplexActionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.ComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestClass(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            accessor.InvokeInstanceAction(test, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], test.IntProp);

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

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassStaticComplexActionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticComplexTestAction));
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection.MethodInfo...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            TestClass.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).InvokeStaticAction((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);

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

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg1 }), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.TestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, test.IntProp);
        }

        [Test]
        public void ClassInstanceRefReturnFunctionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.TestRefFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg = 1;
            object[] args = [arg];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            object result = mi.Invoke(test, parameters);
#else
            object result = test.TestRefFunction(arg);
#endif
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, test.IntField);

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
#else
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, test.IntField);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
#endif

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, arg));
#else
            result = accessor.Invoke(test, arg);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, test.IntField);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Invoke(test, "1"), Res.NotAnInstanceOfType(typeof(int)));
#endif

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<TestClass, int, int>(test, arg));
#else
            result = accessor.InvokeInstanceFunction<TestClass, int, int>(test, arg);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, test.IntField);
            Throws<ArgumentNullException>(() => accessor.InvokeInstanceFunction<TestClass, int, int>(null, arg), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int>(test), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestRefFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, int>(test, arg, "x"), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestRefFunction), mi.DeclaringType));
#endif

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
#else
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, test.IntField);
#endif

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction), parameters));
#else
            result = Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction), parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, test.IntField);
#endif

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction).ToLowerInvariant(), true, parameters));
#else
            result = Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, test.IntField);
#endif
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestClass.StaticIntProp);
        }

        [Test]
        public void ClassInstanceComplexFunctionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.ComplexTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestClass(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.InvokeInstanceFunction<TestClass, int, string, bool, string, int>(test, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], test.IntProp);

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

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestFunction).ToLowerInvariant(), true, parameters);
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

#if NET8_0_OR_GREATER
            TestClass.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            result = MethodAccessor.GetAccessor(mi).InvokeStaticFunction<int, string, bool, string, int>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);

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

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestClass.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void LongActionMethodInvoke()
        {
            var test = new TestClass();
            var accessor = MethodAccessor.GetAccessor(typeof(TestClass).GetMethod(nameof(TestClass.LongTestAction))!);
            
            Assert.DoesNotThrow(() => accessor.Invoke(test, 1, "2", 3L, '4', 5m));
            Throws<ArgumentException>(() => accessor.Invoke(test, Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(5, 0));
            Throws<ArgumentException>(() => accessor.Invoke(test, 1), Res.ReflectionParamsLengthMismatch(5, 1));
            Throws<NotSupportedException>(() => accessor.InvokeInstanceAction(test, 1), Res.ReflectionMethodGenericNotSupported);
        }

        [Test]
        public void LongFunctionMethodInvoke()
        {
            var test = new TestClass();
            var accessor = MethodAccessor.GetAccessor(typeof(TestClass).GetMethod(nameof(TestClass.LongTestFunction))!);

            Assert.DoesNotThrow(() => accessor.Invoke(test, 1, "2", 3L, '4', 5m));
            Throws<ArgumentException>(() => accessor.Invoke(test, Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(5, 0));
            Throws<ArgumentException>(() => accessor.Invoke(test, 1), Res.ReflectionParamsLengthMismatch(5, 1));
            Throws<NotSupportedException>(() => accessor.InvokeInstanceFunction<TestClass, int, bool>(test, 1), Res.ReflectionMethodGenericNotSupported);
        }

        [Test]
        public void SpecialMethodInvoke()
        {
            // abstract method: pass
            MethodInfo mi = typeof(Stream).GetMethod(nameof(Stream.Read), new[] { typeof(byte[]), typeof(int), typeof(int) })!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            Assert.IsTrue(mi.IsAbstract);
            Assert.AreEqual(1, accessor.Invoke(new MemoryStream(new byte[1]), new object[] { new byte[1], 0, 1 }));
            Assert.AreEqual(1, accessor.Invoke(new MemoryStream(new byte[1]), new byte[1], 0, 1));
            Assert.AreEqual(1, accessor.InvokeInstanceFunction<Stream, byte[], int, int, int>(new MemoryStream(new byte[1]), new byte[1], 0, 1));

            // interface: pass
            mi = typeof(ICollection<int>).GetMethod(nameof(ICollection<_>.Contains))!;
            accessor = MethodAccessor.GetAccessor(mi);
            Assert.IsTrue(mi.DeclaringType!.IsInterface);
            Assert.IsFalse((bool)accessor.Invoke(Reflector.EmptyArray<int>(), new object[] { 42 })!);
            Assert.IsFalse((bool)accessor.Invoke(Reflector.EmptyArray<int>(), 42)!);
            Assert.IsFalse(accessor.InvokeInstanceFunction<int[], int, bool>(Reflector.EmptyArray<int>(), 42));

            // generic: fail
            mi = typeof(List<>).GetMethod(nameof(List<_>.Contains))!;
            accessor = MethodAccessor.GetAccessor(mi);
            Assert.IsTrue(mi.DeclaringType!.IsGenericTypeDefinition);
            Throws<InvalidOperationException>(() => accessor.Invoke(new List<int>(), new object[] { 42 }), Res.ReflectionGenericMember);
            Throws<InvalidOperationException>(() => accessor.Invoke(new List<int>(), 42), Res.ReflectionGenericMember);
            Throws<InvalidOperationException>(() => accessor.InvokeInstanceFunction<List<int>, int, bool>(new List<int>(), 42), Res.ReflectionGenericMember);
        }

        #endregion

        #region Class method invoke (unsafe)

        [Test]
        public unsafe void ClassInstanceSimpleActionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg = new IntPtr(1);
            object[] args = [arg];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(arg, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            Assert.AreEqual(arg, (IntPtr)test.InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, [null]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg);
            Assert.AreEqual(arg, (IntPtr)test.InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            accessor.InvokeInstanceAction(test, arg);
            Assert.AreEqual(arg, (IntPtr)test.InstanceProperty);
            Throws<ArgumentException>(() => accessor.InvokeInstanceAction(test, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestAction), mi.DeclaringType));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.TestAction), parameters);
            Assert.AreEqual(arg, (IntPtr)test.InstanceProperty);
        }

        [Test]
        public unsafe void ClassStaticSimpleActionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestClass.StaticTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg = new IntPtr(1);
            object[] args = [arg];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestClass.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            MethodAccessor.GetAccessor(mi).Invoke(null, arg);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestClass.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestClass.StaticProperty);
            Throws<ArgumentException>(() => accessor.InvokeStaticAction(1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.StaticTestAction), mi.DeclaringType));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestAction), parameters);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestClass.StaticProperty);
        }

        [Test]
        public unsafe void ClassInstanceComplexActionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.ComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestClass(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
#else
            accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]); 
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField); 
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceAction(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
#else
            accessor.InvokeInstanceAction(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
#else
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction), parameters));
#else
            Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction), parameters);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction).ToLowerInvariant(), true, parameters));
#else
            Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
        }

        [Test]
        public unsafe void ClassStaticComplexActionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestClass.StaticComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection.MethodInfo...");
            parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);

            UnsafeTestClass.StaticField = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticField = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
#else
            accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticField = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
#endif

            UnsafeTestClass.StaticField = null;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
#else
            accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
#endif

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
#else
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction), parameters));
#else
            Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction), parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction).ToLowerInvariant(), true, parameters));
#else
            Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
        }

        [Test]
        public unsafe void ClassInstanceSimpleFunctionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg1 = new IntPtr(1);
            var arg2 = new IntPtr(2);
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = (IntPtr)(Pointer.Unbox(mi.Invoke(test, parameters)));
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)test.InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, [1, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)test.InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, 1, arg2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, IntPtr>(test, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)test.InstanceProperty);
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, int, IntPtr, IntPtr>(test, 1, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, int>(test, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunction), mi.DeclaringType));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunction), parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)test.InstanceProperty);
        }

        [Test]
        public unsafe void ClassInstanceRefReturnFunctionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestRefFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            IntPtr arg = new IntPtr(1);
            object[] args = [arg];

            Console.Write("System Reflection...");
            object[] parameters;
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            parameters = (object[])args.Clone();
            object result = (IntPtr)Pointer.Unbox(mi.Invoke(test, parameters));
#else
            object result = (IntPtr)test.TestRefFunction((int*)arg);
#endif
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)test.InstanceField);

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
#else
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)test.InstanceField);
            Throws<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, arg));
#else
            result = accessor.Invoke(test, arg);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)test.InstanceField);
            Throws<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr>(test, arg));
#else
            result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr>(test, arg);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)test.InstanceField);
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, int, IntPtr>(test, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestRefFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, int>(test, arg), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestRefFunction), mi.DeclaringType));
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
#else
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)test.InstanceField);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestRefFunction), parameters));
#else
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestRefFunction), parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)test.InstanceField);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestRefFunction).ToLowerInvariant(), true, parameters));
#else
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestRefFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)test.InstanceField);
#endif
        }

        [Test]
        public unsafe void ClassStaticSimpleFunctionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestClass.StaticTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg1 = new IntPtr(1);
            var arg2 = new IntPtr(2);
            object[] args = [arg1, arg2];
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = (IntPtr)(Pointer.Unbox(mi.Invoke(null, parameters)));
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, [1, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1, 2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr>(arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<IntPtr, int, IntPtr>(arg1, 2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, int>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.StaticTestFunction), mi.DeclaringType));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestFunction), parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
        }

        [Test]
        public unsafe void ClassInstanceComplexFunctionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.ComplexTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;
            object result;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (IntPtr)Pointer.Unbox(mi.Invoke(test, parameters));
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestClass(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
#else
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
#else
            result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
#else
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction), parameters));
#else
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction).ToLowerInvariant(), true, parameters));
#else
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)test.InstanceProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
        }

        [Test]
        public unsafe void ClassStaticComplexFunctionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestClass.StaticComplexTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;
            object result;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (IntPtr)Pointer.Unbox(mi.Invoke(null, parameters));
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
#else
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
#endif

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)args[0], (IntPtr)args[1], default, (IntPtr)args[3]));
#else
            result = MethodAccessor.GetAccessor(mi).InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
#endif

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
#else
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction), parameters));
#else
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction).ToLowerInvariant(), true, parameters));
#else
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);

            Throws<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg1 }), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg1, arg2);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);

            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.TestAction).ToLowerInvariant(), true, parameters);
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(null, arg1, arg2);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
        }

        [Test]
        public void StructInstanceComplexActionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.ComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestStruct(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new TestStruct(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            accessor.InvokeInstanceAction(testStruct, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], testStruct.IntProp);

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

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexActionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticComplexTestAction))!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            TestStruct.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            accessor.InvokeStaticAction((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);

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

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, new object[] { arg1 }), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, ((TestStruct)test).IntProp);
            Throws<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.TestFunction).ToLowerInvariant(), true, parameters);
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
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { null, arg2 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, new object[] { arg2, arg1 }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);
            Throws<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

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

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg1, TestStruct.StaticIntProp);
        }

        [Test]
        public void StructInstanceComplexFunctionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.ComplexTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = { 1, "dummy", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestStruct(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new TestStruct(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.InvokeInstanceFunction<TestStruct, int, string, bool, string, int>(testStruct, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], testStruct.IntProp);

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

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], ((TestStruct)test).IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexFunctionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticComplexTestFunction))!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = { 1, "dummy", false, null };
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            TestStruct.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.InvokeStaticFunction<int, string, bool, string, int>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);

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

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], TestStruct.StaticIntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        #endregion

        #region Struct method invoke (unsafe)

        [Test]
        public unsafe void StructInstanceSimpleActionMethodInvokeUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestStruct.TestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;

            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceAction(unsafeTestStruct, arg), Res.ReflectionValueTypeWithPointersGenericNetStandard20);
#else
            accessor.InvokeInstanceAction(unsafeTestStruct, arg);
            Assert.AreEqual(arg, (IntPtr)unsafeTestStruct.InstanceProperty);
            Throws<ArgumentException>(() => accessor.InvokeInstanceAction(unsafeTestStruct, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.TestAction), mi.DeclaringType));
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestAction), parameters);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
        }

        [Test]
        public unsafe void StructStaticSimpleActionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestStruct.StaticTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;

            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(null, arg);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg);
            Assert.AreEqual(arg, (IntPtr)UnsafeTestStruct.StaticProperty);
            Throws<ArgumentException>(() => accessor.InvokeStaticAction(1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.StaticTestAction), mi.DeclaringType));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestAction), parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
        }

        [Test]
        public unsafe void StructInstanceComplexActionMethodInvokeUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestStruct.ComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            Assert.AreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestStruct(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
#else
            accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
#endif

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceAction(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
#else
            accessor.InvokeInstanceAction(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)unsafeTestStruct.InstanceField);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
#else
            Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction), parameters));
#else
            Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction), parameters);
            Assert.AreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction).ToLowerInvariant(), true, parameters));
#else
            Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
        }

        [Test]
        public unsafe void StructStaticComplexActionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestStruct.StaticComplexTestAction))!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);

            UnsafeTestStruct.StaticField = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticField = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
#else
            accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticField = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
#endif

            UnsafeTestStruct.StaticField = null;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
#else
            accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
#endif

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
#else
            Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction), parameters));
#else
            Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction), parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction).ToLowerInvariant(), true, parameters));
#else
            Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
        }

        [Test]
        public unsafe void StructInstanceSimpleFunctionMethodInvokeUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestStruct.TestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;

            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            object result = (IntPtr)Pointer.Unbox(mi.Invoke(test, parameters));
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            Throws<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr>(unsafeTestStruct, arg), Res.ReflectionValueTypeWithPointersGenericNetStandard20);
#else
            result = accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr>(unsafeTestStruct, arg);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)unsafeTestStruct.InstanceProperty);
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, int, IntPtr>(unsafeTestStruct, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.TestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, int>(unsafeTestStruct, arg), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.TestFunction), mi.DeclaringType));
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestFunction), parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg, result);
            Assert.AreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
        }

        [Test]
        public unsafe void StructStaticSimpleFunctionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestStruct.StaticTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg1 = new IntPtr(1);
            var arg2 = new IntPtr(2);
            object[] args = [arg1, arg2];
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = (IntPtr)(Pointer.Unbox(mi.Invoke(null, parameters)));
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, [1, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
            Throws<ArgumentException>(() => accessor.Invoke(null, 1, arg2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr>(arg1, arg2);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<int, IntPtr, IntPtr>(1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.StaticTestFunction), mi.DeclaringType));
            Throws<ArgumentException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, int>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.StaticTestFunction), mi.DeclaringType));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestFunction), parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(arg1, result);
            Assert.AreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
        }

        [Test]
        public unsafe void StructInstanceComplexFunctionMethodInvokeUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestStruct.ComplexTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;
            object result;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (IntPtr)Pointer.Unbox(mi.Invoke(test, parameters));
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestStruct(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
#else
            result = accessor.Invoke(test, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
#endif

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
#else
            result = accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)unsafeTestStruct.InstanceField);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
#else
            result = Reflector.InvokeMethod(test, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction), parameters));
#else
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction).ToLowerInvariant(), true, parameters));
#else
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
        }

        [Test]
        public unsafe void StructStaticComplexFunctionMethodInvokeUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestStruct.StaticComplexTestFunction))!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;
            object result;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (IntPtr)Pointer.Unbox(mi.Invoke(null, parameters));
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
#else
            result = accessor.Invoke(null, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
#else
            result = accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
#endif

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
#else
            result = accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
#endif

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
#else
            result = Reflector.InvokeMethod(null, mi, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction), parameters));
#else
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction), parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
#if NETCOREAPP2_0 && NETSTANDARD_TEST
            Throws<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction).ToLowerInvariant(), true, parameters));
#else
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
            Assert.AreEqual(args[0], result);
            Assert.AreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            Assert.AreNotEqual(args[2], parameters[2]);
#endif
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
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
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
            result = accessor.GetInstanceValue<TestClass, int>((TestClass)test);
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
            Reflector.SetProperty(test, nameof(TestClass.IntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestClass.IntProp).ToLowerInvariant(), true);
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

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public void ClassInstanceRefPropertyAccess()
        {
            // value property
            TestClass test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty(nameof(TestClass.RefIntProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, null);
#else
            test.RefIntProperty = value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(test, null);
#else
            result = ((TestClass)test).RefIntProperty;
#endif
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = PropertyAccessor.GetAccessor(pi).GetInstanceValue<TestClass, int>(test);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));

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
            Reflector.SetProperty(test, nameof(TestClass.RefIntProperty), value);
            result = Reflector.GetProperty(test, nameof(TestClass.RefIntProperty));
            Assert.AreEqual(value, result);
            Reflector.SetProperty(test, nameof(TestClass.RefIntProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestClass.RefIntProperty).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestClass.RefIntProperty), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestClass.RefIntProperty), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestClass.RefIntProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefIntProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestClass.RefIntProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestClass.RefIntProperty)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestClass.RefIntProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefIntProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
        }

        [Test]
        public void ClassInstanceRefReadonlyPropertyAccess()
        {
            // value property
            object test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty(nameof(TestClass.RefReadonlyProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, null);
#else
            typeof(TestClass).GetField(nameof(TestClass.ReadOnlyValueField))!.SetValue(test, value);
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(test, null);
#else
            result = ((TestClass)test).RefReadonlyProperty;
#endif
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testClass = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testClass, value);
            result = accessor.GetInstanceValue<TestClass, int>(testClass);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));

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
            Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty), value);
            result = Reflector.GetProperty(test, nameof(TestClass.RefReadonlyProperty));
            Assert.AreEqual(value, result);
            Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestClass.RefReadonlyProperty).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestClass.RefReadonlyProperty), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestClass.RefReadonlyProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefReadonlyProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestClass.RefReadonlyProperty)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestClass.RefReadonlyProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefReadonlyProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
        }
#endif

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
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntProp = 0;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntProp = 0;
            Console.Write("Property Accessor Generic...");
            accessor.SetStaticValue((int)value);
            result = accessor.GetStaticValue<int>();
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
            Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticIntProp).ToLowerInvariant(), true);
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

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public void ClassStaticRefPropertyAccess()
        {
            Type testType = typeof(TestClass);
            PropertyInfo pi = testType.GetProperty(nameof(TestClass.StaticRefProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1;

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(null, value, null);
#else
            TestClass.StaticRefProperty = 1;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(null, null);
#else
            result = TestClass.StaticRefProperty;
#endif
            Assert.AreEqual(value, result);

            TestClass.StaticRefProperty = 0;
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticRefProperty = 0;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticRefProperty = 0;
            Console.Write("Property Accessor Generic...");
            accessor.SetStaticValue((int)value);
            result = accessor.GetStaticValue<int>();
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefProperty), testType));
            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefProperty), testType));
            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefProperty), testType));
            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefProperty), testType));

            TestClass.StaticRefProperty = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticRefProperty = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty), value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefProperty));
            Assert.AreEqual(value, result);
            Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefProperty).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(TestClass.StaticRefProperty), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefProperty), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
            Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(TestClass.StaticRefProperty)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefProperty)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
        }

        [Test]
        public void ClassStaticRefReadonlyPropertyAccess()
        {
            Type testType = typeof(TestClass);
            PropertyInfo pi = testType.GetProperty(nameof(TestClass.StaticRefReadonlyProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1;

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(null, value, null);
#else
            TestClass.StaticIntField = 1;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(null, null);
#else
            result = TestClass.StaticRefReadonlyProperty;
#endif
            Assert.AreEqual(value, result);

            TestClass.StaticIntField = 0;
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntField = 0;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntField = 0;
            Console.Write("Property Accessor Generic...");
            accessor.SetStaticValue((int)value);
            result = accessor.GetStaticValue<int>();
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));
            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));
            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));
            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty), value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty));
            Assert.AreEqual(value, result);
            Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(TestClass.StaticRefReadonlyProperty), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefReadonlyProperty), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefReadonlyProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
            Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(TestClass.StaticRefReadonlyProperty)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefReadonlyProperty)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefReadonlyProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
        }
#endif

        [Test]
        public void ClassInstanceIndexerAccess()
        {
            var test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(int) });
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1, index = 42;
            object[] indexParameters = { index };

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = pi.GetValue(test, indexParameters);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, indexParameters);
            result = accessor.Get(test, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { 1, "2" }), "More parameters than needed are okay");
            Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => accessor.Get(test, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Assert.DoesNotThrow(() => accessor.Get(test, new object[] { 1, "2" }), "More parameters than needed are okay");

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value, index);
            result = accessor.Get(test, index);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null, index), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value, "1"), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Get(test, "1"), Res.NotAnInstanceOfType(typeof(int)));

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, (int)value, (int)index);
            result = accessor.GetInstanceValue<TestClass, int, int>(test, (int)index);
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
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            test = new TestClass(0);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public void ClassInstanceRefIndexerAccess()
        {
            var test = new TestClass();
            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(string) });
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            string index = "x";
            object[] indexParameters = { index };
            object result;
            string value = "alpha";

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, indexParameters);
#else
            test[index] = value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(test, indexParameters);
#else
            result = test[index];
#endif
            Assert.AreEqual(value, result);

            test = new TestClass();
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, indexParameters);
            result = accessor.Get(test, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
            Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { "1", 2 }), "More parameters than needed are okay");
            Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => accessor.Get(test, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
            Assert.DoesNotThrow(() => accessor.Get(test, new object[] { "1", 2 }), "More parameters than needed are okay");

            test = new TestClass();
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value, index);
            result = accessor.Get(test, index);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(string)));
            Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(string)));

            test = new TestClass();
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, value, index);
            result = accessor.GetInstanceValue<TestClass, string, string>(test, index);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, value, index), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<string>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, string, string>(null, index), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string, string>(new object(), index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestClass, string>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestClass, string, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new TestClass();
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));

            test = new TestClass();
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
        }
#endif

        #endregion

        #region Class property access (unsafe)

        [Test]
        public unsafe void ClassInstancePropertyAccessUNsafe()
        {
            throw null;
            //object test = new UnsafeTestClass(null);
            //PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestClass.IntProp));
            //PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            //object result, value = 1;

            //Console.Write("System Reflection...");
            //pi.SetValue(test, value, null);
            //result = pi.GetValue(test, null);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Property Accessor General...");
            //accessor.Set(test, value, Reflector.EmptyObjects);
            //result = accessor.Get(test, Reflector.EmptyObjects);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

            //test = new UnsafeTestClass(null);
            //Console.Write("Property Accessor NonGeneric...");
            //accessor.Set(test, value);
            //result = accessor.Get(test);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            //test = new UnsafeTestClass(null);
            //Console.Write("Property Accessor Generic...");
            //accessor.SetInstanceValue((UnsafeTestClass)test, (int)value);
            //result = accessor.GetInstanceValue<UnsafeTestClass, int>((UnsafeTestClass)test);
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestClass.IntProp), pi.DeclaringType!));
            //Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, 1), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.IntProp), pi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.IntProp), pi.DeclaringType!));
            //Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestClass.IntProp), pi.DeclaringType!));
            //Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.IntProp), pi.DeclaringType!));

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by PropertyInfo)...");
            //Reflector.SetProperty(test, pi, value);
            //result = Reflector.GetProperty(test, pi);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by name)...");
            //Reflector.SetProperty(test, nameof(UnsafeTestClass.IntProp), value);
            //result = Reflector.GetProperty(test, nameof(UnsafeTestClass.IntProp));
            //Assert.AreEqual(value, result);
            //Reflector.SetProperty(test, nameof(UnsafeTestClass.IntProp).ToLowerInvariant(), true, value);
            //result = Reflector.GetProperty(test, nameof(UnsafeTestClass.IntProp).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(UnsafeTestClass.IntProp), value), Res.ArgumentNull);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.IntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(UnsafeTestClass.IntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), typeof(object)));
            //Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(UnsafeTestClass)));
            //Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.IntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(UnsafeTestClass.IntProp)), Res.ArgumentNull);
            //Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(UnsafeTestClass.IntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), typeof(object)));
            //Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(UnsafeTestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(UnsafeTestClass)));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public unsafe void ClassInstanceRefPropertyAccessUnsafe()
        {
            throw null;
//            // value property
//            UnsafeTestClass test = new UnsafeTestClass(null);
//            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestClass.RefIntProperty));
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result;
//            int value = 1;

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(test, value, null);
//#else
//            test.RefIntProperty = value;
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(test, null);
//#else
//            result = ((UnsafeTestClass)test).RefIntProperty;
//#endif
//            Assert.AreEqual(value, result);

//            test = new UnsafeTestClass(null);
//            Console.Write("Property Accessor General...");
//            accessor.Set(test, value, Reflector.EmptyObjects);
//            result = accessor.Get(test, Reflector.EmptyObjects);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestClass(null);
//            Console.Write("Property Accessor NonGeneric...");
//            accessor.Set(test, value);
//            result = accessor.Get(test);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestClass(null);
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(test, value);
//            result = PropertyAccessor.GetAccessor(pi).GetInstanceValue<UnsafeTestClass, int>(test);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestClass.RefIntProperty), pi.DeclaringType!));
//            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, 1), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefIntProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefIntProperty), pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestClass.RefIntProperty), pi.DeclaringType!));
//            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefIntProperty), pi.DeclaringType!));

//            test = new UnsafeTestClass(null);
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(test, pi, value);
//            result = Reflector.GetProperty(test, pi);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestClass(null);
//            Console.Write("Reflector (by name)...");
//            Reflector.SetProperty(test, nameof(UnsafeTestClass.RefIntProperty), value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefIntProperty));
//            Assert.AreEqual(value, result);
//            Reflector.SetProperty(test, nameof(UnsafeTestClass.RefIntProperty).ToLowerInvariant(), true, value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefIntProperty).ToLowerInvariant(), true);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(UnsafeTestClass.RefIntProperty), value), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefIntProperty), null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(UnsafeTestClass.RefIntProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.RefIntProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(UnsafeTestClass)));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefIntProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(UnsafeTestClass.RefIntProperty)), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(UnsafeTestClass.RefIntProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.RefIntProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(UnsafeTestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(UnsafeTestClass)));
        }

        [Test]
        public unsafe void ClassInstanceRefReadonlyPropertyAccessUnsafe()
        {
            throw null;
//            // value property
//            object test = new UnsafeTestClass(null);
//            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestClass.RefReadonlyProperty));
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result;
//            int value = 1;

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(test, value, null);
//#else
//            typeof(UnsafeTestClass).GetField(nameof(UnsafeTestClass.ReadOnlyValueField))!.SetValue(test, value);
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(test, null);
//#else
//            result = ((UnsafeTestClass)test).RefReadonlyProperty;
//#endif
//            Assert.AreEqual(value, result);

//            test = new UnsafeTestClass(null);
//            Console.Write("Property Accessor General...");
//            accessor.Set(test, value, Reflector.EmptyObjects);
//            result = accessor.Get(test, Reflector.EmptyObjects);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestClass(null);
//            Console.Write("Property Accessor NonGeneric...");
//            accessor.Set(test, value);
//            result = accessor.Get(test);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

//            var UnsafeTestClass = new UnsafeTestClass(null);
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(UnsafeTestClass, value);
//            result = accessor.GetInstanceValue<UnsafeTestClass, int>(UnsafeTestClass);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestClass.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, 1), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestClass.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefReadonlyProperty), pi.DeclaringType!));

//            test = new UnsafeTestClass(null);
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(test, pi, value);
//            result = Reflector.GetProperty(test, pi);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestClass(null);
//            Console.Write("Reflector (by name)...");
//            Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty), value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty));
//            Assert.AreEqual(value, result);
//            Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty).ToLowerInvariant(), true, value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty).ToLowerInvariant(), true);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(UnsafeTestClass.RefReadonlyProperty), value), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(UnsafeTestClass.RefReadonlyProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.RefReadonlyProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(UnsafeTestClass)));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(UnsafeTestClass.RefReadonlyProperty)), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(UnsafeTestClass.RefReadonlyProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.RefReadonlyProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(UnsafeTestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(UnsafeTestClass)));
        }
#endif

        [Test]
        public unsafe void ClassStaticPropertyAccessUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestClass);
            //PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestClass.StaticIntProp));
            //PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            //object result, value = 1;

            //Console.Write("System Reflection...");
            //pi.SetValue(null, value, null);
            //result = pi.GetValue(null, null);
            //Assert.AreEqual(value, result);

            //UnsafeTestClass.StaticIntProp = 0;
            //Console.Write("Property Accessor General...");
            //accessor.Set(null, value, Reflector.EmptyObjects);
            //result = accessor.Get(null, Reflector.EmptyObjects);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestClass.StaticIntProp = 0;
            //Console.Write("Property Accessor NonGeneric...");
            //accessor.Set(null, value);
            //result = accessor.Get(null);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestClass.StaticIntProp = 0;
            //Console.Write("Property Accessor Generic...");
            //accessor.SetStaticValue((int)value);
            //result = accessor.GetStaticValue<int>();
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new UnsafeTestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestClass.StaticIntProp), testType));
            //Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticIntProp), testType));
            //Throws<InvalidOperationException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(new UnsafeTestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestClass.StaticIntProp), testType));
            //Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticIntProp), testType));

            //UnsafeTestClass.StaticIntProp = 0;
            //Console.Write("Reflector (by PropertyInfo)...");
            //Reflector.SetProperty(null, pi, value);
            //result = Reflector.GetProperty(null, pi);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestClass.StaticIntProp = 0;
            //Console.Write("Reflector (by name)...");
            //Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticIntProp), value);
            //result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticIntProp));
            //Assert.AreEqual(value, result);
            //Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticIntProp).ToLowerInvariant(), true, value);
            //result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticIntProp).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(UnsafeTestClass.StaticIntProp), value), Res.ArgumentNull);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticIntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(UnsafeTestClass.StaticIntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(object)));
            //Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), testType));
            //Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticIntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(UnsafeTestClass.StaticIntProp)), Res.ArgumentNull);
            //Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(UnsafeTestClass.StaticIntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.StaticIntProp), typeof(object)));
            //Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(UnsafeTestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), testType));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public unsafe void ClassStaticRefPropertyAccessUnsafe()
        {
            throw null;
//            Type testType = typeof(UnsafeTestClass);
//            PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestClass.StaticRefProperty));
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result, value = 1;

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(null, value, null);
//#else
//            UnsafeTestClass.StaticRefProperty = 1;
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(null, null);
//#else
//            result = UnsafeTestClass.StaticRefProperty;
//#endif
//            Assert.AreEqual(value, result);

//            UnsafeTestClass.StaticRefProperty = 0;
//            Console.Write("Property Accessor General...");
//            accessor.Set(null, value, Reflector.EmptyObjects);
//            result = accessor.Get(null, Reflector.EmptyObjects);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

//            UnsafeTestClass.StaticRefProperty = 0;
//            Console.Write("Property Accessor NonGeneric...");
//            accessor.Set(null, value);
//            result = accessor.Get(null);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

//            UnsafeTestClass.StaticRefProperty = 0;
//            Console.Write("Property Accessor Generic...");
//            accessor.SetStaticValue((int)value);
//            result = accessor.GetStaticValue<int>();
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new UnsafeTestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestClass.StaticRefProperty), testType));
//            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefProperty), testType));
//            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(new UnsafeTestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestClass.StaticRefProperty), testType));
//            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefProperty), testType));

//            UnsafeTestClass.StaticRefProperty = 0;
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(null, pi, value);
//            result = Reflector.GetProperty(null, pi);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

//            UnsafeTestClass.StaticRefProperty = 0;
//            Console.Write("Reflector (by name)...");
//            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty), value);
//            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty));
//            Assert.AreEqual(value, result);
//            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty).ToLowerInvariant(), true, value);
//            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty).ToLowerInvariant(), true);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(UnsafeTestClass.StaticRefProperty), value), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty), null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(UnsafeTestClass.StaticRefProperty), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.StaticRefProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), testType));
//            Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(UnsafeTestClass.StaticRefProperty)), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(UnsafeTestClass.StaticRefProperty)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.StaticRefProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(UnsafeTestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), testType));
        }

        [Test]
        public unsafe void ClassStaticRefReadonlyPropertyAccessUnsafe()
        {
            throw null;
//            Type testType = typeof(UnsafeTestClass);
//            PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestClass.StaticRefReadonlyProperty));
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result, value = 1;

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(null, value, null);
//#else
//            UnsafeTestClass.StaticIntField = 1;
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(null, null);
//#else
//            result = UnsafeTestClass.StaticRefReadonlyProperty;
//#endif
//            Assert.AreEqual(value, result);

//            UnsafeTestClass.StaticIntField = 0;
//            Console.Write("Property Accessor General...");
//            accessor.Set(null, value, Reflector.EmptyObjects);
//            result = accessor.Get(null, Reflector.EmptyObjects);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

//            UnsafeTestClass.StaticIntField = 0;
//            Console.Write("Property Accessor NonGeneric...");
//            accessor.Set(null, value);
//            result = accessor.Get(null);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

//            UnsafeTestClass.StaticIntField = 0;
//            Console.Write("Property Accessor Generic...");
//            accessor.SetStaticValue((int)value);
//            result = accessor.GetStaticValue<int>();
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new UnsafeTestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestClass.StaticRefReadonlyProperty), testType));
//            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefReadonlyProperty), testType));
//            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(new UnsafeTestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestClass.StaticRefReadonlyProperty), testType));
//            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefReadonlyProperty), testType));

//            UnsafeTestClass.StaticIntField = 0;
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(null, pi, value);
//            result = Reflector.GetProperty(null, pi);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

//            UnsafeTestClass.StaticIntField = 0;
//            Console.Write("Reflector (by name)...");
//            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty), value);
//            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty));
//            Assert.AreEqual(value, result);
//            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true, value);
//            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(UnsafeTestClass.StaticRefReadonlyProperty), value), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(UnsafeTestClass.StaticRefReadonlyProperty), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.StaticRefReadonlyProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), testType));
//            Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(UnsafeTestClass.StaticRefReadonlyProperty)), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(UnsafeTestClass.StaticRefReadonlyProperty)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.StaticRefReadonlyProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(UnsafeTestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestClass.IntProp), testType));
        }
#endif

        [Test]
        public unsafe void ClassInstanceIndexerAccessUnsafe()
        {
            throw null;
            //var test = new UnsafeTestClass(null);
            //PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(int) });
            //PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            //object result, value = 1, index = 42;
            //object[] indexParameters = { index };

            //Console.Write("System Reflection...");
            //pi.SetValue(test, value, indexParameters);
            //result = pi.GetValue(test, indexParameters);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Property Accessor General...");
            //accessor.Set(test, value, indexParameters);
            //result = accessor.Get(test, indexParameters);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => accessor.Set(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            //Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            //Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { 1, "2" }), "More parameters than needed are okay");
            //Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            //Throws<ArgumentException>(() => accessor.Get(test, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            //Assert.DoesNotThrow(() => accessor.Get(test, new object[] { 1, "2" }), "More parameters than needed are okay");

            //test = new UnsafeTestClass(null);
            //Console.Write("Property Accessor NonGeneric...");
            //accessor.Set(test, value, index);
            //result = accessor.Get(test, index);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => accessor.Set(test, null, index), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            //Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, value, "1"), Res.NotAnInstanceOfType(typeof(int)));
            //Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            //Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Get(test, "1"), Res.NotAnInstanceOfType(typeof(int)));

            //test = new UnsafeTestClass(null);
            //Console.Write("Property Accessor Generic...");
            //accessor.SetInstanceValue(test, (int)value, (int)index);
            //result = accessor.GetInstanceValue<UnsafeTestClass, int, int>(test, (int)index);
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            //Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, 1, 1), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1", 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            //Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            //Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, int, int>(null, 1), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int, int>(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int, string>(test, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by PropertyInfo)...");
            //Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            //result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            //Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            //Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            //Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by parameters match)...");
            //Reflector.SetIndexedMember(test, value, indexParameters);
            //result = Reflector.GetIndexedMember(test, indexParameters);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            //Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            //Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            //Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            //Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
            //Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
            //Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            //Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            //Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            //Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public unsafe void ClassInstanceRefIndexerAccessUnsafe()
        {
            throw null;
//            var test = new UnsafeTestClass();
//            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(string) });
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            string index = "x";
//            object[] indexParameters = { index };
//            object result;
//            string value = "alpha";

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(test, value, indexParameters);
//#else
//            test[index] = value;
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(test, indexParameters);
//#else
//            result = test[index];
//#endif
//            Assert.AreEqual(value, result);

//            test = new UnsafeTestClass();
//            Console.Write("Property Accessor General...");
//            accessor.Set(test, value, indexParameters);
//            result = accessor.Get(test, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
//            Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { "1", 2 }), "More parameters than needed are okay");
//            Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => accessor.Get(test, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
//            Assert.DoesNotThrow(() => accessor.Get(test, new object[] { "1", 2 }), "More parameters than needed are okay");

//            test = new UnsafeTestClass();
//            Console.Write("Property Accessor NonGeneric...");
//            accessor.Set(test, value, index);
//            result = accessor.Get(test, index);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(string)));
//            Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
//            Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(string)));

//            test = new UnsafeTestClass();
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(test, value, index);
//            result = accessor.GetInstanceValue<UnsafeTestClass, string, string>(test, index);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
//            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, value, index), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<string>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, string, string>(null, index), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string, string>(new object(), index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, string>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, string, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

//            test = new UnsafeTestClass();
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
//            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));

//            test = new UnsafeTestClass();
//            Console.Write("Reflector (by parameters match)...");
//            Reflector.SetIndexedMember(test, value, indexParameters);
//            result = Reflector.GetIndexedMember(test, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
//            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
//            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
//            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
//            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
        }
#endif

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
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestStruct(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

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
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
#endif
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(TestStruct.IntProp), value);
            result = Reflector.GetProperty(test, nameof(TestStruct.IntProp));
            Assert.AreEqual(value, result);
            Reflector.SetProperty(test, nameof(TestStruct.IntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestStruct.IntProp).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestStruct.IntProp), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestStruct.IntProp)), Res.ArgumentNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestStruct.IntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestStruct.IntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.IntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestStruct.IntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
#endif
            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestStruct.IntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.IntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public void StructInstanceRefPropertyAccess()
        {
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty(nameof(TestStruct.RefIntProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, null);
#else
            ((TestStruct)test).RefIntProperty = 1;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(test, null);
#else
            result = ((TestStruct)test).RefIntProperty;
#endif
            Assert.AreEqual(value, result);

            ((TestStruct)test).RefIntProperty = 0;
            test = new TestStruct(0);
            Console.Write("Property Accessor General...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects),
                TargetFramework.NetStandard20))
            {
                result = accessor.Get(test, Reflector.EmptyObjects);
                Assert.AreEqual(value, result);
                Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            }

            ((TestStruct)test).RefIntProperty = 0;
            test = new TestStruct(0);
            Console.Write("Property Accessor NonGeneric...");
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

            ((TestStruct)test).RefIntProperty = 0;
            var testStruct = new TestStruct(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, value);
            result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));

            ((TestStruct)test).RefIntProperty = 1;
            test = new TestStruct(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
#endif
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            ((TestStruct)test).RefIntProperty = 1;
            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty), value);
            result = Reflector.GetProperty(test, nameof(TestStruct.RefIntProperty));
            Assert.AreEqual(value, result);
            Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestStruct.RefIntProperty).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestStruct.RefIntProperty), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestStruct.RefIntProperty)), Res.ArgumentNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestStruct.RefIntProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefIntProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
#endif
            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestStruct.RefIntProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefIntProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
        }

        [Test]
        public void StructInstanceRefReadonlyPropertyAccess()
        {
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty(nameof(TestStruct.RefReadonlyProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1;

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, null);
#else
            typeof(TestStruct).GetField(nameof(TestStruct.StaticIntField))!.SetValue(null, value);
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(test, null);
#else
            result = ((TestStruct)test).RefReadonlyProperty;
#endif
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            Console.Write("Property Accessor General...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects),
                TargetFramework.NetStandard20))
            {
                result = accessor.Get(test, Reflector.EmptyObjects);
                Assert.AreEqual(value, result);
                Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            }

            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            Console.Write("Property Accessor NonGeneric...");
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
            TestStruct.StaticIntField = 0;
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, (int)value);
            result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));

            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
#endif
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), value);
            result = Reflector.GetProperty(test, nameof(TestStruct.RefReadonlyProperty));
            Assert.AreEqual(value, result);
            Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestStruct.RefReadonlyProperty).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestStruct.RefReadonlyProperty), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestStruct.RefReadonlyProperty)), Res.ArgumentNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestStruct.RefReadonlyProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefReadonlyProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
#endif
            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestStruct.RefReadonlyProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefReadonlyProperty), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
        }
#endif

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
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntProp = 0;
            Console.Write("Property Accessor NonGeneric...");
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
            Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(TestStruct.StaticIntProp).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(TestStruct.StaticIntProp), value), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestStruct.StaticIntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestStruct.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.IntProp), testType));
            Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(TestStruct.StaticIntProp)), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestStruct.StaticIntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(object)));
            Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestStruct.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.IntProp), testType));
        }

        [Test]
        public void StructInstanceIndexerAccess()
        {
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(int) });
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1, index = 42;
            object[] indexParameters = { index };

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = pi.GetValue(test, indexParameters);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            accessor.Set(test, value, indexParameters);
            Console.Write("Property Accessor General...");
            result = accessor.Get(test, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentNullException>(() => accessor.Set(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            ThrowsOnFramework<ArgumentException>(() => accessor.Set(test, value, new object[] { 1, "2" }), Res.ReflectionIndexerParamsLengthMismatch(1, 2),
                TargetFramework.NetStandard20); // On other platforms more parameters are accepted
            Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => accessor.Get(test, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            ThrowsOnFramework<ArgumentException>(() => accessor.Get(test, new object[] { 1, "2" }), Res.ReflectionIndexerParamsLengthMismatch(1, 2),
                TargetFramework.NetStandard20); // On other platforms more parameters are accepted

            test = new TestStruct(0);
            accessor.Set(test, value, index);
            Console.Write("Property Accessor NonGeneric...");
            result = accessor.Get(test, index);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentNullException>(() => accessor.Set(test, null, index), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, value, "1"), Res.NotAnInstanceOfType(typeof(int)));
            Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Get(test, "1"), Res.NotAnInstanceOfType(typeof(int)));

            var testStruct = new TestStruct(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, (int)value, (int)index);
            result = accessor.GetInstanceValue<TestStruct, int, int>(testStruct, (int)index);
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
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));
#endif
            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            test = new TestStruct(0);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
#endif
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public void StructInstanceRefIndexerAccess()
        {
            object test = new TestStruct();
            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(string) })!;
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            string index = "x";
            object[] indexParameters = { index };
            string value = "alpha";

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, indexParameters);
#else
            ((TestStruct)test)[index] = value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = pi.GetValue(test, indexParameters);
#else
            result = ((TestStruct)test)[index];
#endif
            Assert.AreEqual(value, result);

            test = new TestStruct();
            TestStruct.StaticStringField = default;
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters),
                TargetFramework.NetStandard20))
            {
                Console.Write("Property Accessor General...");
                result = accessor.Get(test, indexParameters);
                Assert.AreEqual(value, result);
                Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
                Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { "1", 2 }), "More parameters than needed are okay");
                Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
                Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                Throws<ArgumentException>(() => accessor.Get(test, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { "1", 2 }), "More parameters than needed are okay");
            }

            test = new TestStruct(0);
            TestStruct.StaticStringField = default;
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, index),
                    TargetFramework.NetStandard20))
            {
                Console.Write("Property Accessor NonGeneric...");
                result = accessor.Get(test, index);
                Assert.AreEqual(value, result);
                Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
                Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
                Throws<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(string)));
                Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
                Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
                Throws<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(string)));
            }

            var testStruct = new TestStruct();
            TestStruct.StaticStringField = default;
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, value, index);
            result = accessor.GetInstanceValue<TestStruct, string, string>(testStruct, (string)index);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, value), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string, string>(new object(), index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, string>(testStruct), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, string, int>(testStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new TestStruct();
            Console.Write("Reflector (by PropertyInfo)...");
            TestStruct.StaticStringField = default;
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
#endif
            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));

            test = new TestStruct();
            TestStruct.StaticStringField = default;
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
#endif
            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
        }
#endif
#endregion

        #region Struct property access (unsafe)

        [Test]
        public unsafe void StructInstancePropertyAccessUnsafe()
        {
            throw null;
//            object test = new UnsafeTestStruct(0);
//            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestStruct.IntProp));
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result, value = 1;

//            Console.Write("System Reflection...");
//            pi.SetValue(test, value, null);
//            result = pi.GetValue(test, null);
//            Assert.AreEqual(value, result);

//            test = new UnsafeTestStruct(0);
//            Console.Write("Property Accessor General...");
//            accessor.Set(test, value, Reflector.EmptyObjects);
//            result = accessor.Get(test, Reflector.EmptyObjects);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
//                Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestStruct(0);
//            Console.Write("Property Accessor NonGeneric...");
//            accessor.Set(test, value);
//            result = accessor.Get(test);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
//            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
//                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

//            var UnsafeTestStruct = new UnsafeTestStruct(0);
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(UnsafeTestStruct, (int)value);
//            result = accessor.GetInstanceValue<UnsafeTestStruct, int>(UnsafeTestStruct);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.IntProp), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.IntProp), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.IntProp), pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.IntProp), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.IntProp), pi.DeclaringType!));

//            test = new UnsafeTestStruct(0);
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(test, pi, value);
//            result = Reflector.GetProperty(test, pi);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
//#endif
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestStruct(0);
//            Console.Write("Reflector (by name)...");
//            Reflector.SetProperty(test, nameof(UnsafeTestStruct.IntProp), value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.IntProp));
//            Assert.AreEqual(value, result);
//            Reflector.SetProperty(test, nameof(UnsafeTestStruct.IntProp).ToLowerInvariant(), true, value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.IntProp).ToLowerInvariant(), true);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(UnsafeTestStruct.IntProp), value), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(UnsafeTestStruct.IntProp)), Res.ArgumentNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.IntProp), null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(UnsafeTestStruct.IntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.IntProp), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(UnsafeTestStruct)));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.IntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
//#endif
//            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(UnsafeTestStruct.IntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.IntProp), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(UnsafeTestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(UnsafeTestStruct)));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public unsafe void StructInstanceRefPropertyAccessUnsafe()
        {
            throw null;
//            object test = new UnsafeTestStruct(0);
//            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestStruct.RefIntProperty));
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result;
//            int value = 1;

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(test, value, null);
//#else
//            ((UnsafeTestStruct)test).RefIntProperty = 1;
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(test, null);
//#else
//            result = ((UnsafeTestStruct)test).RefIntProperty;
//#endif
//            Assert.AreEqual(value, result);

//            ((UnsafeTestStruct)test).RefIntProperty = 0;
//            test = new UnsafeTestStruct(0);
//            Console.Write("Property Accessor General...");
//            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects),
//                TargetFramework.NetStandard20))
//            {
//                result = accessor.Get(test, Reflector.EmptyObjects);
//                Assert.AreEqual(value, result);
//                Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
//            }

//            ((UnsafeTestStruct)test).RefIntProperty = 0;
//            test = new UnsafeTestStruct(0);
//            Console.Write("Property Accessor NonGeneric...");
//            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value),
//                TargetFramework.NetStandard20))
//            {
//                result = accessor.Get(test);
//                Assert.AreEqual(value, result);
//                Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
//            }

//            ((UnsafeTestStruct)test).RefIntProperty = 0;
//            var UnsafeTestStruct = new UnsafeTestStruct(0);
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(UnsafeTestStruct, value);
//            result = accessor.GetInstanceValue<UnsafeTestStruct, int>(UnsafeTestStruct);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.RefIntProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefIntProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefIntProperty), pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.RefIntProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefIntProperty), pi.DeclaringType!));

//            ((UnsafeTestStruct)test).RefIntProperty = 1;
//            test = new UnsafeTestStruct(0);
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(test, pi, value);
//            result = Reflector.GetProperty(test, pi);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
//#endif
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

//            ((UnsafeTestStruct)test).RefIntProperty = 1;
//            test = new UnsafeTestStruct(0);
//            Console.Write("Reflector (by name)...");
//            Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefIntProperty), value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefIntProperty));
//            Assert.AreEqual(value, result);
//            Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefIntProperty).ToLowerInvariant(), true, value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefIntProperty).ToLowerInvariant(), true);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(UnsafeTestStruct.RefIntProperty), value), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(UnsafeTestStruct.RefIntProperty)), Res.ArgumentNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefIntProperty), null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(UnsafeTestStruct.RefIntProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.RefIntProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(UnsafeTestStruct)));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefIntProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
//#endif
//            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(UnsafeTestStruct.RefIntProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.RefIntProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(UnsafeTestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(UnsafeTestStruct)));
        }

        [Test]
        public void StructInstanceRefReadonlyPropertyAccessUnsafe()
        {
            throw null;
//            object test = new UnsafeTestStruct(0);
//            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestStruct.RefReadonlyProperty));
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result, value = 1;

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(test, value, null);
//#else
//            typeof(UnsafeTestStruct).GetField(nameof(UnsafeTestStruct.StaticIntField))!.SetValue(null, value);
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(test, null);
//#else
//            result = ((UnsafeTestStruct)test).RefReadonlyProperty;
//#endif
//            Assert.AreEqual(value, result);

//            test = new UnsafeTestStruct(0);
//            UnsafeTestStruct.StaticIntField = 0;
//            Console.Write("Property Accessor General...");
//            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects),
//                TargetFramework.NetStandard20))
//            {
//                result = accessor.Get(test, Reflector.EmptyObjects);
//                Assert.AreEqual(value, result);
//                Throws<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
//            }

//            test = new UnsafeTestStruct(0);
//            UnsafeTestStruct.StaticIntField = 0;
//            Console.Write("Property Accessor NonGeneric...");
//            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value),
//                TargetFramework.NetStandard20))
//            {
//                result = accessor.Get(test);
//                Assert.AreEqual(value, result);
//                Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
//            }

//            var UnsafeTestStruct = new UnsafeTestStruct(0);
//            UnsafeTestStruct.StaticIntField = 0;
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(UnsafeTestStruct, (int)value);
//            result = accessor.GetInstanceValue<UnsafeTestStruct, int>(UnsafeTestStruct);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));

//            test = new UnsafeTestStruct(0);
//            UnsafeTestStruct.StaticIntField = 0;
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(test, pi, value);
//            result = Reflector.GetProperty(test, pi);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
//#endif
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

//            test = new UnsafeTestStruct(0);
//            UnsafeTestStruct.StaticIntField = 0;
//            Console.Write("Reflector (by name)...");
//            Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty), value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty));
//            Assert.AreEqual(value, result);
//            Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty).ToLowerInvariant(), true, value);
//            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty).ToLowerInvariant(), true);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(UnsafeTestStruct.RefReadonlyProperty), value), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(UnsafeTestStruct.RefReadonlyProperty)), Res.ArgumentNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(UnsafeTestStruct.RefReadonlyProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.RefReadonlyProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(UnsafeTestStruct)));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
//#endif
//            Throws<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(UnsafeTestStruct.RefReadonlyProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.RefReadonlyProperty), typeof(object)));
//            Throws<ReflectionException>(() => Reflector.GetProperty(test, nameof(UnsafeTestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(UnsafeTestStruct)));
        }
#endif

        [Test]
        public unsafe void StructStaticPropertyAccessUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestStruct);
            //PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestStruct.StaticIntProp));
            //PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            //object result, value = 1;

            //Console.Write("System Reflection...");
            //pi.SetValue(null, value, null);
            //result = pi.GetValue(null, null);
            //Assert.AreEqual(value, result);

            //UnsafeTestStruct.StaticIntProp = 0;
            //Console.Write("Property Accessor General...");
            //accessor.Set(null, value, Reflector.EmptyObjects);
            //result = accessor.Get(null, Reflector.EmptyObjects);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestStruct.StaticIntProp = 0;
            //Console.Write("Property Accessor NonGeneric...");
            //accessor.Set(null, value);
            //result = accessor.Get(null);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestStruct.StaticIntProp = 0;
            //var UnsafeTestStruct = new UnsafeTestStruct();
            //Console.Write("Property Accessor Generic...");
            //PropertyAccessor.GetAccessor(pi).SetStaticValue((int)value);
            //result = PropertyAccessor.GetAccessor(pi).GetStaticValue<int>();
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetInstanceValue(UnsafeTestStruct, value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestStruct.StaticIntProp), testType));
            //Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.StaticIntProp), testType));
            //Throws<InvalidOperationException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(UnsafeTestStruct), Res.ReflectionInstancePropertyExpectedGeneric(nameof(UnsafeTestStruct.StaticIntProp), testType));
            //Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.StaticIntProp), testType));

            //UnsafeTestStruct.StaticIntProp = 0;
            //Console.Write("Reflector (by PropertyInfo)...");
            //Reflector.SetProperty(null, pi, value);
            //result = Reflector.GetProperty(null, pi);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestStruct.StaticIntProp = 0;
            //Console.Write("Reflector (by name)...");
            //Reflector.SetProperty(testType, nameof(UnsafeTestStruct.StaticIntProp), value);
            //result = Reflector.GetProperty(testType, nameof(UnsafeTestStruct.StaticIntProp));
            //Assert.AreEqual(value, result);
            //Reflector.SetProperty(testType, nameof(UnsafeTestStruct.StaticIntProp).ToLowerInvariant(), true, value);
            //result = Reflector.GetProperty(testType, nameof(UnsafeTestStruct.StaticIntProp).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(UnsafeTestStruct.StaticIntProp), value), Res.ArgumentNull);
            //Throws<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestStruct.StaticIntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(UnsafeTestStruct.StaticIntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(object)));
            //Throws<ReflectionException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestStruct.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestStruct.IntProp), testType));
            //Throws<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestStruct.StaticIntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(UnsafeTestStruct.StaticIntProp)), Res.ArgumentNull);
            //Throws<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(UnsafeTestStruct.StaticIntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestStruct.StaticIntProp), typeof(object)));
            //Throws<ReflectionException>(() => Reflector.GetProperty(testType, nameof(UnsafeTestStruct.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(UnsafeTestStruct.IntProp), testType));
        }

        [Test]
        public unsafe void StructInstanceIndexerAccessUnsafe()
        {
            throw null;
//            object test = new UnsafeTestStruct(0);
//            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(int) });
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result, value = 1, index = 42;
//            object[] indexParameters = { index };

//            Console.Write("System Reflection...");
//            pi.SetValue(test, value, indexParameters);
//            result = pi.GetValue(test, indexParameters);
//            Assert.AreEqual(value, result);

//            test = new UnsafeTestStruct(0);
//            accessor.Set(test, value, indexParameters);
//            Console.Write("Property Accessor General...");
//            result = accessor.Get(test, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
//            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
//                Throws<ArgumentNullException>(() => accessor.Set(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
//            ThrowsOnFramework<ArgumentException>(() => accessor.Set(test, value, new object[] { 1, "2" }), Res.ReflectionIndexerParamsLengthMismatch(1, 2),
//                TargetFramework.NetStandard20); // On other platforms more parameters are accepted
//            Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => accessor.Get(test, new object[] { "1" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));
//            ThrowsOnFramework<ArgumentException>(() => accessor.Get(test, new object[] { 1, "2" }), Res.ReflectionIndexerParamsLengthMismatch(1, 2),
//                TargetFramework.NetStandard20); // On other platforms more parameters are accepted

//            test = new UnsafeTestStruct(0);
//            accessor.Set(test, value, index);
//            Console.Write("Property Accessor NonGeneric...");
//            result = accessor.Get(test, index);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
//            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
//                Throws<ArgumentNullException>(() => accessor.Set(test, null, index), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
//            Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => accessor.Set(test, value, "1"), Res.NotAnInstanceOfType(typeof(int)));
//            Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
//            Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => accessor.Get(test, "1"), Res.NotAnInstanceOfType(typeof(int)));

//            var UnsafeTestStruct = new UnsafeTestStruct(0);
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(UnsafeTestStruct, (int)value, (int)index);
//            result = accessor.GetInstanceValue<UnsafeTestStruct, int, int>(UnsafeTestStruct, (int)index);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, "1", 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, 1, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int, int>(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(UnsafeTestStruct), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int, string>(UnsafeTestStruct, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

//            test = new UnsafeTestStruct(0);
//            Console.Write("Reflector (by PropertyInfo)...");
//            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
//            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));
//#endif
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));

//            test = new UnsafeTestStruct(0);
//            Console.Write("Reflector (by parameters match)...");
//            Reflector.SetIndexedMember(test, value, indexParameters);
//            result = Reflector.GetIndexedMember(test, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
//            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
//            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
//#endif
//            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
//            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
//            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
        }

#if !(NETCOREAPP2_0 && NETSTANDARD_TEST)
        [Test]
        public unsafe void StructInstanceRefIndexerAccessUnsafe()
        {
            throw null;
//            object test = new UnsafeTestStruct();
//            PropertyInfo pi = test.GetType().GetProperty("Item", new[] { typeof(string) })!;
//            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
//            object result;
//            string index = "x";
//            object[] indexParameters = { index };
//            string value = "alpha";

//            Console.Write("System Reflection...");
//#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
//            pi.SetValue(test, value, indexParameters);
//#else
//            ((UnsafeTestStruct)test)[index] = value;
//#endif
//#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
//            result = pi.GetValue(test, indexParameters);
//#else
//            result = ((UnsafeTestStruct)test)[index];
//#endif
//            Assert.AreEqual(value, result);

//            test = new UnsafeTestStruct();
//            UnsafeTestStruct.StaticStringField = default;
//            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters),
//                TargetFramework.NetStandard20))
//            {
//                Console.Write("Property Accessor General...");
//                result = accessor.Get(test, indexParameters);
//                Assert.AreEqual(value, result);
//                Throws<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
//                Throws<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
//                Throws<ArgumentException>(() => accessor.Set(test, value, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
//                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { "1", 2 }), "More parameters than needed are okay");
//                Throws<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
//                Throws<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
//                Throws<ArgumentException>(() => accessor.Get(test, new object[] { 1 }), Res.ElementNotAnInstanceOfType(0, typeof(string)));
//                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { "1", 2 }), "More parameters than needed are okay");
//            }

//            test = new UnsafeTestStruct(0);
//            UnsafeTestStruct.StaticStringField = default;
//            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.Set(test, value, index),
//                    TargetFramework.NetStandard20))
//            {
//                Console.Write("Property Accessor NonGeneric...");
//                result = accessor.Get(test, index);
//                Assert.AreEqual(value, result);
//                Throws<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
//                Throws<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
//                Throws<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(string)));
//                Throws<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
//                Throws<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
//                Throws<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
//                Throws<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(string)));
//            }

//            var UnsafeTestStruct = new UnsafeTestStruct();
//            UnsafeTestStruct.StaticStringField = default;
//            Console.Write("Property Accessor Generic...");
//            accessor.SetInstanceValue(UnsafeTestStruct, value, index);
//            result = accessor.GetInstanceValue<UnsafeTestStruct, string, string>(UnsafeTestStruct, (string)index);
//            Assert.AreEqual(value, result);
//            Throws<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, value), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string, string>(new object(), index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, string>(UnsafeTestStruct), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
//            Throws<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, string, int>(UnsafeTestStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

//            test = new UnsafeTestStruct();
//            Console.Write("Reflector (by PropertyInfo)...");
//            UnsafeTestStruct.StaticStringField = default;
//            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
//            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
//#endif
//            Throws<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
//            Throws<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
//            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
//            Throws<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));

//            test = new UnsafeTestStruct();
//            UnsafeTestStruct.StaticStringField = default;
//            Console.Write("Reflector (by parameters match)...");
//            Reflector.SetIndexedMember(test, value, indexParameters);
//            result = Reflector.GetIndexedMember(test, indexParameters);
//            Assert.AreEqual(value, result);
//            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
//            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
//#if !(NETSTANDARD_TEST && NETCOREAPP2_0) // For value types system reflection is used to set properties in .NET Standard 2.0 that provides different errors
//            Throws<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
//            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
//            Throws<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
//            Throws<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
//#endif
//            Throws<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
//            Throws<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
//            Throws<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
//            Throws<ReflectionException>(() => Reflector.GetIndexedMember(test, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
        }
#endif
#endregion

        #region Class field access

        [Test]
        public void ClassInstanceFieldAccess()
        {
            var test = new TestClass(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestClass.IntField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = accessor.GetInstanceValue<TestClass, int>(test);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.IntField), fi.DeclaringType!));

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
            Reflector.SetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassInstanceReadOnlyValueFieldAccess()
        {
            var test = new TestClass(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestClass.ReadOnlyValueField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Field Accessor Generic...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(TestClass.ReadOnlyValueField), typeof(TestClass)),
                    TargetFramework.NetStandard20))
            {
                result = accessor.GetInstanceValue<TestClass, int>(test);
                Assert.AreEqual(value, result);
                Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
                Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
                Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
                Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
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
            Reflector.SetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassInstanceReadOnlyReferenceFieldAccess()
        {
            var test = new TestClass(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestClass.ReadOnlyReferenceField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            string value = "dummy";

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = FieldAccessor.GetAccessor(fi).Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Field Accessor Generic...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(TestClass.ReadOnlyReferenceField), typeof(TestClass)),
                TargetFramework.NetStandard20))
            {
                result = accessor.GetInstanceValue<TestClass, string>(test);
                Assert.AreEqual(value, result);
                Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, value), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, string>(null), Res.ArgumentNull);
                Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
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
            Reflector.SetField(test, nameof(TestClass.ReadOnlyReferenceField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyReferenceField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void ClassStaticFieldAccess()
        {
            Type testType = typeof(TestClass);
            FieldInfo fi = testType.GetField(nameof(TestClass.StaticIntField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(null, value);
            result = fi.GetValue(null);
            Assert.AreEqual(value, result);

            TestClass.StaticIntField = 0;
            Console.Write("Field Accessor...");
            accessor.Set(null, value);
            result = FieldAccessor.GetAccessor(fi).Get(null);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntField = 0;
            Console.Write("Field Accessor Generic...");
            accessor.SetStaticValue(value);
            result = accessor.GetStaticValue<int>();
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestClass.StaticIntField), testType));
            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.StaticIntField), testType));
            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestClass.StaticIntField), testType));
            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.StaticIntField), testType));

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            Assert.AreEqual(value, result);

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, nameof(TestClass.StaticIntField), value);
            result = Reflector.GetField(testType, nameof(TestClass.StaticIntField));
            Assert.AreEqual(value, result);
            Reflector.SetField(testType, nameof(TestClass.StaticIntField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(testType, nameof(TestClass.StaticIntField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Class field access (unsafe)

        [Test]
        public unsafe void ClassInstanceFieldAccessUnsafe()
        {
            throw null;
            //var test = new UnsafeTestClass(null);
            //FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestClass.IntField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //int value = 1;

            //Console.Write("System Reflection...");
            //fi.SetValue(test, value);
            //result = fi.GetValue(test);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Field Accessor...");
            //accessor.Set(test, value);
            //result = accessor.Get(test);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            //test = new UnsafeTestClass(null);
            //Console.Write("Field Accessor Generic...");
            //accessor.SetInstanceValue(test, value);
            //result = accessor.GetInstanceValue<UnsafeTestClass, int>(test);
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestClass.IntField), fi.DeclaringType!));
            //Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, 1), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.IntField), fi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.IntField), fi.DeclaringType!));
            //Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestClass.IntField), fi.DeclaringType!));
            //Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.IntField), fi.DeclaringType!));

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(test, fi, value);
            //result = Reflector.GetField(test, fi);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyValueField), value);
            //result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyValueField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyValueField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyValueField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
        }

        [Test]
        public unsafe void ClassInstanceReadOnlyValueFieldAccessUnsafe()
        {
            throw null;
            //var test = new UnsafeTestClass(null);
            //FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestClass.ReadOnlyValueField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //int value = 1;

            //Console.Write("System Reflection...");
            //fi.SetValue(test, value);
            //result = fi.GetValue(test);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Field Accessor...");
            //accessor.Set(test, value);
            //result = accessor.Get(test);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            //if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
            //    Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            //test = new UnsafeTestClass(null);
            //Console.Write("Field Accessor Generic...");
            //if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(UnsafeTestClass.ReadOnlyValueField), typeof(UnsafeTestClass)),
            //        TargetFramework.NetStandard20))
            //{
            //    result = accessor.GetInstanceValue<UnsafeTestClass, int>(test);
            //    Assert.AreEqual(value, result);
            //    Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestClass.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, 1), Res.ArgumentNull);
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestClass.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(null), Res.ArgumentNull);
            //    Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyValueField), fi.DeclaringType!));
            //}

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(test, fi, value);
            //result = Reflector.GetField(test, fi);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyValueField), value);
            //result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyValueField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyValueField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyValueField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
        }

        [Test]
        public unsafe void ClassInstanceReadOnlyReferenceFieldAccessUnsafe()
        {
            throw null;
            //var test = new UnsafeTestClass(null);
            //FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestClass.ReadOnlyReferenceField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //string value = "dummy";

            //Console.Write("System Reflection...");
            //fi.SetValue(test, value);
            //result = fi.GetValue(test);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Field Accessor...");
            //accessor.Set(test, value);
            //result = FieldAccessor.GetAccessor(fi).Get(test);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            //test = new UnsafeTestClass(null);
            //Console.Write("Field Accessor Generic...");
            //if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(UnsafeTestClass.ReadOnlyReferenceField), typeof(UnsafeTestClass)),
            //    TargetFramework.NetStandard20))
            //{
            //    result = accessor.GetInstanceValue<UnsafeTestClass, string>(test);
            //    Assert.AreEqual(value, result);
            //    Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<ArgumentNullException>(() => accessor.SetInstanceValue((UnsafeTestClass)null, value), Res.ArgumentNull);
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<ArgumentNullException>(() => accessor.GetInstanceValue<UnsafeTestClass, string>(null), Res.ArgumentNull);
            //    Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            //}

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(test, fi, value);
            //result = Reflector.GetField(test, fi);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestClass(null);
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyReferenceField), value);
            //result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyReferenceField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyReferenceField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyReferenceField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
        }

        [Test]
        public unsafe void ClassStaticFieldAccessUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestClass);
            //FieldInfo fi = testType.GetField(nameof(UnsafeTestClass.StaticIntField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //int value = 1;

            //Console.Write("System Reflection...");
            //fi.SetValue(null, value);
            //result = fi.GetValue(null);
            //Assert.AreEqual(value, result);

            //UnsafeTestClass.StaticIntField = 0;
            //Console.Write("Field Accessor...");
            //accessor.Set(null, value);
            //result = FieldAccessor.GetAccessor(fi).Get(null);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestClass.StaticIntField = 0;
            //Console.Write("Field Accessor Generic...");
            //accessor.SetStaticValue(value);
            //result = accessor.GetStaticValue<int>();
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new UnsafeTestClass(), value), Res.ReflectionInstanceFieldExpectedGeneric(nameof(UnsafeTestClass.StaticIntField), testType));
            //Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.StaticIntField), testType));
            //Throws<InvalidOperationException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(new UnsafeTestClass()), Res.ReflectionInstanceFieldExpectedGeneric(nameof(UnsafeTestClass.StaticIntField), testType));
            //Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.StaticIntField), testType));

            //UnsafeTestClass.StaticIntField = 0;
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(null, fi, value);
            //result = Reflector.GetField(null, fi);
            //Assert.AreEqual(value, result);

            //UnsafeTestClass.StaticIntField = 0;
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(testType, nameof(UnsafeTestClass.StaticIntField), value);
            //result = Reflector.GetField(testType, nameof(UnsafeTestClass.StaticIntField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(testType, nameof(UnsafeTestClass.StaticIntField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(testType, nameof(UnsafeTestClass.StaticIntField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
        }

        #endregion

        #region Struct field access

        [Test]
        public void StructInstanceFieldAccess()
        {
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestStruct.IntField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testStruct = new TestStruct(0);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(testStruct, value);
            result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));

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
            Reflector.SetField(test, nameof(TestStruct.IntField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestStruct.IntField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructInstanceReadOnlyValueFieldAccess()
        {
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestStruct.ReadOnlyValueField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
                Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testStruct = new TestStruct(0);
            Console.Write("Field Accessor Generic...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testStruct, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(TestStruct.ReadOnlyValueField), typeof(TestStruct)),
                TargetFramework.NetStandard20))
            {
                result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
                Assert.AreEqual(value, result);
                Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
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
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyValueField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyValueField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructInstanceReadOnlyReferenceFieldAccess()
        {
            object test = new TestStruct(0);
            FieldInfo fi = test.GetType().GetField(nameof(TestStruct.ReadOnlyReferenceField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            string value = "dummy";

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = fi.GetValue(test);
            Assert.AreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            Throws<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testStruct = new TestStruct(0);
            Console.Write("Field Accessor Generic...");
            if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testStruct, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(TestStruct.ReadOnlyReferenceField), typeof(TestStruct)),
                TargetFramework.NetStandard20))
            {
                result = accessor.GetInstanceValue<TestStruct, string>(testStruct);
                Assert.AreEqual(value, result);
                Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<InvalidOperationException>(() => accessor.GetStaticValue<string>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
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
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyReferenceField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyReferenceField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        [Test]
        public void StructStaticFieldAccess()
        {
            Type testType = typeof(TestStruct);
            FieldInfo fi = testType.GetField(nameof(TestStruct.StaticIntField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            int value = 1;

            Console.Write("System Reflection...");
            fi.SetValue(null, value);
            result = fi.GetValue(null);
            Assert.AreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Field Accessor...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            Assert.AreEqual(value, result);
            Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntField = 0;
            Console.Write("Field Accessor Generic...");
            accessor.SetStaticValue(value);
            result = accessor.GetStaticValue<int>();
            Assert.AreEqual(value, result);
            Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new TestStruct(), value), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestStruct.StaticIntField), testType));
            Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.StaticIntField), testType));
            Throws<InvalidOperationException>(() => accessor.GetInstanceValue<TestStruct, int>(new TestStruct()), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestStruct.StaticIntField), testType));
            Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.StaticIntField), testType));

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
            Reflector.SetField(testType, nameof(TestStruct.StaticIntField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(testType, nameof(TestStruct.StaticIntField).ToLowerInvariant(), true);
            Assert.AreEqual(value, result);
        }

        #endregion

        #region Struct field access (unsafe)

        [Test]
        public unsafe void StructInstanceFieldAccessUnsafe()
        {
            throw null;
            //object test = new UnsafeTestStruct(0);
            //FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestStruct.IntField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //int value = 1;

            //Console.Write("System Reflection...");
            //fi.SetValue(test, value);
            //result = fi.GetValue(test);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestStruct(0);
            //Console.Write("Field Accessor...");
            //accessor.Set(test, value);
            //result = accessor.Get(test);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            //if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
            //    Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            //var UnsafeTestStruct = new UnsafeTestStruct(0);
            //Console.Write("Field Accessor Generic...");
            //accessor.SetInstanceValue(UnsafeTestStruct, value);
            //result = accessor.GetInstanceValue<UnsafeTestStruct, int>(UnsafeTestStruct);
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestStruct.IntField), fi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.IntField), fi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.IntField), fi.DeclaringType!));
            //Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestStruct.IntField), fi.DeclaringType!));
            //Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.IntField), fi.DeclaringType!));

            //test = new UnsafeTestStruct(0);
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(test, fi, value);
            //result = Reflector.GetField(test, fi);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestStruct(0);
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(test, nameof(UnsafeTestStruct.IntField), value);
            //result = Reflector.GetField(test, nameof(UnsafeTestStruct.IntField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(test, nameof(UnsafeTestStruct.IntField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(test, nameof(UnsafeTestStruct.IntField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
        }

        [Test]
        public unsafe void StructInstanceReadOnlyValueFieldAccessUnsafe()
        {
            throw null;
            //object test = new UnsafeTestStruct(0);
            //FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestStruct.ReadOnlyValueField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //int value = 1;

            //Console.Write("System Reflection...");
            //fi.SetValue(test, value);
            //result = fi.GetValue(test);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestStruct(0);
            //Console.Write("Field Accessor...");
            //accessor.Set(test, value);
            //result = accessor.Get(test);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            //if (TestedFramework != TargetFramework.NetStandard20) // the fallback reflection accepts null as int
            //    Throws<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            //var UnsafeTestStruct = new UnsafeTestStruct(0);
            //Console.Write("Field Accessor Generic...");
            //if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(UnsafeTestStruct, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(UnsafeTestStruct.ReadOnlyValueField), typeof(UnsafeTestStruct)),
            //    TargetFramework.NetStandard20))
            //{
            //    result = accessor.GetInstanceValue<UnsafeTestStruct, int>(UnsafeTestStruct);
            //    Assert.AreEqual(value, result);
            //    Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestStruct.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestStruct.ReadOnlyValueField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyValueField), fi.DeclaringType!));
            //}

            //test = new UnsafeTestStruct(0);
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(test, fi, value);
            //result = Reflector.GetField(test, fi);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestStruct(0);
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(test, nameof(UnsafeTestStruct.ReadOnlyValueField), value);
            //result = Reflector.GetField(test, nameof(UnsafeTestStruct.ReadOnlyValueField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(test, nameof(UnsafeTestStruct.ReadOnlyValueField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(test, nameof(UnsafeTestStruct.ReadOnlyValueField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
        }

        [Test]
        public unsafe void StructInstanceReadOnlyReferenceFieldAccessUnsafe()
        {
            throw null;
            //object test = new UnsafeTestStruct(0);
            //FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestStruct.ReadOnlyReferenceField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //string value = "dummy";

            //Console.Write("System Reflection...");
            //fi.SetValue(test, value);
            //result = fi.GetValue(test);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestStruct(0);
            //Console.Write("Field Accessor...");
            //accessor.Set(test, value);
            //result = accessor.Get(test);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            //Throws<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            //var UnsafeTestStruct = new UnsafeTestStruct(0);
            //Console.Write("Field Accessor Generic...");
            //if (!ThrowsOnFramework<PlatformNotSupportedException>(() => accessor.SetInstanceValue(UnsafeTestStruct, value), Res.ReflectionSetReadOnlyFieldGenericNetStandard20(nameof(UnsafeTestStruct.ReadOnlyReferenceField), typeof(UnsafeTestStruct)),
            //    TargetFramework.NetStandard20))
            //{
            //    result = accessor.GetInstanceValue<UnsafeTestStruct, string>(UnsafeTestStruct);
            //    Assert.AreEqual(value, result);
            //    Throws<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.SetInstanceValue(UnsafeTestStruct, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<InvalidOperationException>(() => accessor.GetStaticValue<string>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(UnsafeTestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
            //    Throws<ArgumentException>(() => accessor.GetInstanceValue<object, string>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
            //}

            //test = new UnsafeTestStruct(0);
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(test, fi, value);
            //result = Reflector.GetField(test, fi);
            //Assert.AreEqual(value, result);

            //test = new UnsafeTestStruct(0);
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(test, nameof(UnsafeTestStruct.ReadOnlyReferenceField), value);
            //result = Reflector.GetField(test, nameof(UnsafeTestStruct.ReadOnlyReferenceField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(test, nameof(UnsafeTestStruct.ReadOnlyReferenceField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(test, nameof(UnsafeTestStruct.ReadOnlyReferenceField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
        }

        [Test]
        public unsafe void StructStaticFieldAccessUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestStruct);
            //FieldInfo fi = testType.GetField(nameof(UnsafeTestStruct.StaticIntField));
            //FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            //object result;
            //int value = 1;

            //Console.Write("System Reflection...");
            //fi.SetValue(null, value);
            //result = fi.GetValue(null);
            //Assert.AreEqual(value, result);

            //UnsafeTestStruct.StaticIntProp = 0;
            //Console.Write("Field Accessor...");
            //accessor.Set(null, value);
            //result = accessor.Get(null);
            //Assert.AreEqual(value, result);
            //Throws<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            //Throws<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            //UnsafeTestStruct.StaticIntField = 0;
            //Console.Write("Field Accessor Generic...");
            //accessor.SetStaticValue(value);
            //result = accessor.GetStaticValue<int>();
            //Assert.AreEqual(value, result);
            //Throws<InvalidOperationException>(() => accessor.SetInstanceValue(new UnsafeTestStruct(), value), Res.ReflectionInstanceFieldExpectedGeneric(nameof(UnsafeTestStruct.StaticIntField), testType));
            //Throws<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.StaticIntField), testType));
            //Throws<InvalidOperationException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(new UnsafeTestStruct()), Res.ReflectionInstanceFieldExpectedGeneric(nameof(UnsafeTestStruct.StaticIntField), testType));
            //Throws<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.StaticIntField), testType));

            //UnsafeTestStruct.StaticIntProp = 0;
            //Console.Write("Reflector (by FieldInfo)...");
            //Reflector.SetField(null, fi, value);
            //result = Reflector.GetField(null, fi);
            //Assert.AreEqual(value, result);

            //UnsafeTestStruct.StaticIntProp = 0;
            //Console.Write("Reflector (by name)...");
            //Reflector.SetField(testType, nameof(UnsafeTestStruct.StaticIntField), value);
            //result = Reflector.GetField(testType, nameof(UnsafeTestStruct.StaticIntField));
            //Assert.AreEqual(value, result);
            //Reflector.SetField(testType, nameof(UnsafeTestStruct.StaticIntField).ToLowerInvariant(), true, value);
            //result = Reflector.GetField(testType, nameof(UnsafeTestStruct.StaticIntField).ToLowerInvariant(), true);
            //Assert.AreEqual(value, result);
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

            Console.Write("CreateInstanceAccessor General...");
            result = (TestClass)accessor.CreateInstance(Reflector.EmptyObjects);
            Assert.AreEqual(1, result.IntProp);

            Console.Write("CreateInstanceAccessor NonGeneric...");
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

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestClass)accessor.CreateInstance(parameters);
            Assert.AreEqual(arg, result.IntProp);
            Throws<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.CreateInstance(Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.CreateInstance(new object[] { "x" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (TestClass)accessor.CreateInstance(arg);
            Assert.AreEqual(arg, result.IntProp);
            Throws<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.CreateInstance("x"), Res.NotAnInstanceOfType(typeof(int)));

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
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestClass result = (TestClass)ci.Invoke(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestClass)accessor.CreateInstance(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = (TestClass)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result.IntProp);

            Console.Write("CreateInstanceAccessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.CreateInstance<TestClass, int, string, bool, string>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], result.IntProp);

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestClass)Reflector.CreateInstance(ci, parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void InvalidTypeConstructionByType()
        {
            // abstract class
            Type testType = typeof(Type);
            var accessor = CreateInstanceAccessor.GetAccessor(testType);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // interface
            testType = typeof(IComparable);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance<IComparable>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // static type
            testType = typeof(Res);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // generic type definition
            testType = typeof(List<>);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance<object>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // no parameterless constructor
            testType = typeof(string);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionNoDefaultCtor(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionNoDefaultCtor(testType));
            Throws<InvalidOperationException>(() => accessor.CreateInstance<string>(), Res.ReflectionNoDefaultCtor(testType));
        }

        [Test]
        public void InvalidTypeConstructionByCtorInfo()
        {
            // abstract class
            ConstructorInfo ci = typeof(Type).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
            var accessor = CreateInstanceAccessor.GetAccessor(ci);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(typeof(Type)));
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(typeof(Type)));
            Throws<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(typeof(Type)));

            // generic type definition
            ci = typeof(List<>).GetConstructor(Type.EmptyTypes)!;
            accessor = CreateInstanceAccessor.GetAccessor(ci);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(typeof(List<>)));
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(typeof(List<>)));
            Throws<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(typeof(List<>)));

            // static constructor
            ci = typeof(Res).GetConstructor(BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null)!;
            accessor = CreateInstanceAccessor.GetAccessor(ci);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionInstanceCtorExpected);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionInstanceCtorExpected);
            Throws<InvalidOperationException>(() => accessor.CreateInstance<object>(), Res.ReflectionInstanceCtorExpected);

            // module constructor
            if (EnvironmentHelper.IsMono)
                return;
            ci = ((Type)Reflector.GetProperty(typeof(Module).Module, "RuntimeType"))!.GetConstructor(BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null)!;
            accessor = CreateInstanceAccessor.GetAccessor(ci);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionInstanceCtorExpected);
            Throws<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionInstanceCtorExpected);
            Throws<InvalidOperationException>(() => accessor.CreateInstance<object>(), Res.ReflectionInstanceCtorExpected);
        }

        #endregion

        #region Class construction (unsafe)

        [Test]
        public unsafe void ClassConstructionByTypeUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestClass);
            //var accessor = CreateInstanceAccessor.GetAccessor(testType);

            //Console.Write("System Activator...");
            //UnsafeTestClass result = (UnsafeTestClass)Activator.CreateInstance(testType);
            //Assert.AreEqual(1, result.IntProp);

            //Console.Write("CreateInstanceAccessor General...");
            //result = (UnsafeTestClass)accessor.CreateInstance(Reflector.EmptyObjects);
            //Assert.AreEqual(1, result.IntProp);

            //Console.Write("CreateInstanceAccessor NonGeneric...");
            //result = (UnsafeTestClass)accessor.CreateInstance();
            //Assert.AreEqual(1, result.IntProp);

            //Console.Write("CreateInstanceAccessor Generic...");
            //result = accessor.CreateInstance<UnsafeTestClass>();
            //Assert.AreEqual(1, result.IntProp);
            //Throws<ArgumentException>(() => accessor.CreateInstance<TestStruct>(), Res.ReflectionCannotCreateInstanceGeneric(testType));
            //Throws<ArgumentException>(() => accessor.CreateInstance<UnsafeTestClass, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            //Console.Write("Reflector...");
            //result = (UnsafeTestClass)Reflector.CreateInstance(testType);
            //Assert.AreEqual(1, result.IntProp);
        }

        [Test]
        public unsafe void ClassConstructionByCtorInfoUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestClass);
            //ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int) });
            //CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            //int arg = 1;
            //object[] args = { arg };

            //Console.Write("System Reflection...");
            //object[] parameters = (object[])args.Clone();
            //UnsafeTestClass result = (UnsafeTestClass)ci.Invoke(parameters);
            //Assert.AreEqual(arg, result.IntProp);

            //Console.Write("CreateInstanceAccessor General...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestClass)accessor.CreateInstance(parameters);
            //Assert.AreEqual(arg, result.IntProp);
            //Throws<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.CreateInstance(Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(1, 0));
            //Throws<ArgumentException>(() => accessor.CreateInstance(new object[] { "x" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            //Console.Write("CreateInstanceAccessor NonGeneric...");
            //result = (UnsafeTestClass)accessor.CreateInstance(arg);
            //Assert.AreEqual(arg, result.IntProp);
            //Throws<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParamsLengthMismatch(1, 0));
            //Throws<ArgumentException>(() => accessor.CreateInstance("x"), Res.NotAnInstanceOfType(typeof(int)));

            //Console.Write("CreateInstanceAccessor Generic...");
            //result = accessor.CreateInstance<UnsafeTestClass, int>(arg);
            //Assert.AreEqual(arg, result.IntProp);
            //Throws<ArgumentException>(() => accessor.CreateInstance<TestStruct, int>(arg), Res.ReflectionCannotCreateInstanceGeneric(testType));
            //Throws<ArgumentException>(() => accessor.CreateInstance<UnsafeTestClass, string>(null), Res.ReflectionCannotCreateInstanceGeneric(testType));

            //Console.Write("Reflector...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestClass)Reflector.CreateInstance(ci, parameters);
            //Assert.AreEqual(arg, result.IntProp);
        }

        [Test]
        public unsafe void ClassComplexConstructionByCtorInfoUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestClass);
            //ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType() });
            //CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            //object[] args = { 1, "dummy", false, null };

            //Console.Write("System Reflection...");
            //object[] parameters = (object[])args.Clone();
            //UnsafeTestClass result = (UnsafeTestClass)ci.Invoke(parameters);
            //Assert.AreEqual(args[0], result.IntProp);
            //Assert.AreNotEqual(args[2], parameters[2]);

            //Console.Write("CreateInstanceAccessor General...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestClass)accessor.CreateInstance(parameters);
            //Assert.AreEqual(args[0], result.IntProp);
            //Assert.AreNotEqual(args[2], parameters[2]);

            //Console.Write("CreateInstanceAccessor NonGeneric...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestClass)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
            //Assert.AreEqual(args[0], result.IntProp);

            //Console.Write("CreateInstanceAccessor Generic...");
            //parameters = (object[])args.Clone();
            //result = accessor.CreateInstance<UnsafeTestClass, int, string, bool, string>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            //Assert.AreEqual(args[0], result.IntProp);

            //Console.Write("Reflector...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestClass)Reflector.CreateInstance(ci, parameters);
            //Assert.AreEqual(args[0], result.IntProp);
            //Assert.AreNotEqual(args[2], parameters[2]);
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

            Console.Write("CreateInstanceAccessor General...");
            result = accessor.CreateInstance(Reflector.EmptyObjects);
            Assert.AreEqual(default(TestStruct), result);

            Console.Write("CreateInstanceAccessor NonGeneric...");
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

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestStruct)accessor.CreateInstance(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Throws<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            Throws<ArgumentException>(() => accessor.CreateInstance(Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.CreateInstance(new object[] { "x" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (TestStruct)accessor.CreateInstance(arg);
            Assert.AreEqual(args[0], result.IntProp);
            Throws<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParamsLengthMismatch(1, 0));
            Throws<ArgumentException>(() => accessor.CreateInstance("x"), Res.NotAnInstanceOfType(typeof(int)));

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
            ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType() })!;
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            object[] args = { 1, "dummy", false, null };

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestStruct result = (TestStruct)ci.Invoke(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestStruct)accessor.CreateInstance(parameters);
            Assert.AreEqual(args[0], result.IntProp);
            Assert.AreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = (TestStruct)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
            Assert.AreEqual(args[0], result.IntProp);

            Console.Write("CreateInstanceAccessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.CreateInstance<TestStruct, int, string, bool, string>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            Assert.AreEqual(args[0], result.IntProp);

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
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(testType);

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

            Console.Write("CreateInstanceAccessor General...");
            result = (TestStructWithParameterlessCtor)accessor.CreateInstance(Reflector.EmptyObjects);
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (TestStructWithParameterlessCtor)accessor.CreateInstance();
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestStructWithParameterlessCtor>();
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
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);

            Console.Write("System Reflection...");
            TestStructWithParameterlessCtor result = (TestStructWithParameterlessCtor)ci.Invoke(null);
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor General...");
            result = (TestStructWithParameterlessCtor)accessor.CreateInstance(Reflector.EmptyObjects);
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (TestStructWithParameterlessCtor)accessor.CreateInstance();
            Assert.IsTrue(result.Initialized);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestStructWithParameterlessCtor>();
            Assert.IsTrue(result.Initialized);

            Console.Write("Reflector...");
            result = (TestStructWithParameterlessCtor)Reflector.CreateInstance(ci);
            Assert.IsTrue(result.Initialized);
        }

        #endregion

        #region Struct construction (unsafe)

        [Test]
        public unsafe void StructConstructionByTypeUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestStruct);
            //var accessor = CreateInstanceAccessor.GetAccessor(testType);

            //Console.Write("System Activator...");
            //object result = Activator.CreateInstance(testType);
            //Assert.AreEqual(default(UnsafeTestStruct), result);

            //Console.Write("CreateInstanceAccessor General...");
            //result = accessor.CreateInstance(Reflector.EmptyObjects);
            //Assert.AreEqual(default(UnsafeTestStruct), result);

            //Console.Write("CreateInstanceAccessor NonGeneric...");
            //result = accessor.CreateInstance();
            //Assert.AreEqual(default(UnsafeTestStruct), result);

            //Console.Write("CreateInstanceAccessor Generic...");
            //result = accessor.CreateInstance<UnsafeTestStruct>();
            //Assert.AreEqual(default(UnsafeTestStruct), result);
            //Throws<ArgumentException>(() => accessor.CreateInstance<TestClass>(), Res.ReflectionCannotCreateInstanceGeneric(testType));
            //Throws<ArgumentException>(() => accessor.CreateInstance<UnsafeTestStruct, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            //Console.Write("Reflector...");
            //result = Reflector.CreateInstance(testType);
            //Assert.AreEqual(default(UnsafeTestStruct), result);
        }

        [Test]
        public unsafe void StructConstructionByCtorInfoUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestStruct);
            //ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int) });
            //CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            //int arg = 1;
            //object[] args = { arg };

            //Console.Write("System Reflection...");
            //object[] parameters = (object[])args.Clone();
            //UnsafeTestStruct result = (UnsafeTestStruct)ci.Invoke(parameters);
            //Assert.AreEqual(args[0], result.IntProp);

            //Console.Write("CreateInstanceAccessor General...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestStruct)accessor.CreateInstance(parameters);
            //Assert.AreEqual(args[0], result.IntProp);
            //Throws<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            //Throws<ArgumentException>(() => accessor.CreateInstance(Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(1, 0));
            //Throws<ArgumentException>(() => accessor.CreateInstance(new object[] { "x" }), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            //Console.Write("CreateInstanceAccessor NonGeneric...");
            //result = (UnsafeTestStruct)accessor.CreateInstance(arg);
            //Assert.AreEqual(args[0], result.IntProp);
            //Throws<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParamsLengthMismatch(1, 0));
            //Throws<ArgumentException>(() => accessor.CreateInstance("x"), Res.NotAnInstanceOfType(typeof(int)));

            //Console.Write("CreateInstanceAccessor Generic...");
            //result = accessor.CreateInstance<UnsafeTestStruct, int>(arg);
            //Assert.AreEqual(arg, result.IntProp);
            //Throws<ArgumentException>(() => accessor.CreateInstance<TestClass, int>(arg), Res.ReflectionCannotCreateInstanceGeneric(testType));
            //Throws<ArgumentException>(() => accessor.CreateInstance<UnsafeTestStruct, string>(null), Res.ReflectionCannotCreateInstanceGeneric(testType));

            //Console.Write("Reflector...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestStruct)Reflector.CreateInstance(ci, parameters);
            //Assert.AreEqual(args[0], result.IntProp);
        }

        [Test]
        public unsafe void StructComplexConstructionByCtorInfoUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestStruct);
            //ConstructorInfo ci = testType.GetConstructor(new[] { typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType() })!;
            //CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            //object[] args = { 1, "dummy", false, null };

            //Console.Write("System Reflection...");
            //object[] parameters = (object[])args.Clone();
            //UnsafeTestStruct result = (UnsafeTestStruct)ci.Invoke(parameters);
            //Assert.AreEqual(args[0], result.IntProp);
            //Assert.AreNotEqual(args[2], parameters[2]);

            //Console.Write("CreateInstanceAccessor General...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestStruct)accessor.CreateInstance(parameters);
            //Assert.AreEqual(args[0], result.IntProp);
            //Assert.AreNotEqual(args[2], parameters[2]);

            //Console.Write("CreateInstanceAccessor NonGeneric...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestStruct)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
            //Assert.AreEqual(args[0], result.IntProp);

            //Console.Write("CreateInstanceAccessor Generic...");
            //parameters = (object[])args.Clone();
            //result = accessor.CreateInstance<UnsafeTestStruct, int, string, bool, string>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            //Assert.AreEqual(args[0], result.IntProp);

            //Console.Write("Reflector...");
            //parameters = (object[])args.Clone();
            //result = (UnsafeTestStruct)Reflector.CreateInstance(ci, parameters);
            //Assert.AreEqual(args[0], result.IntProp);
            //Assert.AreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public unsafe void StructConstructionWithDefaultCtorByTypeUnsafe()
        {
            throw null;
//            Type testType = typeof(UnsafeTestStructWithParameterlessCtor);
//            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(testType);

//            Console.Write("System Activator...");
//            var result = (UnsafeTestStructWithParameterlessCtor)Activator.CreateInstance(testType);
//            Assert.IsTrue(result.Initialized);

//            Console.Write("System Activator for the 2nd time...");
//            result = (UnsafeTestStructWithParameterlessCtor)Activator.CreateInstance(testType);
//            if (!result.Initialized)
//                Console.WriteLine("Constructor was not invoked!");
//#if !NETFRAMEWORK // Activator.CreateInstance does not execute the default struct constructor for the 2nd time
//            Assert.IsTrue(result.Initialized);
//#endif

//            Console.Write("Type Descriptor...");
//            result = (UnsafeTestStructWithParameterlessCtor)TypeDescriptor.CreateInstance(null, testType, null, null);
//            Assert.IsTrue(result.Initialized);

//            Console.Write("CreateInstanceAccessor General...");
//            result = (UnsafeTestStructWithParameterlessCtor)accessor.CreateInstance(Reflector.EmptyObjects);
//            Assert.IsTrue(result.Initialized);

//            Console.Write("CreateInstanceAccessor NonGeneric...");
//            result = (UnsafeTestStructWithParameterlessCtor)accessor.CreateInstance();
//            Assert.IsTrue(result.Initialized);

//            Console.Write("CreateInstanceAccessor Generic...");
//            result = accessor.CreateInstance<UnsafeTestStructWithParameterlessCtor>();
//            Assert.IsTrue(result.Initialized);

//            Console.Write("Reflector...");
//            result = (UnsafeTestStructWithParameterlessCtor)Reflector.CreateInstance(testType);
//            Assert.IsTrue(result.Initialized);
        }

        [Test]
        public unsafe void StructConstructionWithDefaultCtorByCtorInfoUnsafe()
        {
            throw null;
            //Type testType = typeof(UnsafeTestStructWithParameterlessCtor);
            //ConstructorInfo ci = testType.GetConstructor(Type.EmptyTypes);
            //CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);

            //Console.Write("System Reflection...");
            //UnsafeTestStructWithParameterlessCtor result = (UnsafeTestStructWithParameterlessCtor)ci.Invoke(null);
            //Assert.IsTrue(result.Initialized);

            //Console.Write("CreateInstanceAccessor General...");
            //result = (UnsafeTestStructWithParameterlessCtor)accessor.CreateInstance(Reflector.EmptyObjects);
            //Assert.IsTrue(result.Initialized);

            //Console.Write("CreateInstanceAccessor NonGeneric...");
            //result = (UnsafeTestStructWithParameterlessCtor)accessor.CreateInstance();
            //Assert.IsTrue(result.Initialized);

            //Console.Write("CreateInstanceAccessor Generic...");
            //result = accessor.CreateInstance<UnsafeTestStructWithParameterlessCtor>();
            //Assert.IsTrue(result.Initialized);

            //Console.Write("Reflector...");
            //result = (UnsafeTestStructWithParameterlessCtor)Reflector.CreateInstance(ci);
            //Assert.IsTrue(result.Initialized);
        }

        #endregion

        #region MemberOf

        [Test]
        public void MemberOfTest()
        {
            MemberInfo methodIntParse = Reflector.MemberOf(() => int.Parse(default(string), default(IFormatProvider))); // MethodInfo: Int32.Parse(string, IFormatProvider)
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
        public void ReflectorTest_PartiallyTrusted()
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
