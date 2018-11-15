using System;
using KGySoft.Diagnostics;
using KGySoft.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Tests.Diagnostics
{
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
