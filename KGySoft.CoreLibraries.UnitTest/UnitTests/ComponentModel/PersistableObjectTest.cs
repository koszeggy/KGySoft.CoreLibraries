#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PersistableObjectTest.cs
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

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class PersistableObjectTest
    {
        #region Nested classes

        private class TestClass : PersistableObjectBase
        {
            #region Properties

            public bool TestProperty { get => Get<bool>(); set => Set(value); }

            #endregion
        }

        #endregion

        #region Methods

        [Test]
        public void TestUsage()
        {
            // The underlying storage is empty by default
            IPersistableObject obj = new TestClass();
            Assert.AreEqual(0, obj.GetProperties().Count);

            // Setting the property for the first time creates the property
            Assert.IsTrue(obj.CanSetProperty(nameof(TestClass.TestProperty), false));
            Assert.IsTrue(obj.SetProperty(nameof(TestClass.TestProperty), false));
            Assert.AreEqual(1, obj.GetProperties().Count);

            // Set returns false if the new value is the same as previously
            Assert.IsFalse(obj.SetProperty(nameof(TestClass.TestProperty), false));

            // cannot set null to a bool property
            Assert.IsFalse(obj.CanSetProperty(nameof(TestClass.TestProperty), null));
            Assert.Throws<InvalidOperationException>(() => obj.SetProperty(nameof(TestClass.TestProperty), null));

            // replace succeeds if new value is valid and different
            Assert.IsFalse(obj.TryReplaceProperty(nameof(TestClass.TestProperty), true, false));
            Assert.IsTrue(obj.TryReplaceProperty(nameof(TestClass.TestProperty), false, true));
            Assert.Throws<InvalidOperationException>(() => obj.TryReplaceProperty(nameof(TestClass.TestProperty), true, null));
        }

        #endregion
    }
}