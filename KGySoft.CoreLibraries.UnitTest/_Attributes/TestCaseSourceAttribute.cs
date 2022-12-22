#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TestCaseSourceAttribute.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.CoreLibraries
{
    public class TestCaseSourceAttribute<T> : TestCaseSourceGenericAttribute
    {
        #region Constructors

        public TestCaseSourceAttribute(string sourceName) : base(sourceName) => TypeArguments = new[] { typeof(T) };

        #endregion
    }
}