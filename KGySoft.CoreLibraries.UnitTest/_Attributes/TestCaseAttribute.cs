#if NETCOREAPP3_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TestCaseAttribute.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.CoreLibraries
{
    public class TestCaseAttribute<T> : TestCaseGenericAttribute
    {
        #region Constructors

        public TestCaseAttribute(params object[] arguments) : base(arguments) => TypeArguments = new[] { typeof(T) };

        #endregion
    }
}
#endif