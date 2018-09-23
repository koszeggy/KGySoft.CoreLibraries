using System;
using System.Reflection;
using System.Security.Policy;
using KGySoft.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Diagnostics
{
    /// <summary>
    /// This is a test class for DiagnosticToolsTest and is intended
    /// to contain all DiagnosticToolsTest Unit Tests
    ///</summary>
    [TestClass]
    public class DiagnosticToolsTest
    {
        sealed class TestClass<T> : MarshalByRefObject
        {
            public void Throw(T message)
            {
                throw new Exception(message.ToString());
            }

            public static void Throw<TMessage>(TMessage message)
            {
                throw new Exception(message.ToString());
            }
        }

        /// <summary>
        ///A test for ExceptionToString
        ///</summary>
        [TestMethod]
        public void ExceptionToStringTest()
        {
            Console.WriteLine("============Local Exception============");
            try
            {
                throw new Exception("test");
            }
            catch (Exception e)
            {
                Console.WriteLine("---------Exception.ToString:-----------");
                Console.WriteLine(e);
                Console.WriteLine("---------DiagnosticTools.ExceptionToString:-----------");
                Console.WriteLine(DiagnosticTools.ExceptionToString(e, false));
            }

            Console.WriteLine("============Exception from generic class============");
            try
            {
                TestClass<string> tc = new TestClass<string>();
                tc.Throw("test");
            }
            catch (Exception e)
            {
                Console.WriteLine("---------Exception.ToString:-----------");
                Console.WriteLine(e);
                Console.WriteLine("---------DiagnosticTools.ExceptionToString:-----------");
                Console.WriteLine(DiagnosticTools.ExceptionToString(e, false));
            }

            Console.WriteLine("============Exception from generic method============");
            try
            {
                TestClass<int>.Throw<string>("test");
            }
            catch (Exception e)
            {
                Console.WriteLine("---------Exception.ToString:-----------");
                Console.WriteLine(e);
                Console.WriteLine("---------DiagnosticTools.ExceptionToString:-----------");
                Console.WriteLine(DiagnosticTools.ExceptionToString(e, false));
            }

            Console.WriteLine("============Remote Exception from generic class============");
            try
            {
                Evidence evidence = new Evidence(AppDomain.CurrentDomain.Evidence);
                AppDomain domain = AppDomain.CreateDomain("TestDomain", evidence, AppDomain.CurrentDomain.BaseDirectory, null, false);
                TestClass<string> remoteInstance = (TestClass<string>)domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(TestClass<string>).FullName);
                remoteInstance.Throw("test");
            }
            catch (Exception e)
            {
                Console.WriteLine("---------Exception.ToString:-----------");
                Console.WriteLine(e);
                Console.WriteLine("---------DiagnosticTools.ExceptionToString:-----------");
                Console.WriteLine(DiagnosticTools.ExceptionToString(e, true));
            }
        }
    }
}
