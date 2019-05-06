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
            string asmName = "System.Windows.Forms";
            if (Reflector.ResolveAssembly(asmName, false, true) != null)
            {
                Assert.Inconclusive("Assembly {0} is already loaded, test is ignored. Try to run this test alone.", asmName);
                return;
            }

            Assert.IsNotNull(Reflector.ResolveAssembly(asmName, true, true) != null);
        }

        [Test]
        public void TestGeneric()
        {
            string s = "System.Collections.Generic.Dictionary`2[System.String,[System.Uri, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]";
            Type exp = typeof(Dictionary<string, Uri>);
            Type result = Reflector.ResolveType(s);
            Assert.AreSame(exp, result);

            s = "System.Collections.Generic.Dictionary`2[[System.String],[System.Uri]]";
            result = Reflector.ResolveType(s);
            Assert.AreSame(exp, result);

            s = "System.Collections.Generic.Dictionary`2[System.String,System.Uri]";
            result = Reflector.ResolveType(s);
            Assert.AreSame(exp, result);

            s = "System.Collections.Generic.List`1[System.Uri]";
            exp = typeof(List<Uri>);
            result = Reflector.ResolveType(s);
            Assert.AreSame(exp, result);

            s = "System.Collections.Generic.List`1[[System.Uri]]";
            result = Reflector.ResolveType(s);
            Assert.AreSame(exp, result);

            // partial asm name
            s = "System.Collections.Generic.List`1[[System.Uri, System]]";
            result = Reflector.ResolveType(s);
            Assert.AreSame(exp, result);
        }

        [Test]
        public void TestArrayTypes()
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

        #endregion
    }
}
