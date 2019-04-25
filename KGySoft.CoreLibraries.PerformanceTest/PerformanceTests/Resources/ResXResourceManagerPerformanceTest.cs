using System;
using System.Globalization;
using System.Resources;
using KGySoft.Diagnostics;
using KGySoft.Resources;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.PerformanceTests.Resources
{
    [TestFixture]
    public class ResXResourceManagerPerformanceTest
    {
        [Test]
        //[DeploymentItem("Resources", "Resources")]
        public void GetObject()
        {
            var inv = CultureInfo.InvariantCulture;
            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var refManager = new ResourceManager("KGySoft.CoreLibraries.Resources.TestResourceResX", GetType().Assembly);
            var manager = new ResXResourceManager("TestResourceResX", GetType().Assembly);
            new PerformanceTest<object>
                {
                    TestName = "GetObject Invariant Test",
                    Iterations = 1000000,
                    Repeat = 5
                }
                .AddCase(() => refManager.GetObject("TestString", inv), "ResourceManager")
                .AddCase(() => manager.GetObject("TestString", inv), "ResXResourceManager")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<object>
                {
                    TestName = "GetObject fallback to invariant",
                    Iterations = 1000000,
                    Repeat = 5
                }
                .AddCase(() => refManager.GetObject("TestString", hu), "ResourceManager")
                .AddCase(() => manager.GetObject("TestString", hu), "ResXResourceManager")
                .DoTest()
                .DumpResults(Console.Out);
        }
    }
}
