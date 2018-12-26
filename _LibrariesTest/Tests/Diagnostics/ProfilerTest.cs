using System;
using KGySoft.Diagnostics;
using KGySoft.Reflection;
using NUnit.Framework;

namespace _LibrariesTest.Tests.Diagnostics
{
    [TestFixture]
    public class ProfilerTest
    {
        private int TestMethod()
        {
            return 0;
        }

        [Test]
        public void MesureTest()
        {
            Profiler.Enabled = true;
            Profiler.AutoSaveResults = false;
            const int iteration = 10000;
            using (Profiler.Measure("ProfilerTest.MeasureTest", "FullLength"))
            {
                MethodAccessor mi = MethodAccessor.GetAccessor(((Func<int>)TestMethod).Method);
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
