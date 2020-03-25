#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObservableObjectTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2020 - All Rights Reserved
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
using System.Threading;
using KGySoft.ComponentModel;
using KGySoft.Diagnostics;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class ObservableObjectTest
    {
        #region Nested classes

        #region Base class

        private class Base : ObservableObjectBase
        {
            #region Properties

            private bool PrivateProperty { get => Get<bool>(); set => Set(value); }

            #endregion

            #region Constructors

            protected Base(bool value)
            {
                PrivateProperty = value;
            }

            #endregion
        }

        #endregion

        #region Derived class

        private class Derived : Base
        {
            #region Properties

            public bool PublicProperty { get => Get<bool>(); set => Set(value); }

            #endregion

            #region Constructors

            internal Derived(bool value) : base(value)
            {
            }

            #endregion
        }

        #endregion

        #endregion

        #region Methods

        [Test]
        public void AccessPrivatePropertyTest()
        {
            Assert.DoesNotThrow(() => new Derived(true));
        }

        [Test]
        public void TestMerge()
        {
            // Setting a property for the first time creates the locking storage
            var obj = new Derived(false) { PublicProperty = true };
            Assert.IsTrue(obj.PublicProperty);

            // changing in the locking storage
            obj.PublicProperty = false;
            Assert.IsFalse(obj.PublicProperty);

            // waiting until the timeout and letting the merge happen
            Thread.Sleep(100);
            Assert.IsFalse(obj.PublicProperty);

            // now the change occurs in the non-locking storage
            obj.PublicProperty = true;
            Assert.IsTrue(obj.PublicProperty);

            // resetting the property
            Assert.IsTrue(obj.TryReplaceProperty(nameof(obj.PublicProperty), true, ObservableObjectBase.MissingProperty, true));
            Assert.IsFalse(obj.PublicProperty);
        }

        #endregion
    }
}