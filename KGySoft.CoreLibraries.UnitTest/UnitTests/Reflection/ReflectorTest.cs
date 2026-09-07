#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReflectorTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
#if NETFRAMEWORK
using System.Security;
using System.Security.Permissions;
#endif

using KGySoft.Annotations;
using KGySoft.Reflection;

using NUnit.Framework;

#endregion

#region Suppressions

#pragma warning disable 649 // Fields never assigned to, and will always have their default value - reflection access
// ReSharper disable AssignNullToNotNullAttribute - intended test cases
// ReSharper disable AccessToModifiedClosure
// ReSharper disable UnusedMember.Local - reflection access
// ReSharper disable UnassignedReadonlyField - reflection access
// ReSharper disable UnusedParameter.Local - reflection tests do test the success of the call only
// ReSharper disable MemberCanBePrivate.Local - simpler member retrieval if they are public
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable JoinDeclarationAndInitializer - needed for #ifs and makes testing easier when code is partially commented out
// ReSharper disable RedundantExplicitParamsArrayCreation - makes it more obvious that the array overload is used in the test

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

            public int this[in int intRef]
            {
                get
                {
                    Console.WriteLine($"{nameof(TestClass)}.IndexerGetter[in {intRef}] invoked");
                    return IntProp;
                }
                set
                {
                    Console.WriteLine($"{nameof(TestClass)}.IndexerSetter[in {intRef}] = {value} invoked");
                    IntProp = value;
                }
            }

            public ref string this[string i]
            {
                get
                {
                    Console.WriteLine($"{nameof(TestClass)}.ref IndexerGetter[{i}] invoked");
                    return ref StringField;
                }
            }

            public ref int this[in char i]
            {
                get
                {
                    Console.WriteLine($"{nameof(TestClass)}.ref IndexerGetter[in {i}] invoked");
                    return ref IntField;
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
            public static delegate*<string, void> StaticFunctionPointerField;

            #endregion

            #region Instance Fields

            public readonly void* ReadOnlyInstanceField;

            public void* InstanceField;
            public delegate*<string, void> InstanceFunctionPointerField;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Static Properties

            public static void* StaticProperty { get; set; }
            public static delegate*<string, void> StaticFunctionPointerProperty { get; set; }
            public static ref void* StaticRefProperty => ref StaticField;
            public static ref readonly void* StaticRefReadonlyProperty => ref StaticField;

            #endregion

            #region Instance Properties

            public void* InstanceProperty { get; set; }
            public delegate*<string, void> InstanceFunctionPointerProperty { get; set; }
            public ref void* RefInstanceProperty => ref InstanceField;
            public ref readonly void* RefReadonlyProperty => ref ReadOnlyInstanceField;

            #endregion

            #endregion

            #region Indexers

            // pointer parameter
            public IntPtr this[void* i]
            {
                get
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerGetter[{(IntPtr)i}] invoked");
                    return (IntPtr)InstanceField;
                }
                set
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerSetter[{(IntPtr)i}] = {value} invoked");
                    InstanceField = value.ToPointer();
                }
            }

            // pointer return value
            public void* this[IntPtr i]
            {
                get
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerGetter[{i}] invoked");
                    return InstanceField;
                }
                set
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerSetter[{i}] = {(IntPtr)value} invoked");
                    InstanceField = value;
                }
            }

            // byref pointer parameter
            public IntPtr this[in void* i]
            {
                get
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerGetter[in {(IntPtr)i}] invoked");
                    return (IntPtr)InstanceField;
                }
                set
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.IndexerSetter[in {(IntPtr)i}] = {value} invoked");
                    InstanceField = value.ToPointer();
                }
            }

            // ref pointer return value
            public ref void* this[int* i]
            {
                get
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.ref Indexer[{(IntPtr)i}] invoked");
                    return ref InstanceField;
                }
            }

            // ref pointer return value and byref parameter
            public ref void* this[in int* i]
            {
                get
                {
                    Console.WriteLine($"{nameof(UnsafeTestClass)}.ref Indexer[in {(IntPtr)i}] invoked");
                    return ref InstanceField;
                }
            }

            #endregion

            #endregion

            #region Constructors

            public UnsafeTestClass() => Console.WriteLine($"{nameof(UnsafeTestClass)}.Constructor() invoked");

            public UnsafeTestClass(void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.Constructor({(IntPtr)ptr}) invoked");
                InstanceProperty = ptr;
            }

            public UnsafeTestClass(ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.Constructor(ref {(IntPtr)refPtr}) invoked");
                InstanceField = refPtr;
                refPtr = null;
            }

            public UnsafeTestClass(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.Constructor({(IntPtr)ptr},{(IntPtr)intPtr}, out int*, ref {(IntPtr)refPtr}) invoked");
                InstanceField = ptr;
                InstanceProperty = intPtr;
                outIntPtr = intPtr;
                refPtr = ptr;
            }

            #endregion

            #region Methods

            #region Static Methods

            public static void StaticTestAction(void* ptr, delegate*<string, void> funcPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(StaticTestAction)}({(IntPtr)ptr}, {(IntPtr)funcPtr}) invoked");
                StaticProperty = ptr;
                StaticFunctionPointerField = funcPtr;
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

            public void TestActionRefParam(ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestActionRefParam)}({(IntPtr)refPtr}) invoked");
                InstanceProperty = refPtr;
                refPtr = null;
            }

            public void ComplexTestAction(void* ptr, int* intPtr, out int* outIntPtr, ref void* refPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(ComplexTestAction)}({(IntPtr)ptr},out int*,{(IntPtr)intPtr},{(IntPtr)refPtr}) invoked");
                outIntPtr = intPtr;
                refPtr = ptr;
                InstanceField = ptr;
                InstanceProperty = intPtr;
            }

            public IntPtr TestFunctionPtrParam(int* intPtr, void* ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestFunctionPtrParam)}({(IntPtr)intPtr},{(IntPtr)ptr}) invoked");
                InstanceProperty = ptr;
                return (IntPtr)intPtr;
            }

            public void* TestFunctionPtrReturn(IntPtr intPtr, IntPtr ptr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestFunctionPtrParam)}({intPtr},{ptr}) invoked");
                InstanceProperty = ptr.ToPointer();
                return intPtr.ToPointer();
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

            public IntPtr TestFunctionRefParam(ref int* intPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestFunctionRefParam)}({(IntPtr)intPtr}) invoked");
                InstanceField = intPtr;
                intPtr = null;
                return (IntPtr)InstanceField;
            }

            public ref void* TestFunctionRefReturn(IntPtr intPtr)
            {
                Console.WriteLine($"{nameof(UnsafeTestClass)}.{nameof(TestFunctionRefParam)}({intPtr}) invoked");
                InstanceField = intPtr.ToPointer();
                return ref InstanceField;
            }

            #endregion

            #endregion
        }

        #endregion

        #region TestConstants

        public static class TestConstants
        {
            #region Constants

            public const bool BoolValue = true;
            public const string StringValue = "value";
            public const nint IntPtrValue = -1;
            public const nuint UIntPtrValue = 1;
            [CanBeNull] public const string NullValue = null;
            public const ConsoleColor EnumValue = ConsoleColor.Blue;

            #endregion
        }

        #endregion

        #region StaticTestClassGet

        private static class StaticTestClassGet
        {
            #region Fields

            public static decimal DecimalField;

            #endregion
        }

        #endregion

        #region StaticTestClassSet

        private static class StaticTestClassSet
        {
            #region Fields

            public static decimal DecimalField;

            #endregion
        }

        #endregion

        #region StaticTestClassGetGeneric

        private static class StaticTestClassGetGeneric
        {
            #region Fields

            public static decimal DecimalField;

            #endregion
        }

        #endregion

        #region StaticTestClassSetGeneric

        private static class StaticTestClassSetGeneric
        {
            #region Fields

            public static decimal DecimalField;

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
                test.StructInstanceComplexFunctionMethodInvoke();

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

            public ref int* this[long* index] => ref StaticField;

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

        #region Initialization

#if AOT
        [OneTimeSetUp]
        public void EnsureAotGenericTests()
        {
            Reflector.MemberOf(() => ConstantFieldAccess<byte>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<sbyte>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<short>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<int>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<long>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<ushort>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<uint>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<ulong>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<char>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<float>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<double>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<bool>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<string>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<ConsoleColor>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<nint>(default, default, default));
            Reflector.MemberOf(() => ConstantFieldAccess<nuint>(default, default, default));
        }
#endif

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
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            AssertAreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            AssertAreEqual(arg1, test.IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg1]), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg1, arg2);
            AssertAreEqual(arg1, test.IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            accessor.InvokeInstanceAction(test, arg1, arg2);
            AssertAreEqual(arg1, test.IntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticAction(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestAction), mi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticAction<TestClass, int, string>(null, arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestAction), mi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.InvokeInstanceAction<TestClass, int, string>(null, arg1, arg2), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceAction(test, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestAction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceAction(test, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestAction), mi.DeclaringType));

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.TestAction), parameters);
            AssertAreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.TestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, test.IntProp);
        }

        [Test]
        public void ClassStaticSimpleActionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            AssertAreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            MethodAccessor.GetAccessor(mi).Invoke(null, arg1, arg2);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg1, arg2);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeInstanceAction(new TestClass(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestClass.StaticTestAction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticAction(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestAction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticAction(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestAction), mi.DeclaringType));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestAction), parameters);
            AssertAreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
        }

        [Test]
        public void ClassInstanceComplexActionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.ComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestClass(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            accessor.InvokeInstanceAction(test, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestAction), parameters);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassStaticComplexActionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticComplexTestAction))!;
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection.MethodInfo...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            TestClass.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).InvokeStaticAction((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestAction), parameters);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassInstanceSimpleFunctionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.TestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, test.IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg1]), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, test.IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeInstanceFunction<TestClass, int, string, int>(test, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, test.IntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticFunction<int, string, int>(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestFunction), mi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticFunction<TestClass, int, string, int>(null, arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestClass.TestFunction), mi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, int>(null, arg1, arg2), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int, int>(test, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, string, int, int>(test, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, object>(test, arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestFunction), mi.DeclaringType));

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.TestFunction), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.TestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, test.IntProp);
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
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, test.IntField);

#if NET8_0_OR_GREATER
            test = new TestClass(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            result = inv.Invoke(test, arg);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, test.IntField);
#endif

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                result = accessor.Invoke(test, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, test.IntField);
                AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, ["1"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            }

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, arg));
            else
            {
                result = accessor.Invoke(test, arg);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, test.IntField);
                AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, arg), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), arg), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, "1"), Res.NotAnInstanceOfType(typeof(int)));
            }

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<TestClass, int, int>(test, arg));
            else
            {
                result = accessor.InvokeInstanceFunction<TestClass, int, int>(test, arg);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, test.IntField);
                AssertThrows<ArgumentNullException>(() => accessor.InvokeInstanceFunction<TestClass, int, int>(null, arg), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int>(test), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestRefFunction), mi.DeclaringType));
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, int>(test, arg, "x"), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestRefFunction), mi.DeclaringType));
            }

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, test.IntField);
            }

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction), parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction), parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, test.IntField);
            }

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction).ToLowerInvariant(), true, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(TestClass.TestRefFunction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, test.IntField);
            }
        }

        [Test]
        public void ClassStaticSimpleFunctionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = [arg1, arg2];
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<int, string, int>(arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeInstanceFunction<TestClass, int, string, int>(new TestClass(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<int, int>(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<string, int, int>(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<int, string, object>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.StaticTestFunction), mi.DeclaringType));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestFunction), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestClass.StaticIntProp);
        }

        [Test]
        public void ClassInstanceComplexFunctionMethodInvoke()
        {
            var test = new TestClass(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestClass.ComplexTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestClass(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            test = new TestClass(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);

            test = new TestClass(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.InvokeInstanceFunction<TestClass, int, string, bool, string, int>(test, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);

            test = new TestClass(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestFunction), parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestClass(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestClass.ComplexTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], test.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void ClassStaticComplexFunctionMethodInvoke()
        {
            Type testType = typeof(TestClass);
            MethodInfo mi = testType.GetMethod(nameof(TestClass.StaticComplexTestFunction))!;
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(null, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            TestClass.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = MethodAccessor.GetAccessor(mi).Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            result = MethodAccessor.GetAccessor(mi).InvokeStaticFunction<int, string, bool, string, int>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestFunction), parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestClass.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestClass.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void LongActionMethodInvoke()
        {
            var test = new TestClass();
            var accessor = MethodAccessor.GetAccessor(typeof(TestClass).GetMethod(nameof(TestClass.LongTestAction))!);
            
            Assert.DoesNotThrow(() => accessor.Invoke(test, 1, "2", 3L, '4', 5m));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(5, 0));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.ReflectionParamsLengthMismatch(5, 1));
            AssertThrows<NotSupportedException>(() => accessor.InvokeInstanceAction(test, 1), Res.ReflectionMethodGenericNotSupported);
        }

        [Test]
        public void LongFunctionMethodInvoke()
        {
            var test = new TestClass();
            var accessor = MethodAccessor.GetAccessor(typeof(TestClass).GetMethod(nameof(TestClass.LongTestFunction))!);

            Assert.DoesNotThrow(() => accessor.Invoke(test, 1, "2", 3L, '4', 5m));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(5, 0));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.ReflectionParamsLengthMismatch(5, 1));
            AssertThrows<NotSupportedException>(() => accessor.InvokeInstanceFunction<TestClass, int, bool>(test, 1), Res.ReflectionMethodGenericNotSupported);
        }

        [Test]
        public void SpecialMethodInvoke()
        {
            // abstract method: pass
            MethodInfo mi = typeof(Stream).GetMethod(nameof(Stream.Read), [typeof(byte[]), typeof(int), typeof(int)])!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            Assert.IsTrue(mi.IsAbstract);
            AssertAreEqual(1, accessor.Invoke(new MemoryStream(new byte[1]), [new byte[1], 0, 1]));
            AssertAreEqual(1, accessor.Invoke(new MemoryStream(new byte[1]), new byte[1], 0, 1));
            AssertAreEqual(1, accessor.InvokeInstanceFunction<Stream, byte[], int, int, int>(new MemoryStream(new byte[1]), new byte[1], 0, 1));

            // interface: pass
            mi = typeof(ICollection<int>).GetMethod(nameof(ICollection<>.Contains))!;
            accessor = MethodAccessor.GetAccessor(mi);
            Assert.IsTrue(mi.DeclaringType!.IsInterface);
            Assert.IsFalse((bool)accessor.Invoke(Reflector.EmptyArray<int>(), [42])!);
            Assert.IsFalse((bool)accessor.Invoke(Reflector.EmptyArray<int>(), 42)!);
            Assert.IsFalse(accessor.InvokeInstanceFunction<int[], int, bool>(Reflector.EmptyArray<int>(), 42));

            // generic: fail
            mi = typeof(List<>).GetMethod(nameof(List<>.Contains))!;
            accessor = MethodAccessor.GetAccessor(mi);
            Assert.IsTrue(mi.DeclaringType!.IsGenericTypeDefinition);
            AssertThrows<InvalidOperationException>(() => accessor.Invoke(new List<int>(), [42]), Res.ReflectionGenericMember);
            AssertThrows<InvalidOperationException>(() => accessor.Invoke(new List<int>(), 42), Res.ReflectionGenericMember);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeInstanceFunction<List<int>, int, bool>([], 42), Res.ReflectionGenericMember);
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
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg);
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            accessor.InvokeInstanceAction(test, arg);
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceAction(test, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestClass.TestAction), mi.DeclaringType));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestClass.TestAction), parameters);
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
        }

        [Test]
        public unsafe void ClassStaticSimpleActionMethodInvokeUnsafe()
        {
            if (EnvironmentHelper.IsMono)
                Assert.Inconclusive("This test would crash on Mono");
            Type testType = typeof(UnsafeTestClass);
            MethodInfo mi = testType.GetMethod(nameof(UnsafeTestClass.StaticTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg1 = new IntPtr(1);
            IntPtr arg2 = (IntPtr)(delegate*<string, void>)&Console.WriteLine;
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            AssertAreEqual(arg1, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticFunctionPointerField);

            UnsafeTestClass.StaticProperty = null;
            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            MethodAccessor.GetAccessor(mi).Invoke(null, parameters);
            AssertAreEqual(arg1, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticFunctionPointerField);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [1, 2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Method Accessor NonGeneric...");
            MethodAccessor.GetAccessor(mi).Invoke(null, arg1, arg2);
            AssertAreEqual(arg1, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticFunctionPointerField);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, 1, 2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg1, arg2);
            AssertAreEqual(arg1, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticFunctionPointerField);
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticAction(1, 2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.StaticTestAction), mi.DeclaringType));

            UnsafeTestClass.StaticProperty = null;
            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(arg1, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticFunctionPointerField);

            UnsafeTestClass.StaticProperty = null;
            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestAction), parameters);
            AssertAreEqual(arg1, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticFunctionPointerField);

            UnsafeTestClass.StaticProperty = null;
            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticFunctionPointerField);
        }

        [Test]
        public unsafe void ClassInstanceRefParamActionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestActionRefParam));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;

            // System Reflection does not support initializing the ref pointer parameter - ArgumentException: Object of type 'System.IntPtr' cannot be converted to type 'System.Void*&'
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
            AssertAreEqual(IntPtr.Zero, parameters[0]);

            test = new UnsafeTestClass(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
            AssertAreEqual(IntPtr.Zero, parameters[0]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                accessor.Invoke(test, parameters);
                AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
                AssertAreEqual(IntPtr.Zero, parameters[0]);
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, arg));
            else
            {
                accessor.Invoke(test, arg);
                AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceAction(test, arg));
            else
            {
                accessor.InvokeInstanceAction(test, arg);
                AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceAction(test, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestActionRefParam), mi.DeclaringType));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestActionRefParam), parameters));
            else
            {
                Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestActionRefParam), parameters);
                AssertAreEqual(arg, (IntPtr)test.InstanceProperty);
            }
        }

        [Test]
        public unsafe void ClassInstanceComplexActionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.ComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;

            // System Reflection does not support initializing the ref pointer parameter (ArgumentException : Object of type 'System.IntPtr' cannot be converted to type 'System.Void*&'),
            // and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            AssertAreEqual(args[0], (IntPtr)test.InstanceField);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestClass(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], (IntPtr)test.InstanceField);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                accessor.Invoke(test, parameters);
                AssertAreEqual(args[0], (IntPtr)test.InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], (IntPtr)test.InstanceField);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceAction(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
            else
            {
                accessor.InvokeInstanceAction(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
                AssertAreEqual(args[0], (IntPtr)test.InstanceField);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(args[0], (IntPtr)test.InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction), parameters));
            else
            {
                Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction), parameters);
                AssertAreEqual(args[0], (IntPtr)test.InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction).ToLowerInvariant(), true, parameters));
            else
            {
                Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestAction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], (IntPtr)test.InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            AssertAreNotEqual(args[2], parameters[2]);

            UnsafeTestClass.StaticField = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            UnsafeTestClass.StaticField = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
            else
            {
                accessor.Invoke(null, parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
            else
            {
                accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
            else
            {
                Reflector.InvokeMethod(null, mi, parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction), parameters));
            else
            {
                Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction), parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction).ToLowerInvariant(), true, parameters));
            else
            {
                Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }
        }

        [Test]
        public unsafe void ClassInstancePtrParamFunctionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestFunctionPtrParam));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg1 = new IntPtr(1);
            var arg2 = new IntPtr(2);
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);

#if NET8_0_OR_GREATER
            Console.Write("System Reflection.MethodInvoker...");
            parameters = (object[])args.Clone();
            result = MethodInvoker.Create(mi).Invoke(test, parameters.AsSpan());
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1, arg2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, IntPtr>(test, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, int, IntPtr, IntPtr>(test, 1, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionPtrParam), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, int>(test, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionPtrParam), mi.DeclaringType));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionPtrParam), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionPtrParam).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
        }

        [Test]
        public unsafe void ClassInstancePtrReturnFunctionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestFunctionPtrReturn));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg1 = new IntPtr(1);
            var arg2 = new IntPtr(2);
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = (IntPtr)(Pointer.Unbox(mi.Invoke(test, parameters)));
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);

#if NET8_0_OR_GREATER
            Console.Write("System Reflection.MethodInvoker...");
            parameters = (object[])args.Clone();
            result = (IntPtr)(Pointer.Unbox(MethodInvoker.Create(mi).Invoke(test, parameters.AsSpan())));
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1, arg2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, IntPtr>(test, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, int, IntPtr, IntPtr>(test, 1, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionPtrReturn), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, int>(test, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionPtrReturn), mi.DeclaringType));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionPtrReturn), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionPtrReturn).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)test.InstanceProperty);
        }

        [Test]
        public unsafe void ClassInstanceRefParamFunctionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass();
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestFunctionRefParam));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;

            Console.Write("Direct call...");
            int* ptr = (int*)arg.ToPointer();
            object result = test.TestFunctionRefParam(ref ptr);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)test.InstanceField);
            AssertAreEqual(IntPtr.Zero, (IntPtr)ptr);

#if NET11_0_OR_GREATER
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)test.InstanceField);
            AssertAreEqual(IntPtr.Zero, parameters[0]);

            test = new UnsafeTestClass(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            result = (IntPtr)Pointer.Unbox(inv.Invoke(test, parameters.AsSpan()));
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)test.InstanceField);
            AssertAreEqual(IntPtr.Zero, parameters[0]);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                result = accessor.Invoke(test, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertAreEqual(IntPtr.Zero, parameters[0]);
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, arg));
            else
            {
                result = accessor.Invoke(test, arg);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr>(test, arg));
            else
            {
                result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr>(test, arg);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, int, IntPtr>(test, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionRefParam), mi.DeclaringType));
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, int>(test, arg), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionRefParam), mi.DeclaringType));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertAreEqual(IntPtr.Zero, parameters[0]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefParam), parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefParam), parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertAreEqual(IntPtr.Zero, parameters[0]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefParam).ToLowerInvariant(), true, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefParam).ToLowerInvariant(), true, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertAreEqual(IntPtr.Zero, parameters[0]);
            }
        }

        [Test]
        public unsafe void ClassInstanceRefReturnFunctionMethodInvokeUnsafe()
        {
            var test = new UnsafeTestClass(null);
            MethodInfo mi = test.GetType().GetMethod(nameof(UnsafeTestClass.TestFunctionRefReturn));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            IntPtr arg = new IntPtr(1);
            object[] args = [arg];

            Console.Write("System Reflection...");
            object[] parameters;
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            parameters = (object[])args.Clone();
            object result = (IntPtr)Pointer.Unbox(mi.Invoke(test, parameters));
#else
            object result = (IntPtr)test.TestFunctionRefReturn(arg);
#endif
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)test.InstanceField);

#if NET8_0_OR_GREATER
            test = new UnsafeTestClass(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            result = (IntPtr)Pointer.Unbox(inv.Invoke(test, parameters.AsSpan()));
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)test.InstanceField);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                result = accessor.Invoke(test, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, arg));
            else
            {
                result = accessor.Invoke(test, arg);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr>(test, arg));
            else
            {
                result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr>(test, arg);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, int, IntPtr>(test, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionRefReturn), mi.DeclaringType));
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, int>(test, arg), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.TestFunctionRefReturn), mi.DeclaringType));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefReturn), parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefReturn), parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefReturn).ToLowerInvariant(), true, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.TestFunctionRefReturn).ToLowerInvariant(), true, parameters);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)test.InstanceField);
            }
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
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [1, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1, 2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr>(arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<IntPtr, int, IntPtr>(arg1, 2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, int>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestClass.StaticTestFunction), mi.DeclaringType));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestFunction), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestClass.StaticProperty);
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

            // System Reflection does not support initializing the ref pointer parameter: ArgumentException: 'Object of type 'System.IntPtr' cannot be converted to type 'System.Void*&'
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (IntPtr)Pointer.Unbox(mi.Invoke(test, parameters));
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestClass(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                result = accessor.Invoke(test, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
            else
            {
                result = accessor.InvokeInstanceFunction<UnsafeTestClass, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(test, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction), parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction), parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction).ToLowerInvariant(), true, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestClass.ComplexTestFunction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)test.InstanceProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreNotEqual(args[2], parameters[2]);

            UnsafeTestClass.StaticProperty = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
            else
            {
                result = accessor.Invoke(null, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                result = accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            }

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)args[0], (IntPtr)args[1], default, (IntPtr)args[3]));
            else
            {
                result = accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
            }

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
            else
            {
                result = Reflector.InvokeMethod(null, mi, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction), parameters));
            else
            {
                result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction), parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction).ToLowerInvariant(), true, parameters));
            else
            {
                result = Reflector.InvokeMethod(testType, nameof(UnsafeTestClass.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestClass.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg1]), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg1, arg2);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            accessor.InvokeInstanceAction(testStruct, arg1, arg2);
            AssertAreEqual(arg1, testStruct.IntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticAction(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestAction), mi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticAction<TestStruct, int, string>(default, arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestAction), mi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceAction(testStruct, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestAction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceAction(testStruct, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestAction), mi.DeclaringType));

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.TestAction), parameters);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.TestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);
        }

        [Test]
        public void StructStaticSimpleActionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(null, arg1, arg2);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg1, arg2);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeInstanceAction(new TestStruct(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestStruct.StaticTestAction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticAction(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestAction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticAction(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestAction), mi.DeclaringType));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestAction), parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
        }

        [Test]
        public void StructInstanceComplexActionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.ComplexTestAction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(test, parameters);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestStruct(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            test = new TestStruct(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            accessor.InvokeInstanceAction(testStruct, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], testStruct.IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestAction), parameters);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexActionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticComplexTestAction))!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            mi.Invoke(null, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            TestStruct.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            accessor.InvokeStaticAction((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestAction), parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructInstanceSimpleFunctionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.TestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = [arg1, arg2];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            object result = mi.Invoke(test, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, args), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), args), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [arg1]), Res.ReflectionParamsLengthMismatch(2, 1));

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.Invoke(null, arg1, arg2), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Invoke(new object(), arg1, arg2), Res.NotAnInstanceOfType(test.GetType()));
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(test, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeInstanceFunction<TestStruct, int, string, int>(testStruct, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, testStruct.IntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticFunction<int, string, int>(arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.InvokeStaticFunction<TestStruct, int, string, int>(new TestStruct(), arg1, arg2), Res.ReflectionStaticMethodExpectedGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestStruct, int, int>(testStruct, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestStruct, string, int, int>(testStruct, arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<TestStruct, int, string, object>(testStruct, arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.TestFunction), mi.DeclaringType));

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.TestFunction), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.TestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, ((TestStruct)test).IntProp);
        }

        [Test]
        public void StructStaticSimpleFunctionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            int arg1 = 1;
            string arg2 = "dummy";
            object[] args = [arg1, arg2];
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, [null, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [arg2, arg1]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentException>(() => accessor.Invoke(null, null, arg2), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg2, arg1), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, arg1), Res.ReflectionParamsLengthMismatch(2, 1));

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<int, string, int>(arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);
            AssertThrows<InvalidOperationException>(() => accessor.InvokeInstanceFunction<TestStruct, int, string, int>(new TestStruct(), arg1, arg2), Res.ReflectionInstanceMethodExpectedGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<int, int>(arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<string, int, int>(arg2, arg1), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<int, string, object>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(TestStruct.StaticTestFunction), mi.DeclaringType));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestFunction), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg1, TestStruct.StaticIntProp);
        }

        [Test]
        public void StructInstanceComplexFunctionMethodInvoke()
        {
            object test = new TestStruct(0);
            MethodInfo mi = test.GetType().GetMethod(nameof(TestStruct.ComplexTestFunction));
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [1, "dummy", false, null];
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(test, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            test = new TestStruct(0);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            test = new TestStruct(0);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);

            var testStruct = new TestStruct(0);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.InvokeInstanceFunction<TestStruct, int, string, bool, string, int>(testStruct, (int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], testStruct.IntProp);

            test = new TestStruct(0);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestFunction), parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new TestStruct(0);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(TestStruct.ComplexTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], ((TestStruct)test).IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructStaticComplexFunctionMethodInvoke()
        {
            Type testType = typeof(TestStruct);
            MethodInfo mi = testType.GetMethod(nameof(TestStruct.StaticComplexTestFunction))!;
            MethodAccessor accessor = MethodAccessor.GetAccessor(mi);
            object[] args = [1, "dummy", false, null];
            object result;

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            result = mi.Invoke(null, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            TestStruct.StaticIntProp = 0;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.InvokeStaticFunction<int, string, bool, string, int>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestFunction), parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(TestStruct.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], TestStruct.StaticIntProp);
            AssertAreNotEqual(args[2], parameters[2]);
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
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(test, parameters);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(test, arg);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceAction(unsafeTestStruct, arg));
            else
            {
                accessor.InvokeInstanceAction(unsafeTestStruct, arg);
                AssertAreEqual(arg, (IntPtr)unsafeTestStruct.InstanceProperty);
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceAction(unsafeTestStruct, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.TestAction), mi.DeclaringType));
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestAction), parameters);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
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
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            accessor.Invoke(null, parameters);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            accessor.Invoke(null, arg);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            accessor.InvokeStaticAction(arg);
            AssertAreEqual(arg, (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticAction(1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.StaticTestAction), mi.DeclaringType));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestAction), parameters);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestAction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
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
            AssertAreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestStruct(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                accessor.Invoke(test, parameters);
                AssertAreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            }

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceAction(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
            else
            {
                accessor.InvokeInstanceAction(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
                AssertAreEqual(args[0], (IntPtr)unsafeTestStruct.InstanceField);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction), parameters));
            else
            {
                Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction), parameters);
                AssertAreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction).ToLowerInvariant(), true, parameters));
            else
            {
                Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestAction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            AssertAreNotEqual(args[2], parameters[2]);

            UnsafeTestStruct.StaticField = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            UnsafeTestStruct.StaticField = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
            else
            {
                accessor.Invoke(null, parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestStruct.StaticField = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            }

            UnsafeTestStruct.StaticField = null;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]));
            else
            {
                accessor.InvokeStaticAction((IntPtr)parameters[0], (IntPtr)parameters[1], default(IntPtr), (IntPtr)parameters[3]);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
            }

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
            else
            {
                Reflector.InvokeMethod(null, mi, parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction), parameters));
            else
            {
                Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction), parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction).ToLowerInvariant(), true, parameters));
            else
            {
                Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestAction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticField);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(test, parameters);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(test, arg);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr>(unsafeTestStruct, arg));
            else
            {
                result = accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr>(unsafeTestStruct, arg);
                AssertAreEqual(arg, result);
                AssertAreEqual(arg, (IntPtr)unsafeTestStruct.InstanceProperty);
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, int, IntPtr>(unsafeTestStruct, 1), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.TestFunction), mi.DeclaringType));
                AssertThrows<ArgumentException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, int>(unsafeTestStruct, arg), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.TestFunction), mi.DeclaringType));
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, mi, parameters);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestFunction), parameters);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.TestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg, result);
            AssertAreEqual(arg, (IntPtr)((UnsafeTestStruct)test).InstanceProperty);
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
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            result = accessor.Invoke(null, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, [1, arg2]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            result = accessor.Invoke(null, arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.Invoke(null, 1, arg2), Res.NotAnInstanceOfType(typeof(IntPtr)));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            result = accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr>(arg1, arg2);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<int, IntPtr, IntPtr>(1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.StaticTestFunction), mi.DeclaringType));
            AssertThrows<ArgumentException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, int>(arg1, arg2), Res.ReflectionCannotInvokeMethodGeneric(nameof(UnsafeTestStruct.StaticTestFunction), mi.DeclaringType));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(null, mi, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestFunction), parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticTestFunction).ToLowerInvariant(), true, parameters);
            AssertAreEqual(arg1, result);
            AssertAreEqual(arg2, (IntPtr)UnsafeTestStruct.StaticProperty);
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
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            AssertAreNotEqual(args[2], parameters[2]);

            test = new UnsafeTestStruct(null);
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(test, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters));
            else
            {
                result = accessor.Invoke(test, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                result = accessor.Invoke(test, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
            }

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
            else
            {
                result = accessor.InvokeInstanceFunction<UnsafeTestStruct, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>(unsafeTestStruct, (IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[1], (IntPtr)unsafeTestStruct.InstanceField);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, mi, parameters));
            else
            {
                result = Reflector.InvokeMethod(test, mi, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction), parameters));
            else
            {
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction), parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name, ignore case)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction).ToLowerInvariant(), true, parameters));
            else
            {
                parameters = (object[])args.Clone();
                result = Reflector.InvokeMethod(test, nameof(UnsafeTestStruct.ComplexTestFunction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[1], (IntPtr)((UnsafeTestStruct)test).InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertAreNotEqual(args[2], parameters[2]);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("System Reflection.MethodInvoker...");
            MethodInvoker inv = MethodInvoker.Create(mi);
            parameters = (object[])args.Clone();
            inv.Invoke(null, parameters.AsSpan());
            AssertAreEqual(args[0], result);
            AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            AssertAreNotEqual(args[2], parameters[2]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of MethodAccessor");
#endif

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters));
            else
            {
                result = accessor.Invoke(null, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                result = accessor.Invoke(null, parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            }

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Method Accessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
            else
            {
                result = accessor.InvokeStaticFunction<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
            }

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by MethodInfo)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(null, mi, parameters));
            else
            {
                result = Reflector.InvokeMethod(null, mi, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction), parameters));
            else
            {
                result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction), parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name, ignore case)...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction).ToLowerInvariant(), true, parameters));
            else
            {
                result = Reflector.InvokeMethod(testType, nameof(UnsafeTestStruct.StaticComplexTestFunction).ToLowerInvariant(), true, parameters);
                AssertAreEqual(args[0], result);
                AssertAreEqual(args[0], (IntPtr)UnsafeTestStruct.StaticProperty);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue((TestClass)test, (int)value);
            result = accessor.GetInstanceValue<TestClass, int>((TestClass)test);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.IntProp), pi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(TestClass.IntProp), value);
            result = Reflector.GetProperty(test, nameof(TestClass.IntProp));
            AssertAreEqual(value, result);
            Reflector.SetProperty(test, nameof(TestClass.IntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestClass.IntProp).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestClass.IntProp), value), Res.ArgumentNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestClass.IntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestClass.IntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.IntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestClass.IntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestClass.IntProp)), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestClass.IntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.IntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
        }

        [Test]
        public void ClassInstanceRefPropertyAccess()
        {
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
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            }

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
            }

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value));
            else
            {
                accessor.SetInstanceValue(test, value);
                result = accessor.GetInstanceValue<TestClass, int>(test);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefIntProperty), pi.DeclaringType!));
            }

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));
            }

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(TestClass.RefIntProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(TestClass.RefIntProperty), value);
                result = Reflector.GetProperty(test, nameof(TestClass.RefIntProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(TestClass.RefIntProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(TestClass.RefIntProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(instance:null!, nameof(TestClass.RefIntProperty), value), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestClass.RefIntProperty), null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestClass.RefIntProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefIntProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestClass.RefIntProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(instance:null!, nameof(TestClass.RefIntProperty)), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestClass.RefIntProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefIntProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
            }
        }

        [Test]
        public void ClassInstanceRefReadonlyPropertyAccess()
        {
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
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            }

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
            }

            var testClass = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testClass, value));
            else
            {
                accessor.SetInstanceValue(testClass, value);
                result = accessor.GetInstanceValue<TestClass, int>(testClass);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.RefReadonlyProperty), pi.DeclaringType!));
            }

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));
            }

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty), value);
                result = Reflector.GetProperty(test, nameof(TestClass.RefReadonlyProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(TestClass.RefReadonlyProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(instance:null!, nameof(TestClass.RefReadonlyProperty), value), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestClass.RefReadonlyProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefReadonlyProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestClass.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestClass.RefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(instance:null!, nameof(TestClass.RefReadonlyProperty)), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestClass.RefReadonlyProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.RefReadonlyProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestClass.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(TestClass)));
            }
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
            AssertAreEqual(value, result);

            TestClass.StaticIntProp = 0;
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntProp = 0;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntProp = 0;
            Console.Write("Property Accessor Generic...");
            accessor.SetStaticValue((int)value);
            result = accessor.GetStaticValue<int>();
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticIntProp), testType));
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticIntProp), testType));
            AssertThrows<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticIntProp), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticIntProp), testType));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
               AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp), value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticIntProp));
            AssertAreEqual(value, result);
            Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(TestClass.StaticIntProp).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(TestClass.StaticIntProp), value), Res.ArgumentNull);
            if (!IsAot) // the fallback reflection accepts null as intThrows<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp), null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestClass.StaticIntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticIntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(TestClass.StaticIntProp)), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestClass.StaticIntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticIntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
        }

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
            AssertAreEqual(value, result);

            TestClass.StaticRefProperty = 0;
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(null, value, Reflector.EmptyObjects);
                result = accessor.Get(null, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            TestClass.StaticRefProperty = 0;
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value));
            else
            {
                accessor.Set(null, value);
                result = accessor.Get(null);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));
            }

            TestClass.StaticRefProperty = 0;
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetStaticValue((int)value));
            else
            {
                accessor.SetStaticValue((int)value);
                result = accessor.GetStaticValue<int>();
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefProperty), testType));
                AssertThrows<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefProperty), testType));
                AssertThrows<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefProperty), testType));
                AssertThrows<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefProperty), testType));
            }

            TestClass.StaticRefProperty = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(null, pi, value));
            else
            {
                Reflector.SetProperty(null, pi, value);
                result = Reflector.GetProperty(null, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
            }

            TestClass.StaticRefProperty = 0;
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty), value));
            else
            {
                Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty), value);
                result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(type:null!, nameof(TestClass.StaticRefProperty), value), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty), null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefProperty), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(type:null!, nameof(TestClass.StaticRefProperty)), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefProperty)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
            }
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
            AssertAreEqual(value, result);

            TestClass.StaticIntField = 0;
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(null, value, Reflector.EmptyObjects);
                result = accessor.Get(null, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            TestClass.StaticIntField = 0;
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value));
            else
            {
                accessor.Set(null, value);
                result = accessor.Get(null);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));
            }

            TestClass.StaticIntField = 0;
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetStaticValue((int)value));
            else
            {
                accessor.SetStaticValue((int)value);
                result = accessor.GetStaticValue<int>();
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));
                AssertThrows<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));
                AssertThrows<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));
                AssertThrows<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestClass.StaticRefReadonlyProperty), testType));
            }

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(null, pi, value));
            else
            {
                Reflector.SetProperty(null, pi, value);
                result = Reflector.GetProperty(null, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
            }

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty), value));
            else
            {
                Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty), value);
                result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(type:null!, nameof(TestClass.StaticRefReadonlyProperty), value), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefReadonlyProperty), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefReadonlyProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestClass.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestClass.StaticRefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(type:null!, nameof(TestClass.StaticRefReadonlyProperty)), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestClass.StaticRefReadonlyProperty)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.StaticRefReadonlyProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestClass.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestClass.IntProp), testType));
            }
        }

        [Test]
        public void ClassInstanceIndexerAccess()
        {
            var test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(int)]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1, index = 42;
            object[] indexParameters = [index];

            Console.Write("Direct call...");
            test[(int)index] = (int)value;
            result = test[(int)index];
            AssertAreEqual(value, result);

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = pi.GetValue(test, indexParameters);
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, indexParameters);
            result = accessor.Get(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, ["1"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            if (!IsAot) // the fallback reflection does not tolerate more parameters than needed
                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { 1, "2" }), "More parameters than needed are okay");
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => accessor.Get(test, ["1"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            if (!IsAot) // the fallback reflection does not tolerate more parameters than needed
                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { 1, "2" }), "More parameters than needed are okay");

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value, index);
            result = accessor.Get(test, index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, "1"), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Get(test, "1"), Res.NotAnInstanceOfType(typeof(int)));

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, (int)value, (int)index);
            result = accessor.GetInstanceValue<TestClass, int, int>(test, (int)index);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1, 1), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1", 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int, int>(null, 1), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int, int>(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, int>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, int, string>(test, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            test = new TestClass(0);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            AssertThrows<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        [Test]
        public void ClassInstanceRefParamIndexerAccess()
        {
            var test = new TestClass(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(int).MakeByRefType()]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            int value = 1, index = 42;
            object[] indexParameters = [index];

            Console.Write("Direct call...");
            test[in index] = value;
            result = test[in index];
            AssertAreEqual(value, result);

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = pi.GetValue(test, indexParameters);
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, indexParameters);
            result = accessor.Get(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, ["1"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            if (!IsAot) // the fallback reflection does not tolerate more parameters than needed
                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { 1, "2" }), "More parameters than needed are okay");
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => accessor.Get(test, ["1"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            if (!IsAot) // the fallback reflection does not tolerate more parameters than needed
                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { 1, "2" }), "More parameters than needed are okay");

            test = new TestClass(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value, index);
            result = accessor.Get(test, index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, "1"), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Get(test, "1"), Res.NotAnInstanceOfType(typeof(int)));

            test = new TestClass(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, value, index);
            result = accessor.GetInstanceValue<TestClass, int, int>(test, index);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1, 1), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1", 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int, int>(null, 1), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int, int>(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, int>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, int, string>(test, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            test = new TestClass(0);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            AssertThrows<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        [Test]
        public void ClassInstanceRefReturnIndexerAccess()
        {
            var test = new TestClass();
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(string)]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            string index = "x";
            object[] indexParameters = [index];
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
            AssertAreEqual(value, result);

            test = new TestClass();
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters));
            else
            {
                accessor.Set(test, value, indexParameters);
                result = accessor.Get(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { "1", 2 }), "More parameters than needed are okay");
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { "1", 2 }), "More parameters than needed are okay");
            }

            test = new TestClass();
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, index));
            else
            {
                accessor.Set(test, value, index);
                result = accessor.Get(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(string)));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(string)));
            }

            test = new TestClass();
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value, index));
            else
            {
                accessor.SetInstanceValue(test, value, index);
                result = accessor.GetInstanceValue<TestClass, string, string>(test, index);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, value, index), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), value, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<string>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, string, string>(null, index), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, string, string>(new object(), index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, string>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, string, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            test = new TestClass();
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters));
            else
            {
                Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
                result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
            }

            test = new TestClass();
            Console.Write("Reflector (by parameters match)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetIndexedMember(test, value, indexParameters));
            else
            {
                Reflector.SetIndexedMember(test, value, indexParameters);
                result = Reflector.GetIndexedMember(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
                AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
                AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
                AssertThrows<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
                AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
            }
        }

        [Test]
        public void ClassInstanceRefReturnRefParamIndexerAccess()
        {
            var test = new TestClass();
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(char).MakeByRefType()]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            char index = 'x';
            object[] indexParameters = [index];
            object result;
            int value = 13;

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
            AssertAreEqual(value, result);

            test = new TestClass();
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters));
            else
            {
                accessor.Set(test, value, indexParameters);
                result = accessor.Get(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, '1', indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(char)));
                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { index, 2 }), "More parameters than needed are okay");
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(char)));
                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { index, 2 }), "More parameters than needed are okay");
            }

            test = new TestClass();
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, index));
            else
            {
                accessor.Set(test, value, index);
                result = accessor.Get(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(char)));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(char)));
            }

            test = new TestClass();
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value, index));
            else
            {
                accessor.SetInstanceValue(test, value, index);
                result = accessor.GetInstanceValue<TestClass, int, char>(test, index);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, value, index), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), value, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1", index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, value), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<string>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int, char>(null, index), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int, char>(new object(), index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, string>(test), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestClass, string, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            test = new TestClass();
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters));
            else
            {
                Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
                result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(char)));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(char)));
            }

            test = new TestClass();
            Console.Write("Reflector (by parameters match)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetIndexedMember(test, value, indexParameters));
            else
            {
                Reflector.SetIndexedMember(test, value, indexParameters);
                result = Reflector.GetIndexedMember(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
                AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
                AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
                AssertThrows<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
                AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 1m), Res.ReflectionIndexerNotFound(test.GetType()));
            }
        }

        #endregion

        #region Class property access (unsafe)

        [Test]
        public unsafe void ClassInstancePropertyAccessUnsafe()
        {
            object test = new UnsafeTestClass(null);
            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestClass.InstanceProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            object value = new IntPtr(1);

            Console.Write("System Reflection...");
            pi.SetValue(test, value, null);
            if (!EnvironmentHelper.IsMono) // System.ArgumentException : The type 'System.Void*' may not be used as a type argument
            {
                result = (IntPtr)Pointer.Unbox(pi.GetValue(test, null));
                AssertAreEqual(value, result);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue((UnsafeTestClass)test, (IntPtr)value);
            result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr>((UnsafeTestClass)test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue((UnsafeTestClass)test, 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.InstanceProperty), pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>((UnsafeTestClass)test), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.InstanceProperty), pi.DeclaringType!));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(UnsafeTestClass.InstanceProperty), value);
            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.InstanceProperty));
            AssertAreEqual(value, result);
            Reflector.SetProperty(test, nameof(UnsafeTestClass.InstanceProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.InstanceProperty).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.InstanceProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
        }

        [Test]
        public unsafe void ClassInstanceFunctionPointerPropertyAccessUnsafe()
        {
            if (EnvironmentHelper.IsMono) // IsInstanceOfType, IsValueType, etc. all throw StackOverflowException for function pointers on Mono
                Assert.Inconclusive("This test would crash on Mono");
            object test = new UnsafeTestClass(null);
            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestClass.InstanceFunctionPointerProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            object value = new IntPtr((delegate*<string, void>)&Console.WriteLine);

            Console.Write("System Reflection...");
            pi.SetValue(test, value, null);
            result = (IntPtr)pi.GetValue(test, null)!;
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue((UnsafeTestClass)test, (IntPtr)value);
            result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr>((UnsafeTestClass)test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue((UnsafeTestClass)test, 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.InstanceFunctionPointerProperty), pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>((UnsafeTestClass)test), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.InstanceFunctionPointerProperty), pi.DeclaringType!));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(UnsafeTestClass.InstanceFunctionPointerProperty), value);
            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.InstanceFunctionPointerProperty));
            AssertAreEqual(value, result);
            Reflector.SetProperty(test, nameof(UnsafeTestClass.InstanceFunctionPointerProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(UnsafeTestClass.InstanceFunctionPointerProperty).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.InstanceFunctionPointerProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
        }

        [Test]
        public unsafe void ClassInstanceRefPropertyAccessUnsafe()
        {
            var test = new UnsafeTestClass(null);
            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestClass.RefInstanceProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, null);
#else
            test.RefInstanceProperty = (void*)value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, null));
#else
            result = (IntPtr)test.RefInstanceProperty;
#endif
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value));
            else
            {
                accessor.SetInstanceValue(test, value);
                result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr>(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefInstanceProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(test), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefInstanceProperty), pi.DeclaringType!));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefInstanceProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(UnsafeTestClass.RefInstanceProperty), value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefInstanceProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(UnsafeTestClass.RefInstanceProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefInstanceProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefInstanceProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
            }
        }

        [Test]
        public unsafe void ClassInstanceRefReadonlyPropertyAccessUnsafe()
        {
            object test = new UnsafeTestClass(null);
            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestClass.RefReadonlyProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            var value = new IntPtr(1);

            if (!EnvironmentHelper.IsMono) // (IntPtr)((UnsafeTestClass)test).RefReadonlyProperty returns some random value on Mono
            {
                Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
                pi.SetValue(test, value, null);
#else
                typeof(UnsafeTestClass).GetField(nameof(UnsafeTestClass.ReadOnlyInstanceField))!.SetValue(test, value);
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
                result = (IntPtr)Pointer.Unbox(pi.GetValue(test, null));
#else
                result = (IntPtr)((UnsafeTestClass)test).RefReadonlyProperty;
#endif
                AssertAreEqual(value, result);
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue((UnsafeTestClass)test, value));
            else
            {
                accessor.SetInstanceValue((UnsafeTestClass)test, value);
                result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr>((UnsafeTestClass)test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.RefReadonlyProperty), pi.DeclaringType!));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty), value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestClass.RefReadonlyProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
            }
        }

        [Test]
        public unsafe void ClassStaticPropertyAccessUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestClass.StaticProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            object value = new IntPtr(1);

            Console.Write("System Reflection...");
            pi.SetValue(null, value, null);
            if (!EnvironmentHelper.IsMono) // System.ArgumentException : The type 'System.Void*' may not be used as a type argument
            {
                result = (IntPtr)Pointer.Unbox(pi.GetValue(null, null));
                AssertAreEqual(value, result);
            }

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Property Accessor Generic...");
            accessor.SetStaticValue((IntPtr)value);
            result = accessor.GetStaticValue<IntPtr>();
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticProperty), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticProperty), testType));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticProperty), value);
            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticProperty));
            AssertAreEqual(value, result);
            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticProperty).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
        }

        [Test]
        public unsafe void ClassStaticFunctionPointerPropertyAccessUnsafe()
        {
            if (EnvironmentHelper.IsMono)
                Assert.Inconclusive("This test would crash on Mono");
            Type testType = typeof(UnsafeTestClass);
            PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestClass.StaticFunctionPointerProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            object value = new IntPtr((delegate*<string, void>)&Console.WriteLine);

            Console.Write("System Reflection...");
            pi.SetValue(null, value, null);
            result = (IntPtr)pi.GetValue(null, null)!;
            AssertAreEqual(value, result);

            UnsafeTestClass.StaticFunctionPointerProperty = null;
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticFunctionPointerProperty = null;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticFunctionPointerProperty = null;
            Console.Write("Property Accessor Generic...");
            accessor.SetStaticValue((IntPtr)value);
            result = accessor.GetStaticValue<IntPtr>();
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticFunctionPointerProperty), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticFunctionPointerProperty), testType));

            UnsafeTestClass.StaticFunctionPointerProperty = null;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticFunctionPointerProperty = null;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticFunctionPointerProperty), value);
            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticFunctionPointerProperty));
            AssertAreEqual(value, result);
            Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticFunctionPointerProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticFunctionPointerProperty).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticFunctionPointerProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
        }

        [Test]
        public unsafe void ClassStaticRefPropertyAccessUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestClass.StaticRefProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            object value = new IntPtr(1);

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(null, value, null);
#else
            UnsafeTestClass.StaticRefProperty = (void*)(IntPtr)value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = (IntPtr)Pointer.Unbox(pi.GetValue(null, null));
#else
            result = (IntPtr)UnsafeTestClass.StaticRefProperty;
#endif
            AssertAreEqual(value, result);

            UnsafeTestClass.StaticRefProperty = null;
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(null, value, Reflector.EmptyObjects);
                result = accessor.Get(null, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(null, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            UnsafeTestClass.StaticRefProperty = null;
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value));
            else
            {
                accessor.Set(null, value);
                result = accessor.Get(null);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            UnsafeTestClass.StaticRefProperty = null;
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetStaticValue(value));
            else
            {
                // ReSharper disable once PossibleInvalidCastException
                accessor.SetStaticValue((IntPtr)value);
                result = accessor.GetStaticValue<IntPtr>();
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefProperty), testType));
                AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefProperty), testType));
            }

            UnsafeTestClass.StaticRefProperty = null;
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(null, pi, value));
            else
            {
                Reflector.SetProperty(null, pi, value);
                result = Reflector.GetProperty(null, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            UnsafeTestClass.StaticRefProperty = null;
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty), value));
            else
            {
                Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty), value);
                result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
            }
        }

        [Test]
        public unsafe void ClassStaticRefReadonlyPropertyAccessUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestClass.StaticRefReadonlyProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            object value = new IntPtr(1);

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(null, value, null);
#else
            UnsafeTestClass.StaticField = (void*)(IntPtr)value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = (IntPtr)Pointer.Unbox(pi.GetValue(null, null));
#else
            result = (IntPtr)UnsafeTestClass.StaticRefReadonlyProperty;
#endif
            AssertAreEqual(value, result);

            UnsafeTestClass.StaticField = null;
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(null, value, Reflector.EmptyObjects);
                result = accessor.Get(null, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(null, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(null, value));
            else
            {
                accessor.Set(null, value);
                result = accessor.Get(null);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetStaticValue((IntPtr)value));
            else
            {
                // ReSharper disable once PossibleInvalidCastException
                accessor.SetStaticValue((IntPtr)value);
                result = accessor.GetStaticValue<IntPtr>();
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefReadonlyProperty), testType));
                AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestClass.StaticRefReadonlyProperty), testType));
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(null, pi, value));
            else
            {
                Reflector.SetProperty(null, pi, value);
                result = Reflector.GetProperty(null, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty), value));
            else
            {
                Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty), value);
                result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestClass.StaticRefReadonlyProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
            }
        }

        [Test]
        public unsafe void ClassInstanceIndexerPtrParamAccessUnsafe()
        {
            var test = new UnsafeTestClass(null);
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(void*)]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = new IntPtr(1), index = new IntPtr(42);
            object[] indexParameters = [index];

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = pi.GetValue(test, indexParameters);
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, indexParameters);
            result = accessor.Get(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value, index);
            result = accessor.Get(test, index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, (IntPtr)value, (IntPtr)index);
            result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr, IntPtr>(test, (IntPtr)index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, (IntPtr)value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int, IntPtr>(test, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, IntPtr, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1), Res.ReflectionIndexerNotFound(test.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 1), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        [Test]
        public unsafe void ClassInstanceIndexerRefPtrParamAccessUnsafe()
        {
            var test = new UnsafeTestClass(null);
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(void*).MakeByRefType()]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = new IntPtr(1), index = new IntPtr(42);
            object[] indexParameters = [index];

            Console.Write("Direct Access...");
            void* ptrIndex = ((IntPtr)index).ToPointer();
            test[in ptrIndex] = (IntPtr)value;
            result = test[in ptrIndex];
            AssertAreEqual(value, result);

            // System Reflection does not support initializing the ref pointer parameter - ArgumentException: Object of type 'System.IntPtr' cannot be converted to type 'System.Void*&'
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = pi.GetValue(test, indexParameters);
            AssertAreEqual(value, result);
#endif

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters));
            else
            {
                accessor.Set(test, value, indexParameters);
                result = accessor.Get(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, index));
            else
            {
                accessor.Set(test, value, index);
                result = accessor.Get(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, (IntPtr)value, (IntPtr)index));
            else
            {
                accessor.SetInstanceValue(test, (IntPtr)value, (IntPtr)index);
                result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr, IntPtr>(test, (IntPtr)index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, (IntPtr)value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int, IntPtr>(test, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, IntPtr, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters));
            else
            {
                Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
                result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            // not testing Reflector.SetIndexedMember because the pointer indexers are ambiguous by IntPtr index, and may find the other one
        }

        [Test]
        public unsafe void ClassInstanceIndexerPtrReturnAccessUnsafe()
        {
            var test = new UnsafeTestClass(null);
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(IntPtr)]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = new IntPtr(1), index = new IntPtr(42);
            object[] indexParameters = [index];

            Console.Write("Direct Access...");
            test[(IntPtr)index] = ((IntPtr)value).ToPointer();
            result = (IntPtr)test[(IntPtr)index];
            AssertAreEqual(value, result);

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, indexParameters));
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, indexParameters);
            result = accessor.Get(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value, index);
            result = accessor.Get(test, index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(test, (IntPtr)value, (IntPtr)index);
            result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr, IntPtr>(test, (IntPtr)index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, (IntPtr)value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int, IntPtr>(test, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, IntPtr, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1), Res.ReflectionIndexerNotFound(test.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 1), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        [Test]
        public unsafe void ClassInstanceIndexerRefPtrReturnAccessUnsafe()
        {
            var test = new UnsafeTestClass();
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(int*)]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            var index = new IntPtr(42);
            object[] indexParameters = [index];
            object result;
            var value = new IntPtr(13);

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, indexParameters);
#else
            test[(int*)index] = (int*)value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, indexParameters));
#else
            result = (IntPtr)test[(void*)index];
#endif
            AssertAreEqual(value, result);

            test = new UnsafeTestClass();
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters));
            else
            {
                accessor.Set(test, value, indexParameters);
                result = accessor.Get(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            test = new UnsafeTestClass();
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, index));
            else
            {
                accessor.Set(test, value, index);
                result = accessor.Get(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            }

            test = new UnsafeTestClass();
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value, index));
            else
            {
                accessor.SetInstanceValue(test, value, index);
                result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr, IntPtr>(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, IntPtr, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int, IntPtr>(test, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            test = new UnsafeTestClass();
            Console.Write("Reflector (by PropertyInfo)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters));
            else
            {
                Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
                result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            // not testing Reflector.SetIndexedMember because the two pointer indexers are ambiguous by IntPtr index, and would find the other one
        }

        [Test]
        public unsafe void ClassInstanceRefReturnRefParamIndexerAccessUnsafe()
        {
            var test = new UnsafeTestClass();
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(int*).MakeByRefType()]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            var index = new IntPtr(42);
            object[] indexParameters;
            object result;
            var value = new IntPtr(13);

            Console.Write("Direct access...");
            var ptrIndex = (int*)index;
            test[in ptrIndex] = value.ToPointer();
            result = (IntPtr)test[in ptrIndex];
            AssertAreEqual(value, result);

            Console.Write("System Reflection...");
            // System Reflection does not support initializing the ref pointer parameter - ArgumentException: Object of type 'System.IntPtr' cannot be converted to type 'System.Void*&'
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            test = new UnsafeTestClass();
            indexParameters = [index];
            pi.SetValue(test, value, indexParameters);
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, indexParameters));
            AssertAreEqual(value, result);
#endif

            test = new UnsafeTestClass();
            indexParameters = [index];
            Console.Write("Property Accessor General...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters));
            else
            {
                accessor.Set(test, value, indexParameters);
                result = accessor.Get(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            test = new UnsafeTestClass();
            Console.Write("Property Accessor NonGeneric...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, index));
            else
            {
                accessor.Set(test, value, index);
                result = accessor.Get(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            }

            test = new UnsafeTestClass();
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(test, value, index));
            else
            {
                accessor.SetInstanceValue(test, value, index);
                result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr, IntPtr>(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, IntPtr, int>(test, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int, IntPtr>(test, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            test = new UnsafeTestClass();
            Console.Write("Reflector (by PropertyInfo)...");
            indexParameters = [index];
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters));
            else
            {
                Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
                result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            // not testing Reflector.SetIndexedMember because the pointer indexers are ambiguous by IntPtr index, and may find the other one
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
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestStruct(0);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testStruct = new TestStruct(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, (int)value);
            result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.IntProp), pi.DeclaringType!));

            test = new TestStruct(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(TestStruct.IntProp), value);
            result = Reflector.GetProperty(test, nameof(TestStruct.IntProp));
            AssertAreEqual(value, result);
            Reflector.SetProperty(test, nameof(TestStruct.IntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(TestStruct.IntProp).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestStruct.IntProp), value), Res.ArgumentNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestStruct.IntProp)), Res.ArgumentNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestStruct.IntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestStruct.IntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.IntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestStruct.IntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestStruct.IntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.IntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
        }

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
            AssertAreEqual(value, result);

            Console.Write("Property Accessor General...");
            ((TestStruct)test).RefIntProperty = 0;
            test = new TestStruct(0);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            }

            Console.Write("Property Accessor NonGeneric...");
            ((TestStruct)test).RefIntProperty = 0;
            test = new TestStruct(0);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
            }

            Console.Write("Property Accessor Generic...");
            ((TestStruct)test).RefIntProperty = 0;
            var testStruct = new TestStruct(0);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testStruct, value));
            else
            {
                accessor.SetInstanceValue(testStruct, value);
                result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefIntProperty), pi.DeclaringType!));
            }

            Console.Write("Reflector (by PropertyInfo)...");
            ((TestStruct)test).RefIntProperty = 1;
            test = new TestStruct(0);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));
            }

            Console.Write("Reflector (by name)...");
            ((TestStruct)test).RefIntProperty = 1;
            test = new TestStruct(0);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty), value);
                result = Reflector.GetProperty(test, nameof(TestStruct.RefIntProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(TestStruct.RefIntProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestStruct.RefIntProperty), value), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestStruct.RefIntProperty)), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty), null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestStruct.RefIntProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefIntProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefIntProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestStruct.RefIntProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefIntProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
            }
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
            AssertAreEqual(value, result);

            Console.Write("Property Accessor General...");
            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, Reflector.EmptyObjects), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), Reflector.EmptyObjects), Res.NotAnInstanceOfType(test.GetType()));
            }

            Console.Write("Property Accessor NonGeneric...");
            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));
            }

            Console.Write("Property Accessor Generic...");
            var testStruct = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testStruct, (int)value));
            else
            {
                accessor.SetInstanceValue(testStruct, (int)value);
                result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.RefReadonlyProperty), pi.DeclaringType!));
            }

            Console.Write("Reflector (by PropertyInfo)...");
            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi), Res.NotAnInstanceOfType(test.GetType()));
            }

            Console.Write("Reflector (by name)...");
            test = new TestStruct(0);
            TestStruct.StaticIntField = 0;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), value);
                Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), value);
                result = Reflector.GetProperty(test, nameof(TestStruct.RefReadonlyProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(TestStruct.RefReadonlyProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(instance: null!, nameof(TestStruct.RefReadonlyProperty), value), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(instance: null!, nameof(TestStruct.RefReadonlyProperty)), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), null), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(new object(), nameof(TestStruct.RefReadonlyProperty), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefReadonlyProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.SetProperty(test, nameof(TestStruct.StaticIntProp), value), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(TestStruct.RefReadonlyProperty), "1"), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(new object(), nameof(TestStruct.RefReadonlyProperty)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.RefReadonlyProperty), typeof(object)));
                AssertThrows<ReflectionException>(() => Reflector.GetProperty(test, nameof(TestStruct.StaticIntProp)), Res.ReflectionInstancePropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(TestStruct)));
            }
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
            AssertAreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(null, "1", Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntProp = 0;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntProp = 0;
            var testStruct = new TestStruct();
            Console.Write("Property Accessor Generic...");
            PropertyAccessor.GetAccessor(pi).SetStaticValue((int)value);
            result = PropertyAccessor.GetAccessor(pi).GetStaticValue<int>();
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetInstanceValue(testStruct, value), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestStruct.StaticIntProp), testType));
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.StaticIntProp), testType));
            AssertThrows<InvalidOperationException>(() => accessor.GetInstanceValue<TestStruct, int>(testStruct), Res.ReflectionInstancePropertyExpectedGeneric(nameof(TestStruct.StaticIntProp), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(TestStruct.StaticIntProp), testType));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntProp = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), value);
            result = Reflector.GetProperty(testType, nameof(TestStruct.StaticIntProp));
            AssertAreEqual(value, result);
            Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(TestStruct.StaticIntProp).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(type: null!, nameof(TestStruct.StaticIntProp), value), Res.ArgumentNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.SetProperty(Reflector.ObjectType, nameof(TestStruct.StaticIntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.SetProperty(testType, nameof(TestStruct.IntProp), value), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.IntProp), testType));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(TestStruct.StaticIntProp), "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(type: null!, nameof(TestStruct.StaticIntProp)), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(Reflector.ObjectType, nameof(TestStruct.StaticIntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.StaticIntProp), typeof(object)));
            AssertThrows<ReflectionException>(() => Reflector.GetProperty(testType, nameof(TestStruct.IntProp)), Res.ReflectionStaticPropertyDoesNotExist(nameof(TestStruct.IntProp), testType));
        }

        [Test]
        public void StructInstanceIndexerAccess()
        {
            object test = new TestStruct(0);
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(int)]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = 1, index = 42;
            object[] indexParameters = [index];

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = pi.GetValue(test, indexParameters);
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            accessor.Set(test, value, indexParameters);
            Console.Write("Property Accessor General...");
            result = accessor.Get(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, ["1"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection does not tolerate more parameters than needed
                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { 1, "2" }), "More parameters should be alright");
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => accessor.Get(test, ["1"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection does not tolerate more parameters than needed
                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { 1, "2" }), "More parameters should be alright");

            test = new TestStruct(0);
            accessor.Set(test, value, index);
            Console.Write("Property Accessor NonGeneric...");
            result = accessor.Get(test, index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null, index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1", index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, "1"), Res.NotAnInstanceOfType(typeof(int)));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Get(test, "1"), Res.NotAnInstanceOfType(typeof(int)));

            var testStruct = new TestStruct(0);
            Console.Write("Property Accessor Generic...");
            accessor.SetInstanceValue(testStruct, (int)value, (int)index);
            result = accessor.GetInstanceValue<TestStruct, int, int>(testStruct, (int)index);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1", 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int, int>(new object(), 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, int>(testStruct), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, int, string>(testStruct, "1"), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));

            test = new TestStruct(0);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));
            AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, "1"), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            test = new TestStruct(0);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
            AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, null, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, "1", indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
            AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
            AssertThrows<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        [Test]
        public void StructInstanceRefIndexerAccess()
        {
            object test = new TestStruct();
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(string)])!;
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            string index = "x";
            object[] indexParameters = [index];
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
            AssertAreEqual(value, result);

            Console.Write("Property Accessor General...");
            test = new TestStruct();
            TestStruct.StaticStringField = default;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters));
            else
            {
                accessor.Set(test, value, indexParameters);
                result = accessor.Get(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, value, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                Assert.DoesNotThrow(() => accessor.Set(test, value, new object[] { "1", 2 }), "More parameters than needed are okay");
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => accessor.Get(test, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Get(test, Reflector.EmptyObjects), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                Assert.DoesNotThrow(() => accessor.Get(test, new object[] { "1", 2 }), "More parameters than needed are okay");
            }

            Console.Write("Property Accessor NonGeneric...");
            test = new TestStruct(0);
            TestStruct.StaticStringField = default;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, index));
            else
            {
                accessor.Set(test, value, index);
                result = accessor.Get(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, value, index), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Set(test, value), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                AssertThrows<ArgumentException>(() => accessor.Set(new object(), value, index), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(string)));
                AssertThrows<ArgumentNullException>(() => accessor.Get(null, index), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentException>(() => accessor.Get(test), Res.ReflectionIndexerParamsLengthMismatch(1, 0));
                AssertThrows<ArgumentException>(() => accessor.Get(new object(), index), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(string)));
            }

            Console.Write("Property Accessor Generic...");
            var testStruct = new TestStruct();
            TestStruct.StaticStringField = default;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testStruct, value, index));
            else
            {
                accessor.SetInstanceValue(testStruct, value, index);
                result = accessor.GetInstanceValue<TestStruct, string, string>(testStruct, index);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(1), Res.ReflectionStaticPropertyExpectedGeneric(pi.Name, pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), value, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, value), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, string, string>(new object(), index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, string>(testStruct), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<TestStruct, string, int>(testStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            Console.Write("Reflector (by PropertyInfo)...");
            test = new TestStruct();
            TestStruct.StaticStringField = default;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters));
            else
            {
                Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
                result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(null, pi, value, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(null, pi, indexParameters), Res.ReflectionInstanceIsNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetProperty(test, pi, value, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(new object(), pi, value, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
                AssertThrows<ArgumentNullException>(() => Reflector.GetProperty(test, pi, null), Res.ArgumentNull);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(new object(), pi, indexParameters), Res.NotAnInstanceOfType(test.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi), Res.ReflectionEmptyIndices);
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(string)));
            }

            Console.Write("Reflector (by parameters match)...");
            test = new TestStruct();
            TestStruct.StaticStringField = default;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetIndexedMember(test, value, indexParameters));
            else
            {
                Reflector.SetIndexedMember(test, value, indexParameters);
                result = Reflector.GetIndexedMember(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(null, value, indexParameters), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(null, indexParameters), Res.ArgumentNull);
                AssertThrows<ArgumentNullException>(() => Reflector.SetIndexedMember(test, value, null), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(new object(), value, indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
                AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, value), Res.ReflectionEmptyIndices);
                AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
                AssertThrows<ArgumentNullException>(() => Reflector.GetIndexedMember(test, null), Res.ArgumentNull);
                AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(new object(), indexParameters), Res.ReflectionIndexerNotFound(Reflector.ObjectType));
                AssertThrows<ArgumentException>(() => Reflector.GetIndexedMember(test), Res.ReflectionEmptyIndices);
                AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 'x'), Res.ReflectionIndexerNotFound(test.GetType()));
            }
        }

        #endregion

        #region Struct property access (unsafe)

        [Test]
        public unsafe void StructInstancePropertyAccessUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestStruct.InstanceProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = new IntPtr(1);

            Console.Write("System Reflection...");
            pi.SetValue(test, value, null);
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, null));
            AssertAreEqual(value, result);

            test = new UnsafeTestStruct(null);
            Console.Write("Property Accessor General...");
            accessor.Set(test, value, Reflector.EmptyObjects);
            result = accessor.Get(test, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestStruct(null);
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(unsafeTestStruct, (IntPtr)value));
            else
            {
                accessor.SetInstanceValue(unsafeTestStruct, (IntPtr)value);
                result = accessor.GetInstanceValue<UnsafeTestStruct, IntPtr>(unsafeTestStruct);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(unsafeTestStruct, 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.InstanceProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(unsafeTestStruct), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.InstanceProperty), pi.DeclaringType!));
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value);
            result = Reflector.GetProperty(test, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(test, nameof(UnsafeTestStruct.InstanceProperty), value);
            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.InstanceProperty));
            AssertAreEqual(value, result);
            Reflector.SetProperty(test, nameof(UnsafeTestStruct.InstanceProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.InstanceProperty).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.InstanceProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
        }

        [Test]
        public unsafe void StructInstanceRefPropertyAccessUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestStruct.RefProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, null);
#else
            ((UnsafeTestStruct)test).RefProperty = (int*)value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, null));
#else
            result = (IntPtr)((UnsafeTestStruct)test).RefProperty;
#endif
            AssertAreEqual(value, result);

            Console.Write("Property Accessor General...");
            ((UnsafeTestStruct)test).RefProperty = null;
            test = new UnsafeTestStruct(null);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            Console.Write("Property Accessor NonGeneric...");
            ((UnsafeTestStruct)test).RefProperty = null;
            test = new UnsafeTestStruct(null);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            Console.Write("Property Accessor Generic...");
            ((UnsafeTestStruct)test).RefProperty = null;
            var unsafeTestStruct = new UnsafeTestStruct(null);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(unsafeTestStruct, value));
            else
            {
                accessor.SetInstanceValue(unsafeTestStruct, value);
                result = accessor.GetInstanceValue<UnsafeTestStruct, IntPtr>(unsafeTestStruct);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefProperty), pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.RefProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(unsafeTestStruct), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefProperty), pi.DeclaringType!));
            }

            Console.Write("Reflector (by PropertyInfo)...");
            ((UnsafeTestStruct)test).RefProperty = null;
            test = new UnsafeTestStruct(null);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            Console.Write("Reflector (by name)...");
            ((UnsafeTestStruct)test).RefProperty = null;
            test = new UnsafeTestStruct(null);
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefProperty), value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
            }
        }

        [Test]
        public unsafe void StructInstanceRefReadonlyPropertyAccessUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            PropertyInfo pi = test.GetType().GetProperty(nameof(UnsafeTestStruct.RefReadonlyProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = new IntPtr(1);

            if (!EnvironmentHelper.IsMono) // (IntPtr)((UnsafeTestStruct)test).RefReadonlyProperty returns some random value on Mono
            {
                Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
                pi.SetValue(test, value, null);
#else
                typeof(UnsafeTestStruct).GetField(nameof(UnsafeTestStruct.StaticField))!.SetValue(null, value);
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
                result = (IntPtr)Pointer.Unbox(pi.GetValue(test, null));
#else
                result = (IntPtr)((UnsafeTestStruct)test).RefReadonlyProperty;
#endif
                AssertAreEqual(value, result);
            }

            Console.Write("Property Accessor General...");
            test = new UnsafeTestStruct(null);
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, Reflector.EmptyObjects));
            else
            {
                accessor.Set(test, value, Reflector.EmptyObjects);
                result = accessor.Get(test, Reflector.EmptyObjects);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));
            }

            Console.Write("Property Accessor NonGeneric...");
            test = new UnsafeTestStruct(null);
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value));
            else
            {
                accessor.Set(test, value);
                result = accessor.Get(test);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            Console.Write("Property Accessor Generic...");
            var unsafeTestStruct = new UnsafeTestStruct(null);
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(unsafeTestStruct, (IntPtr)value));
            else
            {
                accessor.SetInstanceValue(unsafeTestStruct, (IntPtr)value);
                result = accessor.GetInstanceValue<UnsafeTestStruct, IntPtr>(unsafeTestStruct);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticPropertyExpectedGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(unsafeTestStruct), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.RefReadonlyProperty), pi.DeclaringType!));
            }

            Console.Write("Reflector (by PropertyInfo)...");
            test = new UnsafeTestStruct(null);
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value));
            else
            {
                Reflector.SetProperty(test, pi, value);
                result = Reflector.GetProperty(test, pi);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1), Res.NotAnInstanceOfType(value.GetType()));
            }

            test = new UnsafeTestStruct(null);
            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by name)...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty), value));
            else
            {
                Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty), value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty));
                AssertAreEqual(value, result);
                Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty).ToLowerInvariant(), true, value);
                result = Reflector.GetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty).ToLowerInvariant(), true);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, nameof(UnsafeTestStruct.RefReadonlyProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
            }
        }

        [Test]
        public unsafe void StructStaticPropertyAccessUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            PropertyInfo pi = testType.GetProperty(nameof(UnsafeTestStruct.StaticProperty));
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = new IntPtr(1);

            Console.Write("System Reflection...");
            pi.SetValue(null, value, null);
            result = (IntPtr)Pointer.Unbox(pi.GetValue(null, null));
            AssertAreEqual(value, result);

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Property Accessor General...");
            accessor.Set(null, value, Reflector.EmptyObjects);
            result = accessor.Get(null, Reflector.EmptyObjects);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1, Reflector.EmptyObjects), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Property Accessor NonGeneric...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Property Accessor Generic...");
            PropertyAccessor.GetAccessor(pi).SetStaticValue((IntPtr)value);
            result = PropertyAccessor.GetAccessor(pi).GetStaticValue<IntPtr>();
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.StaticProperty), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokePropertyGeneric(nameof(UnsafeTestStruct.StaticProperty), testType));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(null, pi, value);
            result = Reflector.GetProperty(null, pi);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(null, pi, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestStruct.StaticProperty = null;
            Console.Write("Reflector (by name)...");
            Reflector.SetProperty(testType, nameof(UnsafeTestStruct.StaticProperty), value);
            result = Reflector.GetProperty(testType, nameof(UnsafeTestStruct.StaticProperty));
            AssertAreEqual(value, result);
            Reflector.SetProperty(testType, nameof(UnsafeTestStruct.StaticProperty).ToLowerInvariant(), true, value);
            result = Reflector.GetProperty(testType, nameof(UnsafeTestStruct.StaticProperty).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(testType, nameof(UnsafeTestStruct.StaticProperty), 1), Res.NotAnInstanceOfType(value.GetType()));
        }

        [Test]
        public unsafe void StructInstanceIndexerAccessUnsafe()
        {
            object test = new UnsafeTestStruct(null);
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(void*)]);
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            object result, value = new IntPtr(1), index = new IntPtr(42);
            object[] indexParameters = [index];

            Console.Write("System Reflection...");
            pi.SetValue(test, value, indexParameters);
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, indexParameters));
            AssertAreEqual(value, result);

            test = new UnsafeTestStruct(null);
            accessor.Set(test, value, indexParameters);
            Console.Write("Property Accessor General...");
            result = accessor.Get(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestStruct(null);
            accessor.Set(test, value, index);
            Console.Write("Property Accessor NonGeneric...");
            result = accessor.Get(test, index);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            var unsafeTestStruct = new UnsafeTestStruct(null);
            Console.Write("Property Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(unsafeTestStruct, (IntPtr)value, (IntPtr)index));
            else
            {
                accessor.SetInstanceValue(unsafeTestStruct, (IntPtr)value, (IntPtr)index);
                result = accessor.GetInstanceValue<UnsafeTestStruct, IntPtr, IntPtr>(unsafeTestStruct, (IntPtr)index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(unsafeTestStruct, 1, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(unsafeTestStruct, (IntPtr)value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int, IntPtr>(unsafeTestStruct, (IntPtr)index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, IntPtr, int>(unsafeTestStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by PropertyInfo)...");
            Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
            result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            test = new UnsafeTestStruct(null);
            Console.Write("Reflector (by parameters match)...");
            Reflector.SetIndexedMember(test, value, indexParameters);
            result = Reflector.GetIndexedMember(test, indexParameters);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => Reflector.SetIndexedMember(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.SetIndexedMember(test, value, 1), Res.ReflectionIndexerNotFound(test.GetType()));
            AssertThrows<ReflectionException>(() => Reflector.GetIndexedMember(test, 1), Res.ReflectionIndexerNotFound(test.GetType()));
        }

        [Test]
        public unsafe void StructInstanceRefIndexerAccessUnsafe()
        {
            object test = new UnsafeTestStruct();
            PropertyInfo pi = test.GetType().GetProperty("Item", [typeof(long*)])!;
            PropertyAccessor accessor = PropertyAccessor.GetAccessor(pi);
            var index = new IntPtr(42);
            object[] indexParameters = [index];
            object result;
            var value = new IntPtr(13);

            Console.Write("System Reflection...");
#if NET11_0_OR_GREATER // ArgumentException : Property set method not found.
            pi.SetValue(test, value, indexParameters);
#else
            ((UnsafeTestStruct)test)[(long*)index] = (int*)value;
#endif
#if NETCOREAPP3_0_OR_GREATER // NotSupportedException : ByRef return value not supported in reflection invocation.
            result = (IntPtr)Pointer.Unbox(pi.GetValue(test, indexParameters));
#else
            result = (IntPtr)((UnsafeTestStruct)test)[(long*)index];
#endif
            AssertAreEqual(value, result);

            Console.Write("Property Accessor General...");
            test = new UnsafeTestStruct();
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, indexParameters));
            else
            {
                accessor.Set(test, value, indexParameters);
                result = accessor.Get(test, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, [1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            Console.Write("Property Accessor NonGeneric...");
            test = new UnsafeTestStruct();
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.Set(test, value, index));
            else
            {
                accessor.Set(test, value, index);
                result = accessor.Get(test, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.Set(test, 1, index), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => accessor.Set(test, value, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => accessor.Get(test, 1), Res.NotAnInstanceOfType(typeof(IntPtr)));
            }

            Console.Write("Property Accessor Generic...");
            var unsafeTestStruct = new UnsafeTestStruct();
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(unsafeTestStruct, value, index));
            else
            {
                accessor.SetInstanceValue(unsafeTestStruct, value, index);
                result = accessor.GetInstanceValue<UnsafeTestStruct, IntPtr, IntPtr>(unsafeTestStruct, index);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(unsafeTestStruct, 1, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(unsafeTestStruct, value, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, IntPtr, int>(unsafeTestStruct, 1), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int, IntPtr>(unsafeTestStruct, index), Res.ReflectionCannotInvokePropertyGeneric("Item", pi.DeclaringType!));
            }

            Console.Write("Reflector (by PropertyInfo)...");
            test = new UnsafeTestStruct();
            UnsafeTestStruct.StaticField = null;
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters));
            else
            {
                Reflector.SetProperty(test, pi, value, ReflectionWays.Auto, indexParameters);
                result = Reflector.GetProperty(test, pi, ReflectionWays.Auto, indexParameters);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, 1, indexParameters), Res.NotAnInstanceOfType(value.GetType()));
                AssertThrows<ArgumentException>(() => Reflector.SetProperty(test, pi, value, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
                AssertThrows<ArgumentException>(() => Reflector.GetProperty(test, pi, 1), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));
            }

            // not testing Reflector.SetIndexedMember because the two pointer indexers are ambiguous by IntPtr index, and would find the other one
        }

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
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = accessor.GetInstanceValue<TestClass, int>(test);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.IntField), fi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.IntField), fi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestClass.ReadOnlyValueField), value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyValueField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
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
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = accessor.GetInstanceValue<TestClass, int>(test);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, 1), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, int>(null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyValueField), fi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestClass.ReadOnlyValueField), value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyValueField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyValueField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
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
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = FieldAccessor.GetAccessor(fi).Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            test = new TestClass(0);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = accessor.GetInstanceValue<TestClass, string>(test);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.SetInstanceValue((TestClass)null, value), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));
            AssertThrows<ArgumentNullException>(() => accessor.GetInstanceValue<TestClass, string>(null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, string>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.ReadOnlyReferenceField), fi.DeclaringType!));

            test = new TestClass(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new TestClass(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestClass.ReadOnlyReferenceField), value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyReferenceField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(TestClass.ReadOnlyReferenceField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestClass.ReadOnlyReferenceField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
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
            AssertAreEqual(value, result);

            TestClass.StaticIntField = 0;
            Console.Write("Field Accessor...");
            accessor.Set(null, value);
            result = FieldAccessor.GetAccessor(fi).Get(null);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestClass.StaticIntField = 0;
            Console.Write("Field Accessor Generic...");
            accessor.SetStaticValue(value);
            result = accessor.GetStaticValue<int>();
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetInstanceValue(new TestClass(), value), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestClass.StaticIntField), testType));
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.StaticIntField), testType));
            AssertThrows<InvalidOperationException>(() => accessor.GetInstanceValue<TestClass, int>(new TestClass()), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestClass.StaticIntField), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestClass.StaticIntField), testType));

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            AssertAreEqual(value, result);

            TestClass.StaticIntField = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, nameof(TestClass.StaticIntField), value);
            result = Reflector.GetField(testType, nameof(TestClass.StaticIntField));
            AssertAreEqual(value, result);
            Reflector.SetField(testType, nameof(TestClass.StaticIntField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(testType, nameof(TestClass.StaticIntField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        [Test]
        public void ClassStaticNonPrimitiveFieldGet()
        {
            // NOTE: It's important that here we use a type not used anywhere else (StaticTestClass*), and that unlike in the normal test cases, 
            // we don't initialize StaticTestClass*.DecimalField and don't use system reflection first.
            // It's because this test covers a rare use case that occurs in the .NET Runtime 2.0 only, which cannot be reproduced once the type is initialized.
            // The issue occurs for uninitialized types only, (obtaining the type by typeof() leaves the type uninitialized) when accessing a static field of a non-primitive type.
            // In that case the generated accessors may throw a NullReferenceException for the first time, which goes away once the type gets initialized.
            Type testType = typeof(StaticTestClassGet);
            FieldInfo fi = testType.GetField(nameof(StaticTestClassGet.DecimalField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            
            Assert.DoesNotThrow(() => accessor.Get(null));
        }

        [Test]
        public void ClassStaticNonPrimitiveFieldSet()
        {
            // NOTE: It's important that here we use a type that is not used in other tests; otherwise, the type may be initialized by other test cases.
            // See the comments in ClassStaticNonPrimitiveFieldGet for more details.
            Type testType = typeof(StaticTestClassSet);
            FieldInfo fi = testType.GetField(nameof(StaticTestClassSet.DecimalField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object value = 1m;

            Assert.DoesNotThrow(() => accessor.Set(null, value));
            AssertAreEqual(value, accessor.Get(null));
        }

        [Test]
        public void ClassStaticNonPrimitiveFieldGetGeneric()
        {
            // NOTE: It's important that here we use a type not used anywhere else (StaticTestClass*), and that unlike in the normal test cases, 
            // we don't initialize StaticTestClass*.DecimalField and don't use system reflection first.
            // It's because this test covers a rare use case that occurs in the .NET Runtime 2.0 only, which cannot be reproduced once the type is initialized.
            // The issue occurs for uninitialized types only, (obtaining the type by typeof() leaves the type uninitialized) when accessing a static field of a non-primitive type.
            // In that case the generated accessors may throw a NullReferenceException for the first time, which goes away once the type gets initialized.
            Type testType = typeof(StaticTestClassGetGeneric);
            FieldInfo fi = testType.GetField(nameof(StaticTestClassGetGeneric.DecimalField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            
            Assert.DoesNotThrow(() => accessor.GetStaticValue<decimal>());
        }

        [Test]
        public void ClassStaticNonPrimitiveFieldSetGeneric()
        {
            // NOTE: It's important that here we use a type that is not used in other tests; otherwise, the type may be initialized by other test cases.
            // See the comments in ClassStaticNonPrimitiveFieldGet for more details.
            Type testType = typeof(StaticTestClassSetGeneric);
            FieldInfo fi = testType.GetField(nameof(StaticTestClassSetGeneric.DecimalField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            decimal value = 1m;

            Assert.DoesNotThrow(() => accessor.SetStaticValue(value));
            AssertAreEqual(value, accessor.Get(null));
        }

        #endregion

        #region Class field access (unsafe)

        [Test]
        public unsafe void ClassInstanceFieldAccessUnsafe()
        {
            var test = new UnsafeTestClass(null);
            FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestClass.InstanceField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = (IntPtr)Pointer.Unbox(fi.GetValue(test));
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr>(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.InstanceField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(test), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.InstanceField), fi.DeclaringType!));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(UnsafeTestClass.InstanceField), value);
            result = Reflector.GetField(test, nameof(UnsafeTestClass.InstanceField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(UnsafeTestClass.InstanceField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(UnsafeTestClass.InstanceField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        [Test]
        public unsafe void ClassInstanceFunctionPointerFieldAccessUnsafe()
        {
            if (EnvironmentHelper.IsMono)
                Assert.Inconclusive("This test would crash on Mono");
            var test = new UnsafeTestClass(null);
            FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestClass.InstanceFunctionPointerField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            IntPtr value = (IntPtr)(delegate*<string, void>)&Console.WriteLine;

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = (IntPtr)fi.GetValue(test)!;
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr>(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.InstanceFunctionPointerField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(test), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.InstanceFunctionPointerField), fi.DeclaringType!));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(UnsafeTestClass.InstanceFunctionPointerField), value);
            result = Reflector.GetField(test, nameof(UnsafeTestClass.InstanceFunctionPointerField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(UnsafeTestClass.InstanceFunctionPointerField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(UnsafeTestClass.InstanceFunctionPointerField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        [Test]
        public unsafe void ClassInstanceReadOnlyValueFieldAccessUnsafe()
        {
            var test = new UnsafeTestClass(null);
            FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestClass.ReadOnlyInstanceField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = (IntPtr)Pointer.Unbox(fi.GetValue(test));
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            test = new UnsafeTestClass(null);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(test, value);
            result = accessor.GetInstanceValue<UnsafeTestClass, IntPtr>(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(test, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyInstanceField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestClass, int>(test), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.ReadOnlyInstanceField), fi.DeclaringType!));

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new UnsafeTestClass(null);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyInstanceField), value);
            result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyInstanceField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(UnsafeTestClass.ReadOnlyInstanceField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(UnsafeTestClass.ReadOnlyInstanceField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        [Test]
        public unsafe void ClassStaticFieldAccessUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            FieldInfo fi = testType.GetField(nameof(UnsafeTestClass.StaticField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
            fi.SetValue(null, value);
            result = (IntPtr)Pointer.Unbox(fi.GetValue(null));
            AssertAreEqual(value, result);

            UnsafeTestClass.StaticField = null;
            Console.Write("Field Accessor...");
            accessor.Set(null, value);
            result = FieldAccessor.GetAccessor(fi).Get(null);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticField = null;
            Console.Write("Field Accessor Generic...");
            accessor.SetStaticValue(value);
            result = accessor.GetStaticValue<IntPtr>();
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.StaticField), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.StaticField), testType));

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            AssertAreEqual(value, result);

            UnsafeTestClass.StaticField = null;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, nameof(UnsafeTestClass.StaticField), value);
            result = Reflector.GetField(testType, nameof(UnsafeTestClass.StaticField));
            AssertAreEqual(value, result);
            Reflector.SetField(testType, nameof(UnsafeTestClass.StaticField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(testType, nameof(UnsafeTestClass.StaticField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        [Test]
        public unsafe void ClassStaticFunctionPointerFieldAccessUnsafe()
        {
            if (EnvironmentHelper.IsMono)
                Assert.Inconclusive("This test would crash on Mono");
            Type testType = typeof(UnsafeTestClass);
            FieldInfo fi = testType.GetField(nameof(UnsafeTestClass.StaticFunctionPointerField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            IntPtr value = (IntPtr)(delegate*<string, void>)&Console.WriteLine;

            Console.Write("System Reflection...");
            fi.SetValue(null, value);
            result = (IntPtr)fi.GetValue(null)!;
            AssertAreEqual(value, result);

            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Field Accessor...");
            accessor.Set(null, value);
            result = FieldAccessor.GetAccessor(fi).Get(null);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Field Accessor Generic...");
            accessor.SetStaticValue(value);
            result = accessor.GetStaticValue<IntPtr>();
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.StaticFunctionPointerField), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestClass.StaticFunctionPointerField), testType));

            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            AssertAreEqual(value, result);

            UnsafeTestClass.StaticFunctionPointerField = null;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, nameof(UnsafeTestClass.StaticFunctionPointerField), value);
            result = Reflector.GetField(testType, nameof(UnsafeTestClass.StaticFunctionPointerField));
            AssertAreEqual(value, result);
            Reflector.SetField(testType, nameof(UnsafeTestClass.StaticFunctionPointerField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(testType, nameof(UnsafeTestClass.StaticFunctionPointerField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
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
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testStruct = new TestStruct(0);
            Console.Write("Field Accessor Generic...");
            accessor.SetInstanceValue(testStruct, value);
            result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));
            AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.IntField), fi.DeclaringType!));

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestStruct.IntField), value);
            result = Reflector.GetField(test, nameof(TestStruct.IntField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(TestStruct.IntField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestStruct.IntField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
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
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            if (TestedFramework != TargetFramework.NetStandard20 && !IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(test, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, "1"), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testStruct = new TestStruct(0);
            Console.Write("Field Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testStruct, value));
            else
            {
                accessor.SetInstanceValue(testStruct, value);
                result = accessor.GetInstanceValue<TestStruct, int>(testStruct);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, "1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<int>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, int>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyValueField), fi.DeclaringType!));
            }

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyValueField), value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyValueField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyValueField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyValueField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
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
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentNullException>(() => accessor.Set(null, value), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Set(new object(), value), Res.NotAnInstanceOfType(test.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentNullException>(() => accessor.Get(null), Res.ReflectionInstanceIsNull);
            AssertThrows<ArgumentException>(() => accessor.Get(new object()), Res.NotAnInstanceOfType(test.GetType()));

            var testStruct = new TestStruct(0);
            Console.Write("Field Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(testStruct, value));
            else
            {
                accessor.SetInstanceValue(testStruct, value);
                result = accessor.GetInstanceValue<TestStruct, string>(testStruct);
                AssertAreEqual(value, result);
                AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(value), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(new object(), value), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(testStruct, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                AssertThrows<InvalidOperationException>(() => accessor.GetStaticValue<string>(), Res.ReflectionStaticFieldExpectedGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<object, string>(new object()), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.ReadOnlyReferenceField), fi.DeclaringType!));
            }

            test = new TestStruct(0);
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new TestStruct(0);
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyReferenceField), value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyReferenceField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(TestStruct.ReadOnlyReferenceField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(TestStruct.ReadOnlyReferenceField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
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
            AssertAreEqual(value, result);

            TestStruct.StaticIntProp = 0;
            Console.Write("Field Accessor...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            AssertAreEqual(value, result);
            if (!IsAot) // the fallback reflection accepts null as int
                AssertThrows<ArgumentNullException>(() => accessor.Set(null, null), Res.NotAnInstanceOfType(value.GetType()));
            AssertThrows<ArgumentException>(() => accessor.Set(null, "1"), Res.NotAnInstanceOfType(value.GetType()));

            TestStruct.StaticIntField = 0;
            Console.Write("Field Accessor Generic...");
            accessor.SetStaticValue(value);
            result = accessor.GetStaticValue<int>();
            AssertAreEqual(value, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetInstanceValue(new TestStruct(), value), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestStruct.StaticIntField), testType));
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue("1"), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.StaticIntField), testType));
            AssertThrows<InvalidOperationException>(() => accessor.GetInstanceValue<TestStruct, int>(new TestStruct()), Res.ReflectionInstanceFieldExpectedGeneric(nameof(TestStruct.StaticIntField), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<object>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(TestStruct.StaticIntField), testType));

            TestStruct.StaticIntField = 0;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            AssertAreEqual(value, result);

            TestStruct.StaticIntField = 0;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, nameof(TestStruct.StaticIntField), value);
            result = Reflector.GetField(testType, nameof(TestStruct.StaticIntField));
            AssertAreEqual(value, result);
            Reflector.SetField(testType, nameof(TestStruct.StaticIntField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(testType, nameof(TestStruct.StaticIntField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        #endregion

        #region Struct field access (unsafe)

        [Test]
        public unsafe void StructInstanceFieldAccessUnsafe()
        {
            object test = new UnsafeTestStruct();
            FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestStruct.InstanceField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = (IntPtr)Pointer.Unbox(fi.GetValue(test));
            AssertAreEqual(value, result);

            test = new UnsafeTestStruct();
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            var unsafeTestStruct = new UnsafeTestStruct();
            Console.Write("Field Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(unsafeTestStruct, value));
            else
            {
                accessor.SetInstanceValue(unsafeTestStruct, value);
                result = accessor.GetInstanceValue<UnsafeTestStruct, IntPtr>(unsafeTestStruct);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(unsafeTestStruct, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.InstanceField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(unsafeTestStruct), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.InstanceField), fi.DeclaringType!));
            }

            test = new UnsafeTestStruct();
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new UnsafeTestStruct();
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(UnsafeTestStruct.InstanceField), value);
            result = Reflector.GetField(test, nameof(UnsafeTestStruct.InstanceField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(UnsafeTestStruct.InstanceField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(UnsafeTestStruct.InstanceField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        [Test]
        public unsafe void StructInstanceReadOnlyValueFieldAccessUnsafe()
        {
            object test = new UnsafeTestStruct();
            FieldInfo fi = test.GetType().GetField(nameof(UnsafeTestStruct.ReadOnlyField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
            fi.SetValue(test, value);
            result = (IntPtr)Pointer.Unbox(fi.GetValue(test));
            AssertAreEqual(value, result);

            test = new UnsafeTestStruct();
            Console.Write("Field Accessor...");
            accessor.Set(test, value);
            result = accessor.Get(test);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(test, 1), Res.NotAnInstanceOfType(value.GetType()));

            var unsafeTestStruct = new UnsafeTestStruct();
            Console.Write("Field Accessor Generic...");
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.SetInstanceValue(unsafeTestStruct, value));
            else
            {
                accessor.SetInstanceValue(unsafeTestStruct, value);
                result = accessor.GetInstanceValue<UnsafeTestStruct, IntPtr>(unsafeTestStruct);
                AssertAreEqual(value, result);
                AssertThrows<ArgumentException>(() => accessor.SetInstanceValue(unsafeTestStruct, 1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyField), fi.DeclaringType!));
                AssertThrows<ArgumentException>(() => accessor.GetInstanceValue<UnsafeTestStruct, int>(unsafeTestStruct), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.ReadOnlyField), fi.DeclaringType!));
            }

            test = new UnsafeTestStruct();
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(test, fi, value);
            result = Reflector.GetField(test, fi);
            AssertAreEqual(value, result);

            test = new UnsafeTestStruct();
            Console.Write("Reflector (by name)...");
            Reflector.SetField(test, nameof(UnsafeTestStruct.ReadOnlyField), value);
            result = Reflector.GetField(test, nameof(UnsafeTestStruct.ReadOnlyField));
            AssertAreEqual(value, result);
            Reflector.SetField(test, nameof(UnsafeTestStruct.ReadOnlyField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(test, nameof(UnsafeTestStruct.ReadOnlyField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        [Test]
        public unsafe void StructStaticFieldAccessUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            FieldInfo fi = testType.GetField(nameof(UnsafeTestStruct.StaticField));
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;
            var value = new IntPtr(1);

            Console.Write("System Reflection...");
            fi.SetValue(null, value);
            result = (IntPtr)Pointer.Unbox(fi.GetValue(null));
            AssertAreEqual(value, result);

            UnsafeTestStruct.StaticField = null;
            Console.Write("Field Accessor...");
            accessor.Set(null, value);
            result = accessor.Get(null);
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.Set(null, 1), Res.NotAnInstanceOfType(value.GetType()));

            UnsafeTestStruct.StaticField = null;
            Console.Write("Field Accessor Generic...");
            accessor.SetStaticValue(value);
            result = accessor.GetStaticValue<IntPtr>();
            AssertAreEqual(value, result);
            AssertThrows<ArgumentException>(() => accessor.SetStaticValue(1), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.StaticField), testType));
            AssertThrows<ArgumentException>(() => accessor.GetStaticValue<int>(), Res.ReflectionCannotInvokeFieldGeneric(nameof(UnsafeTestStruct.StaticField), testType));

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by FieldInfo)...");
            Reflector.SetField(null, fi, value);
            result = Reflector.GetField(null, fi);
            AssertAreEqual(value, result);

            UnsafeTestStruct.StaticField = null;
            Console.Write("Reflector (by name)...");
            Reflector.SetField(testType, nameof(UnsafeTestStruct.StaticField), value);
            result = Reflector.GetField(testType, nameof(UnsafeTestStruct.StaticField));
            AssertAreEqual(value, result);
            Reflector.SetField(testType, nameof(UnsafeTestStruct.StaticField).ToLowerInvariant(), true, value);
            result = Reflector.GetField(testType, nameof(UnsafeTestStruct.StaticField).ToLowerInvariant(), true);
            AssertAreEqual(value, result);
        }

        #endregion

        #region Constant fields access

        [TestCase(typeof(byte), nameof(Byte.MaxValue), Byte.MaxValue)]
        [TestCase(typeof(sbyte), nameof(SByte.MinValue), SByte.MinValue)]
        [TestCase(typeof(short), nameof(Int16.MinValue), Int16.MinValue)]
        [TestCase(typeof(int), nameof(Int32.MinValue), Int32.MinValue)]
        [TestCase(typeof(long), nameof(Int64.MinValue), Int64.MinValue)]
        [TestCase(typeof(ushort), nameof(UInt16.MaxValue), UInt16.MaxValue)]
        [TestCase(typeof(uint), nameof(UInt32.MaxValue), UInt32.MaxValue)]
        [TestCase(typeof(ulong), nameof(UInt64.MaxValue), UInt64.MaxValue)]
        [TestCase(typeof(char), nameof(Char.MaxValue), Char.MaxValue)]
        [TestCase(typeof(float), nameof(Single.Epsilon), Single.Epsilon)]
        [TestCase(typeof(double), nameof(Double.Epsilon), Double.Epsilon)]
        [TestCase(typeof(TestConstants), nameof(TestConstants.BoolValue), TestConstants.BoolValue)]
        [TestCase(typeof(TestConstants), nameof(TestConstants.StringValue), TestConstants.StringValue)]
        [TestCase(typeof(TestConstants), nameof(TestConstants.EnumValue), TestConstants.EnumValue)]
        [TestCaseGeneric(typeof(TestConstants), nameof(TestConstants.NullValue), TestConstants.NullValue, TypeArguments = [typeof(string)])]
        [TestCaseGeneric(typeof(TestConstants), nameof(TestConstants.IntPtrValue), null, TypeArguments = [typeof(IntPtr)])] // null: to avoid CS1082
        [TestCaseGeneric(typeof(TestConstants), nameof(TestConstants.UIntPtrValue), null, TypeArguments = [typeof(UIntPtr)])] // null: to avoid CS1082
        [DynamicDependency(nameof(Byte.MaxValue), typeof(byte))]
        [DynamicDependency(nameof(SByte.MinValue), typeof(sbyte))]
        [DynamicDependency(nameof(Int16.MinValue), typeof(short))]
        [DynamicDependency(nameof(Int32.MinValue), typeof(int))]
        [DynamicDependency(nameof(Int64.MinValue), typeof(long))]
        [DynamicDependency(nameof(UInt16.MaxValue), typeof(ushort))]
        [DynamicDependency(nameof(UInt32.MaxValue), typeof(uint))]
        [DynamicDependency(nameof(UInt64.MaxValue), typeof(ulong))]
        [DynamicDependency(nameof(Char.MaxValue), typeof(char))]
        [DynamicDependency(nameof(Single.Epsilon), typeof(float))]
        [DynamicDependency(nameof(Double.Epsilon), typeof(double))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(TestConstants))]
        public void ConstantFieldAccess<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]Type type, string member, T expectedValue)
        {
            // workaround for CS1082: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
            if (typeof(T) == typeof(IntPtr))
                expectedValue = (T)(object)TestConstants.IntPtrValue;
            else if (typeof(T) == typeof(UIntPtr))
                expectedValue = (T)(object)TestConstants.UIntPtrValue;

            FieldInfo fi = type.GetField(member);
            FieldAccessor accessor = FieldAccessor.GetAccessor(fi);
            object result;

            Console.Write("System Reflection...");
            result = fi.GetValue(null);
            if (expectedValue is not (IntPtr or UIntPtr)) // System.Reflection returns simple integers for IntPtr constants
                AssertAreEqual(expectedValue, result);

            Console.Write("Field Accessor...");
            result = accessor.Get(null);
            AssertAreEqual(expectedValue, result);
            AssertThrows<InvalidOperationException>(() => accessor.Set(null, result), Res.ReflectionCannotSetConstantField(type, member));

            Console.Write("Field Accessor Generic...");
            result = accessor.GetStaticValue<T>();
            AssertAreEqual(expectedValue, result);
            AssertThrows<InvalidOperationException>(() => accessor.SetStaticValue(expectedValue), Res.ReflectionCannotSetConstantField(type, member));

            Console.Write("Reflector (by FieldInfo)...");
            result = Reflector.GetField(null, fi);
            AssertAreEqual(expectedValue, result);
            AssertThrows<InvalidOperationException>(() => Reflector.SetField(null, fi, result), Res.ReflectionCannotSetConstantField(type, member));

            Console.Write("Reflector (by name)...");
            result = Reflector.GetField(type, member);
            AssertAreEqual(expectedValue, result);
            AssertThrows<InvalidOperationException>(() => Reflector.SetField(type, member, result), Res.ReflectionCannotSetConstantField(type, member));
            result = Reflector.GetField(type, member.ToLowerInvariant(), true);
            AssertAreEqual(expectedValue, result);
            AssertThrows<InvalidOperationException>(() => Reflector.SetField(type, member.ToLowerInvariant(), true, result), Res.ReflectionCannotSetConstantField(type, member));
        }

        #endregion

        #region Class construction

        [Test]
        public void ClassConstructionByType()
        {
            Type testType = typeof(TestClass);
            var accessor = CreateInstanceAccessor.GetAccessor(testType);

            Console.Write("System Activator...");
            TestClass result = (TestClass)Activator.CreateInstance(testType)!;
            AssertAreEqual(1, result.IntProp);

            Console.Write("CreateInstanceAccessor General...");
            result = (TestClass)accessor.CreateInstance(Reflector.EmptyObjects);
            AssertAreEqual(1, result.IntProp);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (TestClass)accessor.CreateInstance();
            AssertAreEqual(1, result.IntProp);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestClass>();
            AssertAreEqual(1, result.IntProp);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestStruct>(), Res.ReflectionCannotCreateInstanceGeneric(testType));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestClass, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            result = (TestClass)Reflector.CreateInstance(testType);
            AssertAreEqual(1, result.IntProp);
        }

        [Test]
        public void ClassConstructionByCtorInfo()
        {
            Type testType = typeof(TestClass);
            ConstructorInfo ci = testType.GetConstructor([typeof(int)]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            int arg = 1;
            object[] args = [arg];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestClass result = (TestClass)ci.Invoke(parameters);
            AssertAreEqual(arg, result.IntProp);

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestClass)accessor.CreateInstance(parameters);
            AssertAreEqual(arg, result.IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(["x"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (TestClass)accessor.CreateInstance(arg);
            AssertAreEqual(arg, result.IntProp);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance("x"), Res.NotAnInstanceOfType(typeof(int)));

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestClass, int>(arg);
            AssertAreEqual(arg, result.IntProp);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestStruct, int>(arg), Res.ReflectionCannotCreateInstanceGeneric(testType));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestClass, string>(null), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestClass)Reflector.CreateInstance(ci, parameters);
            AssertAreEqual(arg, result.IntProp);
        }

        [Test]
        public void ClassComplexConstructionByCtorInfo()
        {
            Type testType = typeof(TestClass);
            ConstructorInfo ci = testType.GetConstructor([typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType()]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestClass result = (TestClass)ci.Invoke(parameters);
            AssertAreEqual(args[0], result.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

#if NET8_0_OR_GREATER
            Console.Write("System Reflection.ConstructorInvoker...");
            var inv = ConstructorInvoker.Create(ci);
            parameters = (object[])args.Clone();
            result = (TestClass)inv.Invoke(parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], result.IntProp);
#endif

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestClass)accessor.CreateInstance(parameters);
            AssertAreEqual(args[0], result.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = (TestClass)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], result.IntProp);

            Console.Write("CreateInstanceAccessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.CreateInstance<TestClass, int, string, bool, string>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], result.IntProp);

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestClass)Reflector.CreateInstance(ci, parameters);
            AssertAreEqual(args[0], result.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void InvalidTypeConstructionByType()
        {
            // abstract class
            Type testType = typeof(Type);
            var accessor = CreateInstanceAccessor.GetAccessor(testType);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // interface
            testType = typeof(IComparable);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<IComparable>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // static type
            testType = typeof(Res);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // generic type definition
            testType = typeof(List<>);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<object>(), Res.ReflectionCannotCreateInstanceOfType(testType));

            // no parameterless constructor
            testType = typeof(string);
            accessor = CreateInstanceAccessor.GetAccessor(testType);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionNoDefaultCtor(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionNoDefaultCtor(testType));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<string>(), Res.ReflectionNoDefaultCtor(testType));
        }

        [Test]
        public void InvalidTypeConstructionByCtorInfo()
        {
            // abstract class
            ConstructorInfo ci = typeof(Type).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
            var accessor = CreateInstanceAccessor.GetAccessor(ci);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(typeof(Type)));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(typeof(Type)));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(typeof(Type)));

            // generic type definition
            ci = typeof(List<>).GetConstructor(Type.EmptyTypes)!;
            accessor = CreateInstanceAccessor.GetAccessor(ci);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionCannotCreateInstanceOfType(typeof(List<>)));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionCannotCreateInstanceOfType(typeof(List<>)));
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<Type>(), Res.ReflectionCannotCreateInstanceOfType(typeof(List<>)));

            // static constructor
            ci = typeof(Res).GetConstructor(BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null)!;
            accessor = CreateInstanceAccessor.GetAccessor(ci);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionInstanceCtorExpected);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionInstanceCtorExpected);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<object>(), Res.ReflectionInstanceCtorExpected);

            // module constructor
            if (EnvironmentHelper.IsMono || !RuntimeFeature.IsDynamicCodeSupported)
                return;
            ci = ((Type)Reflector.GetProperty(typeof(Module).Module, "RuntimeType"))!.GetConstructor(BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null)!;
            accessor = CreateInstanceAccessor.GetAccessor(ci);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(null), Res.ReflectionInstanceCtorExpected);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance(), Res.ReflectionInstanceCtorExpected);
            AssertThrows<InvalidOperationException>(() => accessor.CreateInstance<object>(), Res.ReflectionInstanceCtorExpected);
        }

        #endregion

        #region Class construction (unsafe)

        [Test]
        public unsafe void ClassConstructionByCtorInfoPtrParamUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            ConstructorInfo ci = testType.GetConstructor([typeof(void*)]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;
            UnsafeTestClass result;

            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestClass)ci.Invoke(parameters);
            AssertAreEqual(arg, (IntPtr)result.InstanceProperty);

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestClass)accessor.CreateInstance(parameters);
            AssertAreEqual(arg, (IntPtr)result.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance([1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (UnsafeTestClass)accessor.CreateInstance(arg);
            AssertAreEqual(arg, (IntPtr)result.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<UnsafeTestClass, IntPtr>(arg);
            AssertAreEqual(arg, (IntPtr)result.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<UnsafeTestClass, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestClass)Reflector.CreateInstance(ci, parameters);
            AssertAreEqual(arg, (IntPtr)result.InstanceProperty);
        }

        [Test]
        public unsafe void ClassConstructionByCtorInfoRefParamUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            ConstructorInfo ci = testType.GetConstructor([typeof(void*).MakeByRefType()]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;
            UnsafeTestClass result;

            Console.Write("Direct call...");
            void* ptr = arg.ToPointer();
            result = new UnsafeTestClass(ref ptr);
            AssertAreEqual(arg, (IntPtr)result.InstanceField);
            AssertAreEqual(IntPtr.Zero, (IntPtr)ptr);

            // System Reflection does not support initializing the ref pointer parameter - ArgumentException : Object of type 'System.IntPtr' cannot be converted to type 'System.Void*&'
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestClass)ci.Invoke(parameters);
            AssertAreEqual(arg, (IntPtr)result.InstanceField);
            AssertAreEqual(IntPtr.Zero, parameters[0]);

            Console.Write("System Reflection.ConstructorInvoker...");
            var inv = ConstructorInvoker.Create(ci);
            parameters = (object[])args.Clone();
            result = (UnsafeTestClass)inv.Invoke(parameters.AsSpan());
            AssertAreEqual(arg, (IntPtr)result.InstanceField);
            AssertAreEqual(IntPtr.Zero, parameters[0]);
            Assert.Fail("Now that it works, update the validation in the fallback cases of CreateInstanceAccessor");
#endif

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance(parameters));
            else
            {
                result = (UnsafeTestClass)accessor.CreateInstance(parameters);
                AssertAreEqual(arg, (IntPtr)result.InstanceField);
                AssertAreEqual(IntPtr.Zero, parameters[0]);
            }

            Console.Write("CreateInstanceAccessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance(parameters[0]));
            else
            {
                result = (UnsafeTestClass)accessor.CreateInstance(parameters[0]);
                AssertAreEqual(arg, (IntPtr)result.InstanceField);
            }

            Console.Write("CreateInstanceAccessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance<UnsafeTestClass, IntPtr>(arg));
            else
            {
                result = accessor.CreateInstance<UnsafeTestClass, IntPtr>(arg);
                AssertAreEqual(arg, (IntPtr)result.InstanceField);
            }

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.CreateInstance(ci, parameters));
            else
            {
                result = (UnsafeTestClass)Reflector.CreateInstance(ci, parameters);
                AssertAreEqual(arg, (IntPtr)result.InstanceField);
                AssertAreEqual(IntPtr.Zero, parameters[0]);
            }
        }

        [Test]
        public unsafe void ClassComplexConstructionByCtorInfoUnsafe()
        {
            Type testType = typeof(UnsafeTestClass);
            ConstructorInfo ci = testType.GetConstructor([typeof(void*), typeof(int*), typeof(int*).MakeByRefType(), typeof(void*).MakeByRefType()]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;
            UnsafeTestClass result;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestClass)ci.Invoke(parameters);
            AssertAreEqual(args[0], (IntPtr)result.InstanceField);
            AssertAreNotEqual(args[2], parameters[2]);

            Console.Write("System Reflection.ConstructorInvoker...");
            var inv = ConstructorInvoker.Create(ci);
            parameters = (object[])args.Clone();
            result = (UnsafeTestClass)inv.Invoke(parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], (IntPtr)result.InstanceField);
            Assert.Fail("Now that it works, update the validation in the fallback cases of CreateInstanceAccessor");
#endif

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance(parameters));
            else
            {
                result = (UnsafeTestClass)accessor.CreateInstance(parameters);
                AssertAreEqual(args[0], (IntPtr)result.InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            Console.Write("CreateInstanceAccessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                result = (UnsafeTestClass)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], (IntPtr)result.InstanceField);
            }

            Console.Write("CreateInstanceAccessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance<UnsafeTestClass, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
            else
            {
                result = accessor.CreateInstance<UnsafeTestClass, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
                AssertAreEqual(args[0], (IntPtr)result.InstanceField);
            }

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.CreateInstance(ci, parameters));
            else
            {
                result = (UnsafeTestClass)Reflector.CreateInstance(ci, parameters);
                AssertAreEqual(args[0], (IntPtr)result.InstanceField);
                AssertAreNotEqual(args[2], parameters[2]);
            }
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
            AssertAreEqual(default(TestStruct), result);

            Console.Write("CreateInstanceAccessor General...");
            result = accessor.CreateInstance(Reflector.EmptyObjects);
            AssertAreEqual(default(TestStruct), result);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = accessor.CreateInstance();
            AssertAreEqual(default(TestStruct), result);

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestStruct>();
            AssertAreEqual(default(TestStruct), result);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestClass>(), Res.ReflectionCannotCreateInstanceGeneric(testType));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestStruct, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            result = Reflector.CreateInstance(testType);
            AssertAreEqual(default(TestStruct), result);
        }

        [Test]
        public void StructConstructionByCtorInfo()
        {
            Type testType = typeof(TestStruct);
            ConstructorInfo ci = testType.GetConstructor([typeof(int)]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            int arg = 1;
            object[] args = [arg];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestStruct result = (TestStruct)ci.Invoke(parameters);
            AssertAreEqual(args[0], result.IntProp);

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestStruct)accessor.CreateInstance(parameters);
            AssertAreEqual(args[0], result.IntProp);
            AssertThrows<ArgumentNullException>(() => accessor.CreateInstance(null), Res.ArgumentNull);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(Reflector.EmptyObjects), Res.ReflectionParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(["x"]), Res.ElementNotAnInstanceOfType(0, typeof(int)));

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (TestStruct)accessor.CreateInstance(arg);
            AssertAreEqual(args[0], result.IntProp);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(), Res.ReflectionParamsLengthMismatch(1, 0));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance("x"), Res.NotAnInstanceOfType(typeof(int)));

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<TestStruct, int>(arg);
            AssertAreEqual(arg, result.IntProp);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestClass, int>(arg), Res.ReflectionCannotCreateInstanceGeneric(testType));
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<TestStruct, string>(null), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestStruct)Reflector.CreateInstance(ci, parameters);
            AssertAreEqual(args[0], result.IntProp);
        }

        [Test]
        public void StructComplexConstructionByCtorInfo()
        {
            Type testType = typeof(TestStruct);
            ConstructorInfo ci = testType.GetConstructor([typeof(int), typeof(string), typeof(bool).MakeByRefType(), typeof(string).MakeByRefType()])!;
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            object[] args = [1, "dummy", false, null];

            Console.Write("System Reflection...");
            object[] parameters = (object[])args.Clone();
            TestStruct result = (TestStruct)ci.Invoke(parameters);
            AssertAreEqual(args[0], result.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (TestStruct)accessor.CreateInstance(parameters);
            AssertAreEqual(args[0], result.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);

            Console.Write("CreateInstanceAccessor NonGeneric...");
            parameters = (object[])args.Clone();
            result = (TestStruct)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
            AssertAreEqual(args[0], result.IntProp);

            Console.Write("CreateInstanceAccessor Generic...");
            parameters = (object[])args.Clone();
            result = accessor.CreateInstance<TestStruct, int, string, bool, string>((int)parameters[0], (string)parameters[1], (bool)parameters[2], (string)parameters[3]);
            AssertAreEqual(args[0], result.IntProp);

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (TestStruct)Reflector.CreateInstance(ci, parameters);
            AssertAreEqual(args[0], result.IntProp);
            AssertAreNotEqual(args[2], parameters[2]);
        }

        [Test]
        public void StructConstructionWithDefaultCtorByType()
        {
            Type testType = typeof(TestStructWithParameterlessCtor);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(testType);

            Console.Write("System Activator...");
            var result = (TestStructWithParameterlessCtor)Activator.CreateInstance(testType)!;
            Assert.IsTrue(result.Initialized);

            Console.Write("System Activator for the 2nd time...");
            result = (TestStructWithParameterlessCtor)Activator.CreateInstance(testType)!;
            if (!result.Initialized)
                Console.WriteLine("Constructor was not invoked!");
#if !NETFRAMEWORK // Activator.CreateInstance does not execute the default struct constructor for the 2nd time
            Assert.IsTrue(result.Initialized);
#endif

            Console.Write("Type Descriptor...");
            result = (TestStructWithParameterlessCtor)TypeDescriptor.CreateInstance(null, testType, null, null)!;
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
        public unsafe void StructConstructionByCtorInfoUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            ConstructorInfo ci = testType.GetConstructor([typeof(int*)]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            var arg = new IntPtr(1);
            object[] args = [arg];
            object[] parameters;
            UnsafeTestStruct result;

            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestStruct)ci.Invoke(parameters);
            AssertAreEqual(args[0], (IntPtr)result.InstanceProperty);

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestStruct)accessor.CreateInstance(parameters);
            AssertAreEqual(args[0], (IntPtr)result.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance([1]), Res.ElementNotAnInstanceOfType(0, typeof(IntPtr)));

            Console.Write("CreateInstanceAccessor NonGeneric...");
            result = (UnsafeTestStruct)accessor.CreateInstance(arg);
            AssertAreEqual(args[0], (IntPtr)result.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance(1), Res.NotAnInstanceOfType(typeof(IntPtr)));

            Console.Write("CreateInstanceAccessor Generic...");
            result = accessor.CreateInstance<UnsafeTestStruct, IntPtr>(arg);
            AssertAreEqual(arg, (IntPtr)result.InstanceProperty);
            AssertThrows<ArgumentException>(() => accessor.CreateInstance<UnsafeTestStruct, int>(1), Res.ReflectionCannotCreateInstanceGeneric(testType));

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestStruct)Reflector.CreateInstance(ci, parameters);
            AssertAreEqual(args[0], (IntPtr)result.InstanceProperty);
        }

        [Test]
        public unsafe void StructComplexConstructionByCtorInfoUnsafe()
        {
            Type testType = typeof(UnsafeTestStruct);
            ConstructorInfo ci = testType.GetConstructor([typeof(void*), typeof(int*), typeof(int*).MakeByRefType(), typeof(void*).MakeByRefType()]);
            CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(ci);
            object[] args = [new IntPtr(1), new IntPtr(2), null, new IntPtr(4)];
            object[] parameters;
            UnsafeTestStruct result;

            // System Reflection does not support initializing the ref pointer parameter and crashes when attempts to set back the out pointer parameter
#if NET11_0_OR_GREATER // increase version number if it's not fixed
            Console.Write("System Reflection...");
            parameters = (object[])args.Clone();
            result = (UnsafeTestStruct)ci.Invoke(parameters);
            AssertAreEqual(args[0], (IntPtr)result.ReadOnlyField);
            AssertAreNotEqual(args[2], parameters[2]);
#endif

            Console.Write("CreateInstanceAccessor General...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance(parameters));
            else
            {
                result = (UnsafeTestStruct)accessor.CreateInstance(parameters);
                AssertAreEqual(args[0], (IntPtr)result.ReadOnlyField);
                AssertAreNotEqual(args[2], parameters[2]);
            }

            Console.Write("CreateInstanceAccessor NonGeneric...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]));
            else
            {
                result = (UnsafeTestStruct)accessor.CreateInstance(parameters[0], parameters[1], parameters[2], parameters[3]);
                AssertAreEqual(args[0], (IntPtr)result.ReadOnlyField);
            }

            Console.Write("CreateInstanceAccessor Generic...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => accessor.CreateInstance<UnsafeTestStruct, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]));
            else
            {
                result = accessor.CreateInstance<UnsafeTestStruct, IntPtr, IntPtr, IntPtr, IntPtr>((IntPtr)parameters[0], (IntPtr)parameters[1], default, (IntPtr)parameters[3]);
                AssertAreEqual(args[0], (IntPtr)result.ReadOnlyField);
            }

            Console.Write("Reflector...");
            parameters = (object[])args.Clone();
            if (TestedFramework == TargetFramework.NetStandard20 || IsAot)
                AssertThrows<PlatformNotSupportedException>(() => Reflector.CreateInstance(ci, parameters));
            else
            {
                result = (UnsafeTestStruct)Reflector.CreateInstance(ci, parameters);
                AssertAreEqual(args[0], (IntPtr)result.ReadOnlyField);
                AssertAreNotEqual(args[2], parameters[2]);
            }
        }

        #endregion

        #region MemberOf

        [Test]
        public void MemberOfTest()
        {
            MemberInfo methodIntParse = Reflector.MemberOf(() => int.Parse(default(string), default(IFormatProvider))); // MethodInfo: Int32.Parse(string, IFormatProvider)
            AssertAreEqual(typeof(int).GetMethod(nameof(Int32.Parse), [typeof(string), typeof(IFormatProvider)]), methodIntParse);

            MemberInfo ctorList = Reflector.MemberOf(() => new List<int>()); // ConstructorInfo: List<int>().ctor()
            AssertAreEqual(typeof(List<int>).GetConstructor(Type.EmptyTypes), ctorList);

            MemberInfo fieldEmpty = Reflector.MemberOf(() => string.Empty); // FieldInfo: String.Empty
            AssertAreEqual(typeof(string).GetField(nameof(String.Empty)), fieldEmpty);

            MemberInfo propertyLength = Reflector.MemberOf(() => default(string).Length); // PropertyInfo: string.Length
            AssertAreEqual(typeof(string).GetProperty(nameof(String.Length)), propertyLength);

            MethodInfo methodAdd = Reflector.MemberOf(() => default(List<int>).Add(default(int))); // MethodInfo: List<int>.Add()
            AssertAreEqual(typeof(List<int>).GetMethod(nameof(List<>.Add)), methodAdd);
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
