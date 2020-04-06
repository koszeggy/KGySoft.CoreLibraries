#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UndoableObjectTest.cs
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

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    internal class UndoableObjectTest
    {
        #region Nested classes

        #region TestClass class

        private class TestClass : UndoableObjectBase
        {
            #region Properties

            public int IntProp { get => Get<int>(); set => Set(value); }

            public string StringProp { get => Get<string>(); set => Set(value); }

            #endregion
        }

        #endregion

        #endregion

        #region Methods

        [Test]
        public void UndoRedoTest()
        {
            var testObject = ThreadSafeRandom.Instance.NextObject<TestClass>();
            var origInt = testObject.IntProp;
            var origString = testObject.StringProp;
            Assert.IsTrue(testObject.CanUndo);
            Assert.IsFalse(testObject.CanRedo);

            testObject.UndoAll();
            Assert.IsFalse(testObject.CanUndo);
            Assert.IsTrue(testObject.CanRedo);

            testObject.RedoAll();
            Assert.AreEqual(origInt, testObject.IntProp);
            Assert.AreEqual(origString, testObject.StringProp);
        }

        #endregion
    }
}