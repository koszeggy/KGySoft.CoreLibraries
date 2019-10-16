﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeResolverTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;

using KGySoft.Reflection;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Reflection
{
    /// <summary>
    /// Tests for <see cref="Reflector.ResolveType(string,bool,bool)"/> method.
    /// Use non-mscorlib types, otherwise <see cref="Type.GetType(string)"/> resolves the string as well.
    /// </summary>
    [TestFixture]
    public class TypeResolverTest
    {
        private static readonly Type[] sourceDumpAndResolveTypesContainingGenericArguments =
        {
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
            typeof(Array).GetMethod("Resize").GetGenericArguments()[0], // T of Array.Resize<T>
            typeof(Array).GetMethod("Resize").GetGenericArguments()[0].MakeArrayType(), // T[] of Array.Resize<T>
            typeof(List<>).MakeGenericType(typeof(Array).GetMethod("Resize").GetGenericArguments()[0]), // List<T>
        };

        #region Methods

        [Test]
        public void TestAssemblyPartialResolve()
        {
#if NETFRAMEWORK
            string asmName = "System.Design"; 
#elif NETCOREAPP
            string asmName = "System.Data";
#else
#error .NET version is not supported
#endif
            if (Reflector.ResolveAssembly(asmName) != null)
            {
                Assert.Inconclusive("Assembly {0} is already loaded, test is ignored. Try to run this test alone.", asmName);
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
        [TestCase("System.Int32[*,*]")] // int[,]
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
        [TestCase("System.Collections.Generic.List`1[[System.Uri, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // List<int>
        [TestCase("System.Collections.Generic.List`1[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]")] // fail
        [TestCase("System.Collections.Generic.List`1[[System.Int32]]")] // List<int>
        [TestCase("System.Collections.Generic.List`1[ [ System.Int32] ] ")] // List<int>
        [TestCase("System.Collections.Generic.List`1[System.Int32]")] // List<int>
        [TestCase("System.Collections.Generic.List`1[System.Int32][]")] // List<int>[]
        [TestCase("System.Collections.Generic.List`1[[System.Int32]][]")] // List<int>[]
        [TestCase("System.Collections.Generic.List`1[System.Int32[]]")] // List<int[]>
        [TestCase("System.Collections.Generic.List`1[[System.Int32[]]]")] // List<int[]>
        [TestCase("System.Collections.Generic.List`1[[System.Int32][]]")] // fail
        [TestCase("System.Collections.Generic.List`1[System.Int32]&")] // List<int>&
        [TestCase("System.Collections.Generic.List`1[System.Int32&]")] // fail: The type 'System.Int32&' may not be used as a type argument.
        [TestCase("System.Collections.Generic.List`1[System.Int32*]")] // fail: The type 'System.Int32*' may not be used as a type argument.
        [TestCase("System.Collections.Generic.Dictionary`2[System.Int32,System.String]")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[ System.Int32, System.String]")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[[System.Int32],[System.String]]")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[ [ System.Int32] , [ System.String] ] ")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.Dictionary`2[[System.Int32],[System.Uri, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]")] // Dictionary<int, string>
        [TestCase("System.Collections.Generic.List`1+Enumerator[[System.Int32]]")] // List<int>.Enumerator
        [TestCase("System.Collections.Hashtable, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // Hashtable
        public void ResolveSystemCompatibleTypes(string typeName)
        {
            Console.WriteLine($"Test case: {typeName}");
            Type type = Reflector.ResolveType(typeName);
            Console.WriteLine($"Resolved to: {type?.GetName(TypeNameKind.FullName) ?? "<null>"}");

            if (type == null)
                Assert.Catch<Exception>(() => Type.GetType(typeName, true));
            else
                Assert.AreEqual(type, Type.GetType(typeName));
        }

        [TestCaseSource(nameof(sourceDumpAndResolveTypesContainingGenericArguments))]
        public void DumpAndResolveTypesContainingGenericArguments(Type type)
        {
            string fullName = type.GetName(TypeNameKind.FullName);
            string aqn = type.GetName(TypeNameKind.AssemblyQualifiedNameForced);
            Console.WriteLine($"Name: {type.GetName(TypeNameKind.Name)}");
            Console.WriteLine($"FullName: {fullName}");
            Console.WriteLine($"AssemblyQualifiedName: {aqn}");

            Assert.AreEqual(type, Reflector.ResolveType(aqn));
            Assert.AreEqual(type, Reflector.ResolveType(fullName));
        }

        #endregion
    }
}
