#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParameterizedPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using KGySoft.Diagnostics;
using KGySoft.Reflection;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    internal class ParameterizedPerformanceTest<TParam> : PerformanceTestBase<Action<TParam>, object>
    {
        #region Fields

        private TParam parameter;

        #endregion

        #region Properties

        public Func<TParam> ParamFactory { get; set; }

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({PerformanceTest.FrameworkVersion})";
        }

        #endregion

        #region Methods

        protected override object Invoke(Action<TParam> del)
        {
            del.Invoke(parameter);
            return null;
        }

        protected override void OnInitialize()
        {
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
            OnBeforeCase();
        }

        protected override void OnBeforeCase() => parameter = ParamFactory == null ? Activator.CreateInstance<TParam>() : ParamFactory.Invoke();

        #endregion
    }

    internal class ParameterizedPerformanceTest<TParam, TResult> : PerformanceTestBase<Func<TParam, TResult>, TResult>
    {
        #region Fields

        private TParam parameter;

        #endregion

        #region Properties

        public Func<TParam> ParamFactory { get; set; }

        public new string TestName
        {
            get => base.TestName;
            set => base.TestName = $"{value} ({PerformanceTest.FrameworkVersion})";
        }

        #endregion

        #region Methods

        protected override TResult Invoke(Func<TParam, TResult> del) => del.Invoke(parameter);

        protected override void OnInitialize()
        {
#if DEBUG
            Assert.Inconclusive("Run the performance test in Release Build");
#endif
            base.OnInitialize();
            PerformanceTest.CheckTestingFramework();
            OnBeforeCase();
        }

        protected override void OnBeforeCase() => parameter = ParamFactory == null ? Activator.CreateInstance<TParam>() : ParamFactory.Invoke();

        #endregion
    }
}
