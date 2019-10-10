#region Copyright

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
            if (Reflector.ResolveAssembly(asmName, false, true) != null)
            {
                Assert.Inconclusive("Assembly {0} is already loaded, test is ignored. Try to run this test alone.", asmName);
                return;
            }

            Assert.IsNotNull(Reflector.ResolveAssembly(asmName, true, true) != null);
        }

        [TestCase("System.Collections.Generic.Dictionary`2[System.String,[System.Uri, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", typeof(Dictionary<string, Uri>))]
        [TestCase("System.Collections.Generic.Dictionary`2[[System.String],[System.Uri]]", typeof(Dictionary<string, Uri>))]
        [TestCase("System.Collections.Generic.Dictionary`2[System.String,System.Uri]", typeof(Dictionary<string, Uri>))]
        [TestCase("System.Collections.Generic.List`1[System.Uri]", typeof(List<Uri>))]
        [TestCase("System.Collections.Generic.List`1[[System.Uri]]", typeof(List<Uri>))]
        [TestCase("System.Collections.Generic.List`1[[System.Uri, System]]", typeof(List<Uri>))]
        [TestCase("System.Collections.Generic.List`1[System.Uri][]", typeof(List<Uri>[]))]
        [TestCase("System.Collections.Generic.List`1[System.Uri[]]", typeof(List<Uri[]>))]
        public void ResolveGenericTypesTest(string name, Type expectedType)
        {
            Assert.AreSame(expectedType, Reflector.ResolveType(name));
        }

        [Test]
        public void ResolveArrayTypesTest()
        {
            Type t = typeof(Uri[]);
            Type result = Reflector.ResolveType(t.ToString());
            Assert.AreSame(t, result);

            t = typeof(Uri[,]);
            result = Reflector.ResolveType(t.ToString());
            Assert.AreSame(t, result);

            t = typeof(Uri[,][]);
            result = Reflector.ResolveType(t.ToString());
            Assert.AreSame(t, result);

            t = t.MakeArrayType(1); // non-necessarily zero-based 1 dimensional array type
            result = Reflector.ResolveType(t.ToString());
            Assert.AreSame(t, result);

            t = Array.CreateInstance(typeof(Uri), new int[] { 3, 3 }, new int[] { -1, 1 }).GetType(); // array with non-zero-based dimensions
            result = Reflector.ResolveType(t.ToString());
            Assert.AreSame(t, result);

            string s = "System.Uri[*,*]"; // same as [,]
            result = Reflector.ResolveType(s);
            Assert.AreSame(t, result);
        }

        [Test]
        public void TestArrayGenericTypes()
        {
            Type t = Reflector.ResolveType(typeof(Queue<Uri>[]).ToString());
            Assert.AreSame(typeof(Queue<Uri>[]), t);

            t = Reflector.ResolveType(typeof(Queue<Uri>[,]).ToString());
            Assert.AreSame(typeof(Queue<Uri>[,]), t);

            t = Reflector.ResolveType(typeof(Queue<Uri>[,][]).ToString());
            Assert.AreSame(typeof(Queue<Uri>[,][]), t);

            t = Reflector.ResolveType(typeof(Queue<Uri[]>).ToString());
            Assert.AreSame(typeof(Queue<Uri[]>), t);
        }

        [TestCase("int")] // fail
        [TestCase("System.Int32")] // int
        [TestCase("System.Int32[]")] // int[]
        [TestCase("System.Int32*")] // int*
        [TestCase("System.Int32&")] // int&
        [TestCase("System.Int32[ ] ")] // int[]
        [TestCase("System.Int32[*]")] // int[*]
        [TestCase("System.Int32[,]")] // int[,]
        [TestCase("System.Int32[*,**]")] // int[,]
        [TestCase("System.Int32[]*")] // int[]*
        [TestCase("System.Int32[]**&")] // int[]**&
        [TestCase("System.Int32[], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // int[]
        [TestCase("System.Int32[], mscorlib")] // int[]
        [TestCase("System.Int32[,][]")] // int[][,]
        [TestCase("System.Int32[][,]")] // int[,][]
        [TestCase("System.Collections.Generic.List`1")] // List<>
        [TestCase("System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] // List<int>
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
        public void ResolveTests(string typeName)
        {
            Console.WriteLine($"Test case: {typeName}");
            TypeResolver resolver = new TypeResolver(typeName, false);
            Type type = null;
            Exception exception = null;
            try
            {
                type = resolver.Resolve();
            }
            catch (Exception e)
            {
                exception = e;
            }

            Console.WriteLine($"Resolved to: {type?.ToString() ?? exception?.Message ?? "<null>"}");
            Console.WriteLine($"RootName: {resolver?.RootName ?? "<null>"}");
            Console.WriteLine($"Name: {resolver.Name ?? "<null>"}");
            Console.WriteLine($"FullName: {resolver.FullName ?? "<null>"}");
            Console.WriteLine($"AssemblyQualifiedName: {resolver.AssemblyQualifiedName ?? "<null>"}");

            // if type cannot be resolved, it is not resolvable by Type.GetType either
            if (type == null)
            {
                if (exception == null)
                    Assert.IsNull(Type.GetType(typeName));
                else
                    Assert.Throws(exception.GetType(), () => Type.GetType(typeName));
                return;
            }

            // Name is always compatible
            Assert.AreEqual(type.Name, resolver.Name);

            if (type.ContainsGenericParameters)
                return;

            // Full name is the same as Type.ToString(), except for generic parameters, which return enough information for resolve
            Assert.AreEqual(type.ToString(), resolver.FullName);

            // Type.GetType is able to resolve by AssemblyQualifiedName
            Assert.AreEqual(type, Type.GetType(resolver.AssemblyQualifiedName));
        }

        #endregion
    }
}
