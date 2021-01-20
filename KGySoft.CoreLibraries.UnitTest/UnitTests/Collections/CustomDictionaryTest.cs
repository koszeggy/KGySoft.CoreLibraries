#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CustomDictionaryTest.cs
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

using KGySoft.Collections;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Collections
{
    [TestFixture]
    public class CustomDictionaryTest
    {
        #region Methods

        [TestCase(false)]
        [TestCase(true)]
        public void UsageTest(bool ignoreCase)
        {
            var dict = new CustomDictionary<string, int>(default, 2, ignoreCase ? StringComparer.OrdinalIgnoreCase : null);

            // Add
            dict.Add("alpha", 1);
            dict.Add("beta", 2);
            dict.Add("gamma", 3); // here resize is needed
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // TryAdd
            Assert.IsFalse(dict.TryAdd("alpha", 11));
            Assert.IsTrue(dict.TryAdd("delta", 4));
            Assert.AreEqual(4, dict.Count);

            // Update
            dict["alpha"] = 11;
            Assert.AreEqual(4, dict.Count);
            Assert.AreEqual(11, dict["alpha"]);

            // Remove
            Assert.IsTrue(dict.TryRemove("alpha", out int value));
            Assert.AreEqual(11, value);
            Assert.AreEqual(3, dict.Count);
            Assert.IsFalse(dict.TryRemove("alpha", out value));
            Assert.AreEqual(0, value);
            Assert.IsTrue(dict.Remove("beta"));

            // Re-add
            dict.Add("alpha", -1);
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(-1, dict["alpha"]);

            // Replace
            Assert.IsFalse(dict.TryReplace("alpha", 1, 11));
            Assert.IsTrue(dict.TryReplace("alpha", 1, -1));
            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(1, dict["alpha"]);

            // Clear
            dict.Clear();
            Assert.AreEqual(0, dict.Count);
            dict.Add("alpha", 42);
            Assert.AreEqual(42, dict["alpha"]);
            Assert.AreEqual(1, dict.Count);
        }

        #endregion
    }
}
