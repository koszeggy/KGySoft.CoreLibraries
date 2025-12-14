#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeResolverTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2025 - All Rights Reserved
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

using KGySoft.Annotations;
#if NET9_0_OR_GREATER
using System.Reflection.Metadata;
#endif

using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Reflection
{
    /// <summary>
    /// Tests for <see cref="Reflector.ResolveType(string,ResolveTypeOptions)"/> method.
    /// Use non-mscorlib types, otherwise <see cref="Type.GetType(string)"/> resolves the string as well.
    /// </summary>
    [TestFixture]
    public class TypeResolverTest
    {
        #region Fields

        private static readonly Type[] sourceDumpAndResolveTypesContainingGenericArguments =
        [
            typeof(List<>).GetGenericArguments()[0], // T of List<>
            typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(Dictionary<,>).GetGenericArguments()[1]), // Dictionary<string, TValue>
            typeof(Dictionary<,>).MakeGenericType(typeof(Dictionary<,>).GetGenericArguments()[0], typeof(string)), // Dictionary<TKey, string>
            typeof(List<>).GetGenericArguments()[0].MakeArrayType(), // T[] of List<>
            typeof(List<>).GetGenericArguments()[0].MakeByRefType(), // T& of List<>
            typeof(List<>).GetGenericArguments()[0].MakePointerType(), // T* of List<>
            typeof(List<>).GetGenericArguments()[0].MakeArrayType().MakeByRefType(), // T[]& of List<>
            typeof(List<>).GetGenericArguments()[0].MakeArrayType().MakePointerType().MakeByRefType(), // T[]*& of List<>
            typeof(List<>).MakeGenericType(typeof(Dictionary<,>).GetGenericArguments()[0]), // List<TKey>
            typeof(List<>).MakeGenericType(typeof(List<>).GetGenericArguments()[0].MakeArrayType()), // List<T[]>
            //typeof(List<>).MakeGenericType(typeof(List<>).GetGenericArguments()[0].MakeByRefType()), // List<T&>
            //typeof(List<>).MakeGenericType(typeof(List<>).GetGenericArguments()[0].MakePointerType()), // List<T*>
            typeof(List<>).MakeGenericType(typeof(Dictionary<,>).GetGenericArguments()[0]).MakeArrayType(), // List<TKey>[]
            typeof(Array).GetMethod("Resize")!.GetGenericArguments()[0], // T of Array.Resize<T>
            typeof(Array).GetMethod("Resize")!.GetGenericArguments()[0].MakeArrayType(), // T[] of Array.Resize<T>
            typeof(List<>).MakeGenericType(typeof(Array).GetMethod("Resize")!.GetGenericArguments()[0]) // List<T> of Array.Resize<T>
        ];

        private static readonly Type[] sourceFunctionPointerTypesTest =
        [
            typeof(delegate*<string, void>),
            typeof(delegate*<string, void>[]),
            typeof(delegate*<string, void>*),
            typeof(delegate*<string, void>*[]),
            typeof(delegate*<string, delegate*<string, void>, void>),
            typeof(delegate*<string, delegate*<string, void>[], void*[]>[]),
            typeof(delegate*<string, delegate*<string, void>>),
            typeof(delegate*<string, delegate*<string, void>[]>),
            typeof(delegate* managed<int?, void>),
            typeof(delegate* <int?, void>),
#if NET8_0_OR_GREATER
            typeof(delegate* unmanaged<KeyValuePair<int, string>, void*>),
            typeof(delegate* unmanaged<KeyValuePair<int, string>, void*>[]),
            typeof(delegate* unmanaged[Cdecl]<string, void>),
            typeof(delegate* unmanaged[Cdecl]<string, void>[]),
            typeof(delegate* unmanaged[Stdcall, SuppressGCTransition]<string, void>),
            typeof(delegate* unmanaged[Stdcall, SuppressGCTransition]<string, void>[,]),
#endif
        ];

        #endregion

        #region Methods

        [Test]
        public void TestAssemblyPartialResolve()
        {
#if NET35
            string asmName = "System.Design, Version=2.0.0.0, PublicKeyToken=b77a5c561934e089"; 
#else
            string asmName = "System.Numerics, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089";
#endif
            if (Reflector.ResolveAssembly(asmName, ResolveAssemblyOptions.AllowPartialMatch) != null)
            {
                Assert.Inconclusive($"Assembly {asmName} is already loaded, test is ignored. Try to run this test alone.");
                return;
            }

            Assert.IsNotNull(Reflector.ResolveAssembly(asmName) != null);
        }

        [TestCase("int")] // fail
        [TestCase("System.Int32")] // int
        [TestCase("System.Int32[]")] // int[]
        [TestCase("System.Int32*")] // int*
        [TestCase("System.Int32&")] // int&
        [TestCase("System.Int32[ ] ")] // int[]
        [TestCase("System.Int32[*]")] // int[*]
        [TestCase("System.Int32[,]")] // int[,]
        [TestCase("System.Int32[*,*]")] // fail
        [TestCase("System.Int32[*,**]")] // fail
        [TestCase("System.Int32[]*")] // int[]*
        [TestCase("System.Int32[]**&")] // int[]**&
        [TestCase("System.Int32[], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // int[]
        [TestCase("System.Int32[], mscorlib")] // int[]
        [TestCase("System.Int32[,][]")] // int[][,]
        [TestCase("System.Int32[][,]")] // int[,][]
        [TestCase("System.Collections.Generic.List`1")] // List<>
        [TestCase("System.Collections.Generic.List`1[]")] // List<>[]
        [TestCase("System.Collections.Generic.List`1[]&")] // List<>[]&
        [TestCase("System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // List<int>
        [TestCase("System.Collections.Generic.List`1[[System.Uri, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // List<Uri>
        [TestCase("System.Collections.Generic.List`1[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]")] // fail
        [TestCase("System.Collections.Generic.List`1[[System.Int32]]")] // List<int>
        [TestCase("System.Collections.Generic.List`1[ [ System.Int32] ] ")] // List<int>
        [TestCase("System.Collections.Generic.List`1[System.Int32]")] // List<int>
        [TestCase("System.Collections.Generic.List`1[System.Int32][]")] // List<int>[]
        [TestCase("System.Collections.Generic.List`1[[System.Int32]][]")] // List<int>[]
        [TestCase("System.Collections.Generic.List`1[System.Int32[]]")] // List<int[]>
        [TestCase("System.Collections.Generic.List`1[[System.Int32[]]]")] // List<int[]>
        [TestCase("System.Collections.Generic.List`1[[System.Collections.Generic.List`1[System.Int32], mscorlib]]")] // List<List<int>>
        [TestCase("System.Collections.Generic.List`1[[System.Int32][]]")] // fail (non-generic)
        [TestCase("System.Collections.Generic.List`1[System.Int32]&")] // List<int>&
        [TestCase("System.Collections.Generic.List`1[System.Int32&]")] // fail: The type 'System.Int32&' may not be used as a type argument (except in Mono)
        [TestCase("System.Collections.Generic.List`1[System.Int32*]")] // fail: The type 'System.Int32*' may not be used as a type argument (except in Mono)
        [TestCase("System.Collections.Generic.Dictionary`2[System.Int32,System.String]")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[ System.Int32, System.String]")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[[System.Int32],[System.String]]")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[ [ System.Int32] , [ System.String] ] ")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[[System.Int32],[System.Uri, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]")] // Dictionary<int, Uri>
        [TestCase("System.Collections.Generic.List`1+Enumerator[[System.Int32]]")] // List<int>.Enumerator
        [TestCase("System.Collections.Hashtable, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // Hashtable
        public void ResolveSystemCompatibleTypes(string typeName)
        {
            Console.WriteLine($"Test case: {typeName}");
            Type type = Reflector.ResolveType(typeName);
            Console.WriteLine($"Resolved to: {type?.GetName(TypeNameKind.LongName) ?? "<null>"}");

#if NET9_0_OR_GREATER
            if (TypeName.TryParse(typeName, out TypeName name))
            {
                Type typeByTypeName = Reflector.ResolveType(name, _ => null);
                Assert.AreEqual(type, typeByTypeName);
            }
            else
                Assert.IsNull(type);
#endif

            if (type == null)
            {
                // On Mono, it's quite unpredictable what Type.GetType does with invalid type names: null, exception, or even a successful resolution.
                if (!EnvironmentHelper.IsMono)
                    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed - justification: intended, testing only whether it throws an exception
                    Assert.Catch<Exception>(() => Type.GetType(typeName, true));
                return;
            }

            Type bySystem = Type.GetType(typeName);
            if (bySystem == null && EnvironmentHelper.IsMono)
            {
                Assert.Inconclusive($"On Mono System.Type.GetType fails to resolve {typeName}");
                return;
            }

            Assert.AreEqual(type, bySystem);
        }

        [TestCaseSource(nameof(sourceDumpAndResolveTypesContainingGenericArguments))]
        public void DumpAndResolveTypesContainingGenericArguments(Type type)
        {
            // NOTE: Cannot test .NET9+'s TypeName here because it cannot dump a name, and it also doesn't support type arguments
            string fullName = type.GetName(TypeNameKind.LongName);
            string aqn = type.GetName(TypeNameKind.ForcedAssemblyQualifiedName);
            Console.WriteLine($"Name: {type.GetName(TypeNameKind.ShortName)}");
            Console.WriteLine($"FullName: {fullName}");
            Console.WriteLine($"AssemblyQualifiedName: {aqn}");

            Assert.AreEqual(type, Reflector.ResolveType(aqn));
            Assert.AreEqual(type, Reflector.ResolveType(fullName));
        }

        [TestCaseSource(nameof(sourceFunctionPointerTypesTest))]
        public void FunctionPointerTypesTest(Type type)
        {
            Console.WriteLine($"Name: {type.GetName(TypeNameKind.ShortName)}");
            string fullName = type.GetName(TypeNameKind.LongName);
            string aqn = type.GetName(TypeNameKind.ForcedAssemblyQualifiedName);
            Console.WriteLine($"FullName: {fullName}");
            Console.WriteLine($"AssemblyQualifiedName: {aqn}");

            // resolve is not supported on recent platforms, but we can test parse/rebuild by stripping
            Assert.AreEqual(fullName, TypeResolver.StripName(aqn, false));
#if NET11_0_OR_GREATER
            Assert.AreEqual(type, Reflector.ResolveType(aqn));
            Assert.AreEqual(type, Reflector.ResolveType(fullName));
#endif
        }

        // simple types
        [TestCase("System.Int32", TypeNameKind.ShortName, "Int32")]
        [TestCase("System.Int32", TypeNameKind.LongName, "System.Int32")]
        [TestCase("System.Int32*", TypeNameKind.ShortName, "Int32*")]
        [TestCase("System.Int32&", TypeNameKind.ShortName, "Int32&")]
        [TestCase("System.Int32[ ] ", TypeNameKind.ShortName, "Int32[]")]
        [TestCase("System.Int32[*]", TypeNameKind.ShortName, "Int32[*]")]
        [TestCase("System.Int32[,]", TypeNameKind.ShortName, "Int32[,]")]
        [TestCase("System.Int32[*,*]", TypeNameKind.ShortName, null)]
        [TestCase("System.Int32[], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", TypeNameKind.ShortName, "Int32[]")]
        [TestCase("System.Int32[], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", TypeNameKind.FullName, "System.Int32[]")]
        
        // generics
        [TestCase("System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", TypeNameKind.ShortName, "List`1[Int32]")]
        [TestCase("System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", TypeNameKind.LongName, "System.Collections.Generic.List`1[System.Int32]")]
        [TestCase("System.Collections.Generic.List`1[[[System.Int32]]]", TypeNameKind.ShortName, null)]
        [TestCase("System.Collections.Generic.List`1[System.Int32][]", TypeNameKind.ShortName, "List`1[Int32][]")]
        [TestCase("System.Collections.Generic.List`1[[System.Int32]][]", TypeNameKind.ShortName, "List`1[Int32][]")]
        [TestCase("System.Collections.Generic.List`1[System.Int32[]]", TypeNameKind.ShortName, "List`1[Int32[]]")]
        [TestCase("System.Collections.Generic.List`1[[System.Int32[]]]", TypeNameKind.ShortName, "List`1[Int32[]]")]
        [TestCase("System.Collections.Generic.List`1[[System.Collections.Generic.List`1[System.Int32], mscorlib]]", TypeNameKind.ShortName, "List`1[List`1[Int32]]")]
        [TestCase("System.Collections.Generic.List`1[[System.Int32][]]", TypeNameKind.ShortName, null)]
        [TestCase("System.Collections.Generic.List`1+Enumerator[[System.Int32]]", TypeNameKind.ShortName, "Enumerator[Int32]")]
        [TestCase("System.Collections.Generic.Dictionary`2[System.Int32,System.String]", TypeNameKind.ShortName, "Dictionary`2[Int32,String]")]
        [TestCase("System.Collections.Generic.Dictionary`2[[System.Int32],[System.String]]", TypeNameKind.ShortName, "Dictionary`2[Int32,String]")]
        [TestCase("System.Collections.Generic.Dictionary`2[[System.Int32],[System.Uri, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", TypeNameKind.ShortName, "Dictionary`2[Int32,Uri]")]
        
        // generic type/method arguments
        [TestCase("!T:System.Collections.Generic.List`1", TypeNameKind.ShortName, "T")]
        [TestCase("!T[]&:System.Collections.Generic.List`1", TypeNameKind.ShortName, "T[]&")]
        [TestCase("!!T:Void Resize[T](T[] ByRef, Int32):System.Array", TypeNameKind.ShortName, "T")]
        [TestCase("System.Collections.Generic.Dictionary`2[System.String,!TValue:System.Collections.Generic.Dictionary`2]", TypeNameKind.ShortName, "Dictionary`2[String,TValue]")]
        [TestCase("System.Collections.Generic.List`1[!!T:Void Resize[T](T[] ByRef, Int32):System.Array]", TypeNameKind.ShortName, "List`1[T]")]

        // function pointers
        [TestCase("&fn():System.Void", TypeNameKind.LongName, "&fn():System.Void")]
        [TestCase("&fn():[System.Void]", TypeNameKind.ShortName, "&fn():Void")]
        [TestCase("&fn(System.Int32):System.Void", TypeNameKind.LongName, "&fn(System.Int32):System.Void")]
        [TestCase("&fn([System.Int32]):System.Void", TypeNameKind.LongName, "&fn(System.Int32):System.Void")]
        [TestCase("&fn(System.Int32, System.String):System.Void", TypeNameKind.ShortName, "&fn(Int32,String):Void")]
        [TestCase("&fn([System.Int32], System.String):System.Void", TypeNameKind.ShortName, "&fn(Int32,String):Void")]
        [TestCase("&fn(System.Int32, [System.String]):System.Void", TypeNameKind.ShortName, "&fn(Int32,String):Void")]
        [TestCase("&fn:System.Void", TypeNameKind.LongName, null)]
        [TestCase("&fn([System.Int32, System.Private.CoreLib]):[[System.Void]]", TypeNameKind.ShortName, null)]
        [TestCase("*fn[Cdecl](System.Int32[], System.String):System.Void", TypeNameKind.ShortName, "*fn(Int32[],String):Void")]
        [TestCase("*fn[Cdecl](System.Int32[], System.String[]):System.Void", TypeNameKind.ShortName, "*fn(Int32[],String[]):Void")]
        [TestCase("*fn[Cdecl]([System.Int32]):System.Void", TypeNameKind.ShortName, "*fn(Int32):Void")]
        [TestCase("*fn[Cdecl](System.Int32, [System.String]):System.Void", TypeNameKind.ShortName, "*fn(Int32,String):Void")]
        [TestCase("*fn[Cdecl](System.Int32):[System.Void]", TypeNameKind.ShortName, "*fn(Int32):Void")]
        [TestCase("*fn[Cdecl]([System.Int32[]]):[System.Void]", TypeNameKind.ShortName, "*fn(Int32[]):Void")]
        [TestCase("*fn[Cdecl]([System.Int32[]],[System.String]):[System.Void]", TypeNameKind.ShortName, "*fn(Int32[],String):Void")]
        [TestCase("*fn[Cdecl]([System.Int32],[System.String[]]):[System.Void]", TypeNameKind.ShortName, "*fn(Int32,String[]):Void")]
        [TestCase("*fn[Cdecl](System.Int32):System.Int32[]", TypeNameKind.ShortName, "*fn(Int32):Int32[]")]
        [TestCase("*fn[Cdecl](System.Int32):[System.Int32[]]", TypeNameKind.ShortName, "*fn(Int32):Int32[]")]
        [TestCase("*fn[Cdecl](System.Int32)[]:System.Void", TypeNameKind.ShortName, "*fn(Int32)[]:Void")]
        [TestCase("*fn[Cdecl]([System.Int32])[]:System.Void", TypeNameKind.ShortName, "*fn(Int32)[]:Void")]
        [TestCase("*fn[Cdecl](System.Int32)[]:[System.Void]", TypeNameKind.ShortName, "*fn(Int32)[]:Void")]
        [TestCase("*fn[Cdecl](System.List`1[System.Int32], System.String):System.Void", TypeNameKind.ShortName, "*fn(List`1[Int32],String):Void")]
        [TestCase("*fn[Cdecl]([System.Collections.Generic.List`1[System.Int32], mscorlib], System.String):System.Void", TypeNameKind.ShortName, "*fn(List`1[Int32],String):Void")]
        [TestCase("*fn[ Stdcall, SuppressGCTransition ] ( [ System.Collections.Generic.List`1[System.Int32] , mscorlib], System.String ) [,]: [System.Void] ", TypeNameKind.ShortName, "*fn(List`1[Int32],String)[,]:Void")]

        // embedded fn, 1st param
        [TestCase("&fn(&fn(System.String):System.Byte,System.Int32):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String):System.Byte,System.Int32):System.Void")]
        [TestCase("&fn(&fn([System.String]):System.Byte,System.Int32):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String):System.Byte,System.Int32):System.Void")]
        [TestCase("&fn(&fn(System.String):[System.Byte],System.Int32):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String):System.Byte,System.Int32):System.Void")]
        [TestCase("&fn([&fn(System.String):System.Byte],[System.Int32]):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String):System.Byte,System.Int32):System.Void")]
        [TestCase("&fn([&fn([System.String]):System.Byte],[System.Int32]):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String):System.Byte,System.Int32):System.Void")]
        [TestCase("&fn([&fn(System.String[]):System.Byte],[System.Int32]):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String[]):System.Byte,System.Int32):System.Void")]
        [TestCase("&fn([&fn(System.String):System.Byte],System.Int32):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String):System.Byte,System.Int32):System.Void")]
        [TestCase("&fn([&fn(System.String):[System.Byte]],System.Int32):System.Void", TypeNameKind.LongName, "&fn(&fn(System.String):System.Byte,System.Int32):System.Void")]

        // embedded fn, 2nd param
        [TestCase("&fn([System.Int32, System.Private.CoreLib],[&fn([System.String, System.Private.CoreLib]):[System.Byte, System.Private.CoreLib]]):[System.Void, System.Private.CoreLib]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String):System.Byte):System.Void")]
        [TestCase("&fn([System.Int32],[&fn([System.String]):[System.Byte]]):[System.Void]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String):System.Byte):System.Void")]
        [TestCase("&fn([System.Int32],[&fn([System.String]):System.Byte]):[System.Void]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String):System.Byte):System.Void")]
        [TestCase("&fn(System.Int32,[&fn(System.String):System.Byte]):System.Void", TypeNameKind.ShortName, "&fn(Int32,&fn(String):Byte):Void")]
        [TestCase("&fn([System.Int32],[&fn(System.String):System.Byte]):System.Void", TypeNameKind.ShortName, "&fn(Int32,&fn(String):Byte):Void")]
        [TestCase("&fn([System.Int32],&fn([System.String]):[System.Byte]):[System.Void]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String):System.Byte):System.Void")]
        [TestCase("&fn([System.Int32],&fn([System.String]):System.Byte):[System.Void]", TypeNameKind.ShortName, "&fn(Int32,&fn(String):Byte):Void")]
        [TestCase("&fn(System.Int32,&fn(System.String):System.Byte):System.Void", TypeNameKind.ShortName, "&fn(Int32,&fn(String):Byte):Void")]

        // embedded fn, 2nd param array
        [TestCase("&fn([System.Int32],[&fn([System.String])[]:[System.Byte]]):[System.Void]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String)[]:System.Byte):System.Void")]
        [TestCase("&fn([System.Int32],[&fn([System.String]):[System.Byte]])[]:[System.Void]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String):System.Byte)[]:System.Void")]
        [TestCase("&fn([System.Int32],&fn([System.String]):[System.Byte])[]:[System.Void]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String):System.Byte)[]:System.Void")]
        [TestCase("&fn([System.Int32],&fn([System.String]):System.Byte)[]:[System.Void]", TypeNameKind.LongName, "&fn(System.Int32,&fn(System.String):System.Byte)[]:System.Void")]

        // fn in return value
        [TestCase("&fn([System.Int32, System.Private.CoreLib]):[&fn([System.String, System.Private.CoreLib])[]:[System.Void, System.Private.CoreLib]]", TypeNameKind.ShortName, "&fn(Int32):&fn(String)[]:Void")]
        [TestCase("&fn(System.Int32):[&fn(System.String):System.Void]", TypeNameKind.ShortName, "&fn(Int32):&fn(String):Void")]
        [TestCase("&fn(System.Int32):[&fn(System.String)[]:System.Void]", TypeNameKind.ShortName, "&fn(Int32):&fn(String)[]:Void")]
        [TestCase("&fn(System.Int32):&fn(System.String):System.Void", TypeNameKind.ShortName, "&fn(Int32):&fn(String):Void")]
        [TestCase("&fn(System.Int32):&fn(System.String)[]:System.Void", TypeNameKind.ShortName, "&fn(Int32):&fn(String)[]:Void")]
        [TestCase("&fn(System.Int32):[&fn(System.String):[System.Void]]", TypeNameKind.ShortName, "&fn(Int32):&fn(String):Void")]
        [TestCase("&fn(System.Int32):&fn(System.String)[]:[System.Void]", TypeNameKind.ShortName, "&fn(Int32):&fn(String)[]:Void")]
        [TestCase("&fn(System.Int32):&fn(System.String):System.Void*", TypeNameKind.ShortName, "&fn(Int32):&fn(String):Void*")]
        [TestCase("&fn(System.Int32):[&fn(System.String):System.Void*]", TypeNameKind.ShortName, "&fn(Int32):&fn(String):Void*")]
        [TestCase("&fn(System.Int32):&fn(System.String):[System.Void*]", TypeNameKind.ShortName, "&fn(Int32):&fn(String):Void*")]
        [TestCase("&fn(System.Int32):[&fn(System.String):[System.Void*]]", TypeNameKind.ShortName, "&fn(Int32):&fn(String):Void*")]
        public void ParseTest(string typeName, TypeNameKind kind, [CanBeNull]string expected)
        {
            string result = TypeResolver.GetName(typeName, kind);
            Console.WriteLine(typeName);
            Console.WriteLine($" ==={kind}===>");
            Console.WriteLine(result);
            Assert.AreEqual(expected, result);

            Type type = Reflector.ResolveType(typeName);
            if (expected == null)
                Assert.IsNull(type);

#if NET11_0_OR_GREATER // - see https://github.com/dotnet/runtime/issues/75348
            Assert.AreEqual(TypeResolver.GetName(typeName, TypeNameKind.ShortName), type!.GetName(TypeNameKind.ShortName));
#endif
        }

        #endregion
    }
}
