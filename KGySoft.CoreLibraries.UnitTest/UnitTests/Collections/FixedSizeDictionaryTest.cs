#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FixedSizeDictionaryTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using KGySoft.Collections;
using KGySoft.Reflection;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class FixedSizeDictionaryTest
    {
        #region Methods

        [TestCase(false)]
        [TestCase(true)]
        public void UsageTest(bool ignoreCase)
        {
            var dict = new FixedSizeDictionary<string, int>(default, new Dictionary<string, int>
            {
                ["alpha"] = 1,
                ["beta"] = 2,
                ["gamma"] = 3,
            }, ignoreCase ? StringComparer.OrdinalIgnoreCase : null);

            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // TryAdd
            Assert.IsFalse(dict.TryAdd("alpha", 11));
            Assert.IsFalse(dict.TryAdd("delta", 4));
            Assert.AreEqual(3, dict.Count);

            // Update
            dict["alpha"] = 11;
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(11, dict["alpha"]);

            // Remove
            Assert.IsTrue(dict.TryRemove("alpha", out int value));
            Assert.AreEqual(11, value);
            Assert.AreEqual(2, dict.Count);
            Assert.IsFalse(dict.TryRemove("alpha", out value));
            Assert.AreEqual(0, value);
            Assert.IsTrue(dict.Remove("beta"));

            // Re-add
            Assert.IsTrue(dict.TryAdd("alpha", -1));
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(-1, dict["alpha"]);

            // Replace
            Assert.IsFalse(dict.TryReplace("alpha", 1, 11));
            Assert.IsTrue(dict.TryReplace("alpha", 1, -1));
            Assert.AreEqual(2, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // Clear
            dict.Clear();
            Assert.AreEqual(0, dict.Count);
            Assert.IsTrue(dict.TryAdd("alpha", 42));
            Assert.AreEqual(42, dict["alpha"]);
            Assert.AreEqual(1, dict.Count);
        }

        #endregion
    }
}
