#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DictionaryExtensionsTest.cs
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

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    internal class DictionaryExtensionsTest
    {
        #region Methods

        [Test]
        public void OverloadConflictTest()
        {
            var dObj = new Dictionary<string, object>();
            var dConv = new Dictionary<string, IConvertible>();
#if !(NET35 || NET40)
            IReadOnlyDictionary<string, IConvertible> dConvRo = dConv;
            IReadOnlyDictionary<string, object> dObjRo = dObj;
#endif
            IDictionary<string, IConvertible> dConvRw = dConv;
            IDictionary<string, object> dObjRw = dObj;

            // the extensions
            Assert.IsNull(dConv.GetValueOrDefault(""));
            Assert.IsNull(dObj.GetValueOrDefault(""));
#if !(NET35 || NET40)
            Assert.IsNull(dConvRo.GetValueOrDefault(""));
            Assert.IsNull(dObjRo.GetValueOrDefault(""));
            Assert.AreEqual(0, dObjRo.GetValueOrDefault<int>("")); 
#endif
            Assert.IsNull(dConvRw.GetValueOrDefault(""));
            Assert.IsNull(dObjRw.GetValueOrDefault(""));

            // actual value extensions (so the GetValueOrDefault methods can have the same behavior as the .NET Core/Standard methods)
            Assert.AreEqual(0, dObjRw.GetValueOrDefault<int>(""));
            Assert.AreEqual(1, dConv.GetActualValueOrDefault("", 1));
            Assert.AreEqual(1, dObj.GetActualValueOrDefault("", 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dConvRo.GetActualValueOrDefault("", 1));
#endif
            Assert.AreEqual(1, dConvRw.GetActualValueOrDefault("", 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dObjRo.GetActualValueOrDefault("", 1));
#endif
            Assert.AreEqual(1, dObjRw.GetActualValueOrDefault("", 1));


            var dictConv = new StringKeyedDictionary<IConvertible>();
            var dictObj = new StringKeyedDictionary<object>();
#if !(NET35 || NET40)
            IStringKeyedReadOnlyDictionary<IConvertible> dictConvRo = dictConv;
            IStringKeyedReadOnlyDictionary<object> dictObjRo = dictObj;
#endif
            IStringKeyedDictionary<IConvertible> dictConvRw = dictConv;
            IStringKeyedDictionary<object> dictObjRw = dictObj;

            // As StringKeyedDictionary defines these methods as well, these are not the extensions
            Assert.IsNull(dictConv.GetValueOrDefault(""));
            Assert.IsNull(dictObj.GetValueOrDefault(""));
#if !(NET35 || NET40)
            Assert.IsNull(dictConvRo.GetValueOrDefault(""));
            Assert.IsNull(dictObjRo.GetValueOrDefault(""));
            Assert.AreEqual(0, dictObjRo.GetValueOrDefault<int>(""));
#endif
            Assert.IsNull(dictConvRw.GetValueOrDefault(""));
            Assert.IsNull(dictObjRw.GetValueOrDefault(""));
            Assert.AreEqual(0, dictObjRw.GetValueOrDefault<int>(""));

            // But the extensions work seamlessly as for normal dictionaries
            Assert.AreEqual(1, dictConv.GetActualValueOrDefault("", 1));
            Assert.AreEqual(1, dictObj.GetActualValueOrDefault("", 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dictConvRo.GetActualValueOrDefault("", 1));
#endif
            Assert.AreEqual(1, dictConvRw.GetActualValueOrDefault("", 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dictObjRo.GetActualValueOrDefault("", 1));
#endif
            Assert.AreEqual(1, dictObjRw.GetActualValueOrDefault("", 1));

            // As here no need to be compatible with library methods, hese GetValueOrDefault can work the same way as GetActualValueOrDefault
            Assert.AreEqual(1, dictConv.GetValueOrDefault("", 1));
            Assert.AreEqual(1, dictObj.GetValueOrDefault("", 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dictConvRo.GetValueOrDefault("", 1));
#endif
            Assert.AreEqual(1, dictConvRw.GetValueOrDefault("", 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dictObjRo.GetValueOrDefault("", 1));
#endif
            Assert.AreEqual(1, dictObjRw.GetValueOrDefault("", 1));

            // StringSegment overloads
            Assert.IsNull(dictConv.GetValueOrDefault("".AsSegment()));
            Assert.IsNull(dictObj.GetValueOrDefault("".AsSegment()));
#if !(NET35 || NET40)
            Assert.IsNull(dictConvRo.GetValueOrDefault("".AsSegment()));
            Assert.IsNull(dictObjRo.GetValueOrDefault("".AsSegment()));
            Assert.AreEqual(0, dictObjRo.GetValueOrDefault<int>("".AsSegment()));
#endif
            Assert.IsNull(dictConvRw.GetValueOrDefault("".AsSegment()));
            Assert.IsNull(dictObjRw.GetValueOrDefault("".AsSegment()));
            Assert.AreEqual(0, dictObjRw.GetValueOrDefault<int>("".AsSegment()));
            Assert.AreEqual(1, dictConv.GetValueOrDefault("".AsSegment(), 1));
            Assert.AreEqual(1, dictObj.GetValueOrDefault("".AsSegment(), 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dictConvRo.GetValueOrDefault("".AsSegment(), 1));
#endif
            Assert.AreEqual(1, dictConvRw.GetValueOrDefault("".AsSegment(), 1));
#if !(NET35 || NET40)
            Assert.AreEqual(1, dictObjRo.GetValueOrDefault("".AsSegment(), 1));
#endif
            Assert.AreEqual(1, dictObjRw.GetValueOrDefault("".AsSegment(), 1));

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            // Span overloads
            Assert.IsNull(dictConv.GetValueOrDefault("".AsSpan()));
            Assert.IsNull(dictObj.GetValueOrDefault("".AsSpan()));
            Assert.IsNull(dictConvRo.GetValueOrDefault("".AsSpan()));
            Assert.IsNull(dictObjRo.GetValueOrDefault("".AsSpan()));
            Assert.AreEqual(0, dictObjRo.GetValueOrDefault<int>("".AsSpan()));
            Assert.IsNull(dictConvRw.GetValueOrDefault("".AsSpan()));
            Assert.IsNull(dictObjRw.GetValueOrDefault("".AsSpan()));
            Assert.AreEqual(0, dictObjRw.GetValueOrDefault<int>("".AsSpan()));
            Assert.AreEqual(1, dictConv.GetValueOrDefault("".AsSpan(), 1));
            Assert.AreEqual(1, dictObj.GetValueOrDefault("".AsSpan(), 1));
            Assert.AreEqual(1, dictConvRo.GetValueOrDefault("".AsSpan(), 1));
            Assert.AreEqual(1, dictConvRw.GetValueOrDefault("".AsSpan(), 1));
            Assert.AreEqual(1, dictObjRo.GetValueOrDefault("".AsSpan(), 1));
            Assert.AreEqual(1, dictObjRw.GetValueOrDefault("".AsSpan(), 1));
#endif
        }

        #endregion
    }
}
