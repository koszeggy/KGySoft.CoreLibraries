#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObservableObjectTest.cs
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
using System.Diagnostics.CodeAnalysis;
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

            [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
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
        public void UsageTest()
        {
            // Setting a property for the first time adds it in the underlying storage
            var obj = new Derived(false) { PublicProperty = true };
            Assert.IsTrue(obj.PublicProperty);

            // changing triggers the changed event
            bool changed = false;
            obj.PropertyChanged += (sender, args) => changed = true;
            obj.PublicProperty = false;
            Assert.IsFalse(obj.PublicProperty);
            Assert.IsTrue(changed);

            // resetting the property
            Assert.IsTrue(obj.TryReplaceProperty(nameof(obj.PublicProperty), true, ObservableObjectBase.MissingProperty, true));
            Assert.IsFalse(obj.PublicProperty);

            // disposing makes the properties unavailable
            obj.Dispose();
            Assert.Throws<ObjectDisposedException>(() => changed = obj.PublicProperty);
        }

#if !NETFRAMEWORK
        [Obsolete]
#endif
        [Test]
        public void SerializationTest()
        {
            var test = new Derived(true);
            var clone = test.DeepClone();
            Assert.AreEqual(test.PublicProperty, clone.PublicProperty);
        }

        #endregion
    }
}