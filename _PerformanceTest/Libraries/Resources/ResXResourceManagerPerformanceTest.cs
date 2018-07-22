using System.Globalization;
using System.Resources;
using KGySoft.Libraries.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _PerformanceTest.Libraries.Resources
{
    [TestClass]
    public class ResXResourceManagerPerformanceTest : TestBase
    {
        [TestMethod]
        [DeploymentItem("Resources", "Resources")]
        public void GetObject()
        {
            var inv = CultureInfo.InvariantCulture;
            var hu = CultureInfo.GetCultureInfo("hu-HU");
            var refManager = new ResourceManager("_PerformanceTest.Resources.TestResourceResX", GetType().Assembly);
            var manager = new ResXResourceManager("TestResourceResX", GetType().Assembly);
            var test = new TestOperation
                {
                    TestName = "GetObject Invariant",
                    RefOpName = "ResourceManager",
                    TestOpName = "ResXResourceManager",
                    Iterations = 1000000,
                    ReferenceOperation = () => refManager.GetObject("TestString", inv),
                    TestOperation = () => manager.GetObject("TestString", inv),
                    Repeat = 5
                };

            test.DoTest();

            test.TestName = "GetObject fallback to invariant";
            test.ReferenceOperation = () => refManager.GetObject("TestString", hu);
            test.TestOperation = () => manager.GetObject("TestString", hu);

            test.DoTest();

            // 1. jelenleg a gyári vagy a resx-e a gyorsabb -> inv: 122.83 % - 128.50 %; hu: 127.37 % - 136.73 %
            // 2. a resx-ben mindenképpen overrideolni kell a GetString/Object-et, és beletenni az ortogonalitást. -> inv: 144.92 % - 149.58 %; hu: 151.30 % - 153.41 %
            // 3. Egy elemes cache után: inv: 101.34 % - 103.66 %; hu: 102.30 % - 106.24 %
            // 4. Késznek tekintett állapot: inv: 213.72 % - 217.49 %; hu: 220.73 % - 226.53 %
            // 5. aqnValid bevezetése után: inv: 95.83 % - 98.39 %; hu: 98.23 % - 100.85 %
            // 6. CultureInfo.InvariantCulture ReferenceEquals után: inv: 87.65 % - 92.90 %; hu: 98.48 % - 101.50 %
            // 7. Helyes fallback és proxyzás beépítése után: inv: 91.11 % - 92.18 %; hu: 97.79 % - 99.17 %
        }
    }
}
