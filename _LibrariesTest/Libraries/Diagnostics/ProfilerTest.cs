using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using KGySoft.Libraries.Diagnostics;
using KGySoft.Libraries.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest
{
    using System.Diagnostics;

    [TestClass]
    public class ProfilerTest
    {
        private int TestMethod()
        {
            return 0;
        }

        [TestMethod]
        public void MesureTest()
        {
            Profiler.Enabled = true;
            Profiler.AutoSaveResults = false;
            const int iteration = 10000;
            using (Profiler.Measure("ProfilerTest.MeasureTest", "FullLength"))
            {
                MethodInvoker mi = MethodInvoker.GetMethodInvoker(((Func<int>)TestMethod).Method);
                for (int i = 0; i < iteration; i++)
                {
                    using (Profiler.Measure("ProfilerTest.MeasureTest", "InvokeMethod"))
                    {
                        mi.Invoke(this);
                    }
                }
            }
        }
    }
}
