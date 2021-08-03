#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TestCaseSourceGenericAttribute.cs
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

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;

#endregion

namespace KGySoft.CoreLibraries
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseSourceGenericAttribute : TestCaseSourceAttribute, ITestBuilder
    {
        #region Properties

        public Type[] TypeArguments { get; set; }

        #endregion

        #region Constructors

        public TestCaseSourceGenericAttribute(string sourceName)
            : base(sourceName)
        {
        }

        #endregion

        #region Methods

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            if (!method.IsGenericMethodDefinition)
                return base.BuildFrom(method, suite);

            if (TypeArguments == null || TypeArguments.Length != method.GetGenericArguments().Length)
            {
                var parms = new TestCaseParameters { RunState = RunState.NotRunnable };
                parms.Properties.Set(PropertyNames.SkipReason, $"{nameof(TypeArguments)} should have {method.GetGenericArguments().Length} elements");
                return new[] { new NUnitTestCaseBuilder().BuildTestMethod(method, suite, parms) };
            }

            var genMethod = method.MakeGenericMethod(TypeArguments);
            return base.BuildFrom(genMethod, suite);
        }

        #endregion
    }
}
