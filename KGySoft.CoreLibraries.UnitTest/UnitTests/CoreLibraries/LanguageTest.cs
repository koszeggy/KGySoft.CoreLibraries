using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class LanguageTest
    {
        [Test]
        public void TestMethod()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            LanguageSettings.FormattingLanguageChanged += (sender, e) => PrintThread("FormattingLanguageChanged");
            LanguageSettings.FormattingLanguageChangedGlobal += (sender, e) => PrintThread("FormattingLanguageChangedGlobal " + (threadId == Thread.CurrentThread.ManagedThreadId));
            PrintThread("Main");
            Console.WriteLine("Setting en-GB in main");
            LanguageSettings.FormattingLanguage = CultureInfo.GetCultureInfo("en-GB");
            Console.WriteLine();
            //ManualResetEvent mre = new ManualResetEvent(false);

            ThreadStart threadStart = () =>
                {
                    Console.WriteLine("Setting hu-HU in work thread");
                    LanguageSettings.FormattingLanguage = CultureInfo.GetCultureInfo("hu-HU");
                    Console.WriteLine();
                    //mre.Set();
                };
            Thread t = new Thread(threadStart);
            t.Start();
            t.Join();
            PrintThread("Main");
        }

        private void PrintThread(string message)
        {
            Console.WriteLine(message + " ThreadID: {0}; Culture: {1}; UICulture: {2}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.CurrentCulture, Thread.CurrentThread.CurrentUICulture);
        }
    }
}
