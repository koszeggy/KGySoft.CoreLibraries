﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReflectorPerformanceTest.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Reflection
{
    [TestFixture]
    public class ReflectorPerformanceTest
    {
        #region Nested types

        #region Nested classes

        #region TestClass class

        private class TestClass
        {
            #region Fields

            #region Static Fields

            public static int StaticField;

            #endregion

            #region Instance Fields

            public int InstanceField;

            #endregion

            #endregion

            #region Properties and Indexers

            #region Properties

            #region Static Properties

            public static int StaticProperty { get; set; }

            #endregion

            #region Instance Properties

            public int InstanceProperty { get; set; }

            #endregion

            #endregion

            #region Indexers

            [IndexerName("Indexer")]
            public int this[int index]
            {
                get => InstanceField + index;
                set => InstanceField = value + index;
            }

            #endregion

            #endregion

            #region Constructors

            public TestClass()
            {
            }

            public TestClass(int field, int value)
            {
                InstanceField = field;
                InstanceProperty = value;
            }

            #endregion

            #region Methods

            #region Static Methods

            public static int StaticMethod(int p1, int p2) => p1 + p2;


            #endregion

            #region Instance Methods

#if NET5_0_OR_GREATER
            [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Must be a property for the test")]
#endif
            public int InstanceMethod(int p1, int p2) => p1 + p2;

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Nested structs

        #region TestStruct struct

        private struct TestStruct
        {
            #region Constructors

            [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed for reflection test")]
            public TestStruct(int p1, int p2)
            {
            }

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region Methods

        [Test]
        public void TestMethodInvoke()
        {
            const int p1 = 1;
            const int p2 = 10;
            TestClass t = new TestClass();
            const string nameInstance = nameof(TestClass.InstanceMethod);
            const string nameStatic = nameof(TestClass.StaticMethod);
            MethodInfo miInstance = t.GetType().GetMethod(nameInstance);
            MethodInfo miStatic = t.GetType().GetMethod(nameStatic);
            MethodAccessor accessorInstance = MethodAccessor.GetAccessor(miInstance);
            MethodAccessor accessorStatic = MethodAccessor.GetAccessor(miStatic);
#if NET8_0_OR_GREATER
            MethodInvoker invokerInstance = MethodInvoker.Create(miInstance);
            MethodInvoker invokerStatic = MethodInvoker.Create(miStatic);
#endif

            new PerformanceTest<int> { TestName = "Method Invoke", Iterations = 1_000_000, TestTime = 200, Repeat = 1 }
                .AddCase(() => t.InstanceMethod(p1, p2), "Direct invoke (instance)")
                .AddCase(() => TestClass.StaticMethod(p1, p2), "Direct invoke (static)")

                .AddCase(() => (int)miInstance.Invoke(t, new object[] { p1, p2 }), "MethodInfo.Invoke (instance)")
                .AddCase(() => (int)miStatic.Invoke(null, new object[] { p1, p2 }), "MethodInfo.Invoke (static)")

#if !NET35
                .AddCase(() => ((dynamic)t).InstanceMethod(p1, p2), "Dynamic invoke (instance)")
#endif

                .AddCase(() => (int)typeof(TestClass).GetMethod(nameInstance).Invoke(t, new object[] { p1, p2 }), "Type.GetMethod(name).Invoke (instance)")
                .AddCase(() => (int)typeof(TestClass).GetMethod(nameStatic).Invoke(null, new object[] { p1, p2 }), "Type.GetMethod(name).Invoke (static)")

                .AddCase(() => (int)accessorInstance.Invoke(t, new object[] { p1, p2 }), "MethodAccessor.Invoke(object, object[]) (instance)")
                .AddCase(() => (int)accessorStatic.Invoke(null, new object[] { p1, p2 }), "MethodAccessor.Invoke(object, object[]) (static)")

                .AddCase(() => (int)accessorInstance.Invoke(t, p1, p2), "MethodAccessor.Invoke (specialized, instance)")
                .AddCase(() => (int)accessorStatic.Invoke(null, p1, p2), "MethodAccessor.Invoke (specialized, static)")

                .AddCase(() => accessorInstance.InvokeInstanceFunction<TestClass, int, int, int>(t, p1, p2), "MethodAccessor.InvokeInstanceFunction<,,,>")
                .AddCase(() => accessorStatic.InvokeStaticFunction<int, int, int>(p1, p2), "MethodAccessor.InvokeStaticFunction<,,>")

                .AddCase(() => (int)MethodAccessor.GetAccessor(miInstance).Invoke(t, p1, p2), "MethodAccessor.GetAccessor(MethodInfo).Invoke (instance)")
                .AddCase(() => (int)MethodAccessor.GetAccessor(miStatic).Invoke(null, p1, p2), "MethodAccessor.GetAccessor(MethodInfo).Invoke (static)")

#if NET8_0_OR_GREATER
                .AddCase(() => (int)invokerInstance.Invoke(t, p1, p2), "MethodInvoker.Invoke (instance)")
                .AddCase(() => (int)invokerStatic.Invoke(null, p1, p2), "MethodInvoker.Invoke (static)")

                .AddCase(() => (int)MethodInvoker.Create(miInstance).Invoke(t, p1, p2), "MethodInvoker.Create(MethodInfo).Invoke (instance)")
                .AddCase(() => (int)MethodInvoker.Create(miStatic).Invoke(null, p1, p2), "MethodInvoker.Create(MethodInfo).Invoke (static)")
#endif

                .AddCase(() => (int)Reflector.InvokeMethod(t, miInstance, p1, p2), "Reflector.InvokeMethod (instance by MethodInfo)")
                .AddCase(() => (int)Reflector.InvokeMethod(null, miStatic, p1, p2), "Reflector.InvokeMethod (static by MethodInfo)")

                .AddCase(() => (int)Reflector.InvokeMethod(t, nameInstance, p1, p2), "Reflector.InvokeMethod (instance by name)")
                .AddCase(() => (int)Reflector.InvokeMethod(typeof(TestClass), nameStatic, p1, p2), "Reflector.InvokeMethod (static by name)")

                .AddCase(() => (int)Accessors.InvokeMethod(t, nameInstance, p1, p2), "Accessors.InvokeMethod (instance by name)")
                .AddCase(() => (int)Accessors.InvokeMethod(typeof(TestClass), nameStatic, p1, p2), "Accessors.InvokeMethod (static by name)")

                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void TestFieldAccess()
        {
            TestClass t = new TestClass();
            const int value = 1;
            const string nameInstance = nameof(TestClass.InstanceField);
            const string nameStatic = nameof(TestClass.StaticField);
            FieldInfo fiInstance = t.GetType().GetField(nameInstance);
            FieldInfo fiStatic = t.GetType().GetField(nameStatic);
            FieldAccessor accessorInstance = FieldAccessor.GetAccessor(fiInstance);
            FieldAccessor accessorStatic = FieldAccessor.GetAccessor(fiStatic);

            new PerformanceTest { TestName = "Set Field", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => t.InstanceField = value, "Direct set (instance)")
                .AddCase(() => TestClass.StaticField = value, "Direct set (static)")

                .AddCase(() => fiInstance.SetValue(t, value), "FieldInfo.SetValue (instance)")
                .AddCase(() => fiStatic.SetValue(null, value), "FieldInfo.SetValue (static)")

#if !NET35
                .AddCase(() => ((dynamic)t).InstanceField = value, "Dynamic set (instance)") 
#endif

                .AddCase(() => typeof(TestClass).GetField(nameInstance).SetValue(t, value), "Type.GetField(name).SetValue (instance)")
                .AddCase(() => typeof(TestClass).GetField(nameStatic).SetValue(null, value), "Type.GetField(name).SetValue (static)")

                .AddCase(() => accessorInstance.Set(t, value), "FieldAccessor.Set (instance)")
                .AddCase(() => accessorStatic.Set(null, value), "FieldAccessor.Set (static)")

                .AddCase(() => accessorInstance.SetInstanceValue(t, value), "FieldAccessor.SetInstanceValue<,>")
                .AddCase(() => accessorStatic.SetStaticValue(value), "FieldAccessor.SetStaticValue<>")

                .AddCase(() => FieldAccessor.GetAccessor(fiInstance).Set(t, value), "FieldAccessor.GetAcceccor(FieldInfo).Set (instance)")
                .AddCase(() => FieldAccessor.GetAccessor(fiStatic).Set(null, value), "FieldAccessor.GetAcceccor(FieldInfo).Set (static)")

                .AddCase(() => Reflector.SetField(t, fiInstance, value), "Reflector.SetField (instance by FieldInfo)")
                .AddCase(() => Reflector.SetField(null, fiStatic, value), "Reflector.SetField (static by FieldInfo)")

                .AddCase(() => Reflector.SetField(t, nameInstance, value), "Reflector.SetField (instance by name)")
                .AddCase(() => Reflector.SetField(typeof(TestClass), nameStatic, value), "Reflector.SetField (static by name)")

                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Get Field", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => t.InstanceField, "Direct get (instance)")
                .AddCase(() => TestClass.StaticField, "Direct get (static)")

                .AddCase(() => (int)fiInstance.GetValue(t), "FieldInfo.GetValue (instance)")
                .AddCase(() => (int)fiStatic.GetValue(null), "FieldInfo.GetValue (static)")

#if !NET35
                .AddCase(() => ((dynamic)t).InstanceField, "Dynamic get (instance)") 
#endif

                .AddCase(() => (int)typeof(TestClass).GetField(nameInstance).GetValue(t), "Type.GetField(name).GetValue (instance)")
                .AddCase(() => (int)typeof(TestClass).GetField(nameStatic).GetValue(null), "Type.GetField(name).GetValue (static)")

                .AddCase(() => (int)accessorInstance.Get(t), "FieldAccessor.Get (instance)")
                .AddCase(() => (int)accessorStatic.Get(null), "FieldAccessor.Get (static)")

                .AddCase(() => accessorInstance.GetInstanceValue<TestClass, int>(t), "FieldAccessor.GetInstanceValue<,>")
                .AddCase(() => accessorStatic.GetStaticValue<int>(), "FieldAccessor.GetStaticValue<>")

                .AddCase(() => (int)FieldAccessor.GetAccessor(fiInstance).Get(t), "FieldAccessor.GetAcceccor(FieldInfo).Get (instance)")
                .AddCase(() => (int)FieldAccessor.GetAccessor(fiStatic).Get(null), "FieldAccessor.GetAcceccor(FieldInfo).Get (static)")

                .AddCase(() => (int)Reflector.GetField(t, fiInstance), "Reflector.GetField (instance by FieldInfo)")
                .AddCase(() => (int)Reflector.GetField(null, fiStatic), "Reflector.GetField (static by FieldInfo)")

                .AddCase(() => (int)Reflector.GetField(t, nameInstance), "Reflector.GetField (instance by name)")
                .AddCase(() => (int)Reflector.GetField(typeof(TestClass), nameStatic), "Reflector.GetField (static by name)")

                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void TestPropertyAccess()
        {
            TestClass t = new TestClass();
            const int value = 1;
            const string nameInstance = nameof(TestClass.InstanceProperty);
            const string nameStatic = nameof(TestClass.StaticProperty);
            PropertyInfo piInstance = t.GetType().GetProperty(nameInstance);
            PropertyInfo piStatic = t.GetType().GetProperty(nameStatic);
            PropertyAccessor accessorInstance = PropertyAccessor.GetAccessor(piInstance);
            PropertyAccessor accessorStatic = PropertyAccessor.GetAccessor(piStatic);
            PropertyDescriptorCollection propertyDescriptorCollection = TypeDescriptor.GetProperties(typeof(TestClass));
            PropertyDescriptor propertyDescriptor = propertyDescriptorCollection[nameInstance];

            new PerformanceTest { TestName = "Set Property", Iterations = 1_000_000, TestTime = 200, Repeat = 1 }
                .AddCase(() => t.InstanceProperty = value, "Direct set (instance)")
                .AddCase(() => TestClass.StaticProperty = value, "Direct set (static)")

                .AddCase(() => piInstance.SetValue(t, value, null), "PropertyInfo.SetValue (instance, general)")
                .AddCase(() => piStatic.SetValue(null, value, null), "PropertyInfo.SetValue (static, general)")

#if NET45_OR_GREATER || !NETFRAMEWORK
                .AddCase(() => piInstance.SetValue(t, value), "PropertyInfo.SetValue (instance, specialized)")
                .AddCase(() => piStatic.SetValue(null, value), "PropertyInfo.SetValue (static, specialized)")
#endif

#if !NET35
                .AddCase(() => ((dynamic)t).InstanceProperty = value, "Dynamic set (instance)")
#endif

                .AddCase(() => typeof(TestClass).GetProperty(nameInstance).SetValue(t, value, null), "Type.GetProperty(name).SetValue (instance)")
                .AddCase(() => typeof(TestClass).GetProperty(nameStatic).SetValue(null, value, null), "Type.GetProperty(name).SetValue (static)")

                .AddCase(() => propertyDescriptor.SetValue(t, value), "PropertyDescriptor.SetValue")
                .AddCase(() => propertyDescriptorCollection[nameInstance].SetValue(t, value), "PropertyDescriptorCollection[name].SetValue")
                .AddCase(() => TypeDescriptor.GetProperties(typeof(TestClass))[nameInstance].SetValue(t, value), "TypeDescriptor.GetProperties(Type)[name].SetValue")

                .AddCase(() => accessorInstance.Set(t, value, Reflector.EmptyObjects), "PropertyAccessor.Set (instance, general)")
                .AddCase(() => accessorStatic.Set(null, value, Reflector.EmptyObjects), "PropertyAccessor.Set (static, general)")

                .AddCase(() => accessorInstance.Set(t, value), "PropertyAccessor.Set (instance, specialized)")
                .AddCase(() => accessorStatic.Set(null, value), "PropertyAccessor.Set (static, specialized)")

                .AddCase(() => accessorInstance.SetInstanceValue(t, value), "PropertyAccessor.SetInstanceValue<,>")
                .AddCase(() => accessorStatic.SetStaticValue(value), "PropertyAccessor.SetStaticValue<>")

                .AddCase(() => PropertyAccessor.GetAccessor(piInstance).Set(t, value), "PropertyAccessor.GetAccessor(PropertyInfo).Set (instance)")
                .AddCase(() => PropertyAccessor.GetAccessor(piStatic).Set(null, value), "PropertyAccessor.GetAccessor(PropertyInfo).Set (static)")

                .AddCase(() => Reflector.SetProperty(t, piInstance, value), "Reflector.SetProperty (instance by PropertyInfo)")
                .AddCase(() => Reflector.SetProperty(null, piStatic, value), "Reflector.SetProperty (static by PropertyInfo)")

                .AddCase(() => Reflector.SetProperty(t, nameInstance, value), "Reflector.SetProperty (instance by name)")
                .AddCase(() => Reflector.SetProperty(typeof(TestClass), nameStatic, value), "Reflector.SetProperty (static by name)")

                .AddCase(() => Accessors.Set(piInstance, t, value), "Accessors.Set (instance)")
                .AddCase(() => Accessors.Set(piStatic, null, value), "Accessors.Set (static)")

                .AddCase(() => Accessors.SetPropertyValue(t, nameInstance, value), "Accessors.SetPropertyValue (instance by name)")

                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Get Property", Iterations = 1_000_000, TestTime = 200, Repeat = 1 }
                .AddCase(() => t.InstanceProperty, "Direct get (instance)")
                .AddCase(() => TestClass.StaticProperty, "Direct get (static)")

                .AddCase(() => (int)piInstance.GetValue(t, null), "PropertyInfo.GetValue (instance)")
                .AddCase(() => (int)piStatic.GetValue(null, null), "PropertyInfo.GetValue (static)")

#if !NET35
                .AddCase(() => ((dynamic)t).InstanceProperty, "Dynamic get (instance)")
#endif

                .AddCase(() => (int)typeof(TestClass).GetProperty(nameInstance).GetValue(t, null), "Type.GetProperty(name).GetValue (instance)")
                .AddCase(() => (int)typeof(TestClass).GetProperty(nameStatic).GetValue(null, null), "Type.GetProperty(name).GetValue (static)")

                .AddCase(() => (int)propertyDescriptor.GetValue(t), "PropertyDescriptor.GetValue")
                .AddCase(() => (int)propertyDescriptorCollection[nameInstance].GetValue(t), "PropertyDescriptorCollection[name].GetValue")
                .AddCase(() => (int)TypeDescriptor.GetProperties(typeof(TestClass))[nameInstance].GetValue(t), "TypeDescriptor.GetProperties(Type)[name].GetValue")

                .AddCase(() => (int)accessorInstance.Get(t), "PropertyAccessor.Get (instance)")
                .AddCase(() => (int)accessorStatic.Get(null), "PropertyAccessor.Get (static)")

                .AddCase(() => accessorInstance.GetInstanceValue<TestClass, int>(t), "PropertyAccessor.GetInstanceValue<,>")
                .AddCase(() => accessorStatic.GetStaticValue<int>(), "PropertyAccessor.GetStaticValue<>")

                .AddCase(() => (int)PropertyAccessor.GetAccessor(piInstance).Get(t), "PropertyAccessor.GetAcceccor(PropertyInfo).Get (instance)")
                .AddCase(() => (int)PropertyAccessor.GetAccessor(piStatic).Get(null), "PropertyAccessor.GetAcceccor(PropertyInfo).Get (static)")

                .AddCase(() => (int)Reflector.GetProperty(t, piInstance), "Reflector.GetProperty (instance by PropertyInfo)")
                .AddCase(() => (int)Reflector.GetProperty(null, piStatic), "Reflector.GetProperty (static by PropertyInfo)")

                .AddCase(() => (int)Reflector.GetProperty(t, nameInstance), "Reflector.GetProperty (instance by name)")
                .AddCase(() => (int)Reflector.GetProperty(typeof(TestClass), nameStatic), "Reflector.GetProperty (static by name)")

                .AddCase(() => (int)Accessors.Get(piInstance, t), "Accessors.Get (instance)")
                .AddCase(() => (int)Accessors.Get(piStatic, null), "Accessors.Get (static)")

                .AddCase(() => (int)Accessors.GetPropertyValue(t, nameInstance), "Accessors.GetPropertyValue (instance by name)")
                .AddCase(() => (int)Accessors.GetPropertyValue(typeof(TestClass), nameStatic), "Accessors.GetPropertyValue (static by name)")

                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void TestIndexerAccess()
        {
            TestClass t = new TestClass();
            const int index = 1;
            const int value = 10;
            const string name = "Indexer";
            PropertyInfo piIndexer = t.GetType().GetProperty(name);
            PropertyAccessor accessorIndexer = PropertyAccessor.GetAccessor(piIndexer);

            new PerformanceTest { TestName = "Set Indexer", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => t[index] = value, "Direct set")
                .AddCase(() => piIndexer.SetValue(t, value, new object[] { index }), "PropertyInfo.SetValue")
#if !NET35
                .AddCase(() => ((dynamic)t)[index] = value, "Dynamic set") 
#endif
                .AddCase(() => typeof(TestClass).GetProperty(name).SetValue(t, value, new object[] { index }), "Type.GetProperty(name).SetValue")
                .AddCase(() => accessorIndexer.Set(t, value, index), "PropertyAccessor.Set")
                .AddCase(() => accessorIndexer.SetInstanceValue(t, value, index), "PropertyAccessor.SetInstanceValue<,,>")
                .AddCase(() => PropertyAccessor.GetAccessor(piIndexer).Set(t, value, index), "PropertyAccessor.GetAccessor(PropertyInfo).Set")
                .AddCase(() => Reflector.SetProperty(t, piIndexer, value, index), "Reflector.SetProperty (by PropertyInfo)")
                .AddCase(() => Reflector.SetProperty(t, name, value, index), "Reflector.SetProperty (by name)")
                .AddCase(() => Reflector.SetIndexedMember(t, value, index), "Reflector.SetIndexedMember")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<int> { TestName = "Get Indexer", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => t[index], "Direct get")
                .AddCase(() => (int)piIndexer.GetValue(t, new object[] { index }), "PropertyInfo.GetValue")
#if !NET35
                .AddCase(() => ((dynamic)t)[index], "Dynamic get") 
#endif
                .AddCase(() => (int)typeof(TestClass).GetProperty(name).GetValue(t, new object[] { index }), "Type.GetProperty(name).GetValue")
                .AddCase(() => (int)accessorIndexer.Get(t, index), "PropertyAccessor.Get")
                .AddCase(() => accessorIndexer.GetInstanceValue<TestClass, int, int>(t, index), "PropertyAccessor.GetInstanceValue<,,>")
                .AddCase(() => (int)PropertyAccessor.GetAccessor(piIndexer).Get(t, index), "PropertyAccessor.GetAccessor(PropertyInfo).Get")
                .AddCase(() => (int)Reflector.GetProperty(t, piIndexer, index), "Reflector.GetProperty (by PropertyInfo)")
                .AddCase(() => (int)Reflector.GetProperty(t, name, index), "Reflector.GetProperty (by name)")
                .AddCase(() => (int)Reflector.GetIndexedMember(t, index), "Reflector.GetIndexedMember")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void TestArrayAccess()
        {
            var a = new byte[100];
            const int index = 1;
            const byte value = 10;

            new PerformanceTest { TestName = "Set Array Element", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => a[index] = value, "Direct set")
                .AddCase(() => a.SetValue(value, index), "Array.SetValue")
                .AddCase(() => Reflector.SetIndexedMember(a, value, index), "Reflector.SetIndexedMember")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<byte> { TestName = "Get Array Element", Iterations = 1_000_000, TestTime = 25 }
                .AddCase(() => a[index], "Direct get")
                .AddCase(() => (byte)a.GetValue(index), "Array.GetValue")
                .AddCase(() => (byte)Reflector.GetIndexedMember(a, index), "Reflector.GetIndexedMember")
                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void TestClassCreation()
        {
            const int p1 = 1;
            const int p2 = 10;
            Type type = typeof(TestClass);
            ConstructorInfo ciDefault = type.GetConstructor(Type.EmptyTypes);
            ConstructorInfo ciParams = type.GetConstructor(new[] { typeof(int), typeof(int) });
            CreateInstanceAccessor accessorType = CreateInstanceAccessor.GetAccessor(type);
            CreateInstanceAccessor accessorCtorDefault = CreateInstanceAccessor.GetAccessor(ciDefault);
            CreateInstanceAccessor accessorCtorParams = CreateInstanceAccessor.GetAccessor(ciParams);

            new PerformanceTest<TestClass> { TestName = "Create Class", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => new TestClass(), "Direct constructor invoke (default)")
                .AddCase(() => new TestClass(p1, p2), "Direct constructor invoke (parameterized)")

                .AddCase(() => (TestClass)ciDefault.Invoke(null), "ConstructorInfo.Invoke (default)")
                .AddCase(() => (TestClass)ciParams.Invoke(new object[] { p1, p2 }), "ConstructorInfo.Invoke (parameterized)")

                .AddCase(() => Activator.CreateInstance<TestClass>(), "Activator.CreateInstance<T>")
                .AddCase(() => (TestClass)Activator.CreateInstance(type), "Activator.CreateInstance (by type)")
                .AddCase(() => (TestClass)Activator.CreateInstance(type, p1, p2), "Activator.CreateInstance (by type and parameters)")

                .AddCase(() => (TestClass)TypeDescriptor.CreateInstance(null, type, null, null), "TypeDescriptor.CreateInstance (by type)")
                .AddCase(() => (TestClass)TypeDescriptor.CreateInstance(null, type, new[] { typeof(int), typeof(int) }, new object[] { p1, p2 }), "TypeDescriptor.CreateInstance (by type and parameters)")

                .AddCase(() => (TestClass)accessorType.CreateInstance(), "CreateInstanceAccessor.CreateInstance (by type)")
                .AddCase(() => (TestClass)accessorCtorDefault.CreateInstance(), "CreateInstanceAccessor.CreateInstance (by default constructor)")
                .AddCase(() => (TestClass)accessorCtorParams.CreateInstance(p1, p2), "CreateInstanceAccessor.CreateInstance (by parameterized constructor)")

                .AddCase(() => accessorType.CreateInstance<TestClass>(), "CreateInstanceAccessor.CreateInstance<> (by type)")
                .AddCase(() => accessorCtorDefault.CreateInstance<TestClass>(), "CreateInstanceAccessor.CreateInstance<> (by default constructor)")
                .AddCase(() => accessorCtorParams.CreateInstance<TestClass, int, int>(p1, p2), "CreateInstanceAccessor.CreateInstance<,,>")

                .AddCase(() => (TestClass)CreateInstanceAccessor.GetAccessor(type).CreateInstance(), "CreateInstanceAccessor.GetAccessor(Type).CreateInstance")
                .AddCase(() => (TestClass)CreateInstanceAccessor.GetAccessor(ciDefault).CreateInstance(), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance (default)")
                .AddCase(() => (TestClass)CreateInstanceAccessor.GetAccessor(ciParams).CreateInstance(p1, p2), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance (parameterized)")

                .AddCase(() => (TestClass)Reflector.CreateInstance(type), "Reflector.CreateInstance (by type)")
                .AddCase(() => (TestClass)Reflector.CreateInstance(type, p1, p2), "Reflector.CreateInstance (by type and parameters)")
                .AddCase(() => (TestClass)Reflector.CreateInstance(ciDefault), "Reflector.CreateInstance (by default constructor)")
                .AddCase(() => (TestClass)Reflector.CreateInstance(ciParams, p1, p2), "Reflector.CreateInstance (by parameterized constructor)")

                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void TestStructCreation()
        {
            const int p1 = 1;
            const int p2 = 10;
            Type type = typeof(TestStruct);
            ConstructorInfo ci = type.GetConstructor(new[] { typeof(int), typeof(int) });
            CreateInstanceAccessor accessorType = CreateInstanceAccessor.GetAccessor(type);
            CreateInstanceAccessor accessorCtor = CreateInstanceAccessor.GetAccessor(ci);

            new PerformanceTest<TestStruct> { TestName = "Create Struct", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => new TestStruct(), "Direct default initialization")
                .AddCase(() => new TestStruct(p1, p2), "Direct constructor invoke")

                .AddCase(() => (TestStruct)ci.Invoke(new object[] { p1, p2 }), "ConstructorInfo.Invoke")

                .AddCase(() => Activator.CreateInstance<TestStruct>(), "Activator.CreateInstance<T>")
                .AddCase(() => (TestStruct)Activator.CreateInstance(type), "Activator.CreateInstance (by type)")
                .AddCase(() => (TestStruct)Activator.CreateInstance(type, p1, p2), "Activator.CreateInstance (by type and parameters)")

                .AddCase(() => (TestStruct)TypeDescriptor.CreateInstance(null, type, null, null), "TypeDescriptor.CreateInstance (by type)")
                .AddCase(() => (TestStruct)TypeDescriptor.CreateInstance(null, type, new[] { typeof(int), typeof(int) }, new object[] { p1, p2 }), "TypeDescriptor.CreateInstance (by type and parameters)")

                .AddCase(() => (TestStruct)accessorType.CreateInstance(), "CreateInstanceAccessor.CreateInstance (by type)")
                .AddCase(() => (TestStruct)accessorCtor.CreateInstance(p1, p2), "CreateInstanceAccessor.CreateInstance (by constructor)")

                .AddCase(() => accessorType.CreateInstance<TestStruct>(), "CreateInstanceAccessor.CreateInstance<>")
                .AddCase(() => accessorCtor.CreateInstance<TestStruct, int, int>(p1, p2), "CreateInstanceAccessor.CreateInstance<,,>")

                .AddCase(() => (TestStruct)CreateInstanceAccessor.GetAccessor(type).CreateInstance(), "CreateInstanceAccessor.GetAccessor(Type).CreateInstance")
                .AddCase(() => (TestStruct)CreateInstanceAccessor.GetAccessor(ci).CreateInstance(p1, p2), "CreateInstanceAccessor.GetAccessor(ConstructorInfo).CreateInstance")

                .AddCase(() => (TestStruct)Reflector.CreateInstance(type), "Reflector.CreateInstance (by type)")
                .AddCase(() => (TestStruct)Reflector.CreateInstance(type, p1, p2), "Reflector.CreateInstance (by type and parameters)")
                .AddCase(() => (TestStruct)Reflector.CreateInstance(ci, p1, p2), "Reflector.CreateInstance (by constructor)")

                .DoTest()
                .DumpResults(Console.Out);
        }

        [Test]
        public void TestTypeResolve()
        {
            string typeNameInt = typeof(int).FullName;
            var list = new List<int>();
            string typeNameList = list.GetType().ToString();

            new PerformanceTest<Type> { TestName = "Resolve Type", Iterations = 1_000_000, TestTime = 200 }
                .AddCase(() => typeof(int), "typeof(int)")
                .AddCase(() => 0.GetType(), "intConstant.GetType")
                .AddCase(() => Type.GetType(typeNameInt), $"Type.GetType(\"{typeNameInt}\")")
                .AddCase(() => Reflector.ResolveType(typeNameInt), $"Reflector.ResolveType(\"{typeNameInt}\")")

                .AddCase(() => typeof(List<int>), "typeof(List<int>)")
                .AddCase(() => list.GetType(), "list.GetType")
                .AddCase(() => Type.GetType(typeNameList), $"Type.GetType(\"{typeNameList}\")")
                .AddCase(() => Reflector.ResolveType(typeNameList), $"Reflector.ResolveType(\"{typeNameList}\")")

                .DoTest()
                .DumpResults(Console.Out);
        }

        #endregion
    }
}
