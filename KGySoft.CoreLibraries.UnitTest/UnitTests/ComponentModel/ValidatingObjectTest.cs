#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ValidatingObjectTest.cs
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

using System.Collections.Generic;

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class ValidatingObjectTest
    {
        #region Nested classes

        #region TestClass class

        private class TestClass : ValidatingObjectBase
        {
            #region Properties

            public int IntProp { get => Get<int>(); set => Set(value); }

            public string StringProp { get => Get<string>(); set => Set(value); }

            #endregion

            #region Methods

            protected override ValidationResultsCollection DoValidation()
            {
                var result = new ValidationResultsCollection();
                if (IntProp < 0)
                    result.AddWarning(nameof(IntProp), "< 0");
                if (StringProp == null)
                    result.AddError(nameof(StringProp), "null");
                return result;
            }

            #endregion
        }

        #endregion

        #endregion

        #region Methods

        [Test]
        public void ValidationTest()
        {
            using var testObject = new TestClass
            {
                IntProp = 1,
                StringProp = "alpha"
            };

            var changedProperties = new HashSet<string>();
            testObject.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            // evaluating IsValid and ValidationResults for the first time
            Assert.IsTrue(testObject.IsValid);
            CollectionAssert.IsEmpty(testObject.ValidationResults);

            // if the object was valid, there was no IsValid change
            CollectionAssert.IsEmpty(changedProperties);

            // producing a warning makes ValidationResults invalidated but not immediately changed
            testObject.IntProp = -1;
            CollectionAssert.Contains(changedProperties, nameof(testObject.IntProp));
            CollectionAssert.DoesNotContain(changedProperties, nameof(testObject.ValidationResults));
            CollectionAssert.DoesNotContain(changedProperties, nameof(testObject.IsValid));

            // evaluating IsValid again makes ValidationResults changed
            Assert.IsTrue(testObject.IsValid);
            CollectionAssert.IsNotEmpty(testObject.ValidationResults);
            Assert.IsTrue(testObject.ValidationResults.HasWarnings);
            Assert.AreEqual(1, testObject.ValidationResults.Count);
            CollectionAssert.Contains(changedProperties, nameof(testObject.ValidationResults));
            CollectionAssert.DoesNotContain(changedProperties, nameof(testObject.IsValid));

            // producing an error: again, ValidationResults invalidated but not immediately changed
            testObject.StringProp = null;
            CollectionAssert.Contains(changedProperties, nameof(testObject.StringProp));
            CollectionAssert.Contains(changedProperties, nameof(testObject.ValidationResults));
            CollectionAssert.DoesNotContain(changedProperties, nameof(testObject.IsValid));

            // evaluating IsValid again updates ValidationResults
            Assert.IsFalse(testObject.IsValid);
            CollectionAssert.Contains(changedProperties, nameof(testObject.IsValid));
            Assert.IsTrue(testObject.ValidationResults.HasErrors);
            Assert.AreEqual(2, testObject.ValidationResults.Count);
        }

        #endregion
    }
}
