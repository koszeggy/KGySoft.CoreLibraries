using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using KGySoft.Libraries;
using KGySoft.Libraries.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SystemXmlSerializer = System.Xml.Serialization.XmlSerializer;

namespace _PerformanceTest.Libraries.Serialization
{
    /// <summary>
    /// Summary description for BinarySerializerTest
    /// </summary>
    [TestClass]
    public class XmlSerializerPerformanceTest
    {
        public class SimpleClass
        {
            public int IntProp { get; set; }

            public string StringProp { get; set; }

            public object ObjProp { get; set; }
        }

        private static void DoTestXmlSerialize(object obj, XmlSerializationOptions options)
        {
            const int iterations = 10000;
            Type type = obj.GetType();
            Console.WriteLine("=========={0} (iterations: {1:N0}, options: {2})===========", obj.GetType(), iterations, Enum<XmlSerializationOptions>.ToString(options));

            StringBuilder sb = new StringBuilder();
            //XElement resultElement = new XElement("dummy");

            // first out of test:
            XmlSerializer.Serialize(new StringWriter(sb), obj, options);
            XmlSerializer.Deserialize(new StringReader(sb.ToString()));

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sb = new StringBuilder();
                //resultElement = XmlSerializer.Serialize(obj);
                //object deserializedObject = XmlSerializer.Deserialize(resultElement);
                XmlSerializer.Serialize(new StringWriter(sb), obj, options);
                object deserializedObject = XmlSerializer.Deserialize(new StringReader(sb.ToString()));
            }
            watch.Stop();
            //sb = new StringBuilder(resultElement.ToString());
            Console.WriteLine(sb);

            decimal time1 = watch.ElapsedMilliseconds;
            decimal size1 = sb.Length;
            Console.WriteLine("KGySoft XmlSerializer time: " + time1);
            Console.WriteLine("KGySoft XmlSerializer size: " + size1);

            SystemXmlSerializer serializer = new SystemXmlSerializer(type);

            // first out of test:
            sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                serializer.Serialize(sw, obj);
            }
            serializer.Deserialize(new StringReader(sb.ToString()));

            watch.Reset();
            watch.Start();

            for (int i = 0; i < iterations; i++)
            {
                sb = new StringBuilder();
                try
                {
                    //SystemXmlSerializer serializer = new SystemXmlSerializer(type);
                    using (StringWriter sw = new StringWriter(sb))
                    {
                        serializer.Serialize(sw, obj);
                    }
                    object deserializedObject = serializer.Deserialize(new StringReader(sb.ToString()));
                }
                catch (Exception e)
                {
                    Console.WriteLine("System serialization failed: {0}", e);
                    break;
                }
            }
            watch.Stop();

            if (sb.Length != 0)
            {
                Console.WriteLine("System XmlSerializer time: " + watch.ElapsedMilliseconds);
                Console.WriteLine("System XmlSerializer size: " + sb.Length);
                Console.WriteLine("Time performance: {0:P2}", time1 / watch.ElapsedMilliseconds);
                Console.WriteLine("Size performance: {0:P2}", size1 / sb.Length);
            }
        }

        [TestMethod]
        public void SerializerTest()
        {
            //var x = new byte[] { 1, 2, 3, 4, 5 };
            //var x = new int[] { 1, 2, 3, 4, 5 };
            //var x = 1;
            //var x = new List<int>(new int[] { 1, 2, 3, 4, 5 });
            //var x = new HashSet<int> { 1, 2, 3, 4, 5 };
            //var x = new HashSet<int[]> { new int[] { 1, 2, 3, 4, 5 }, null };
            //var x = new Collection<int>{ 1, 2, 3, 4, 5 };
            //var x = new DictionaryEntry(new object(), "alma");
            var x = new SimpleClass { IntProp = 1, StringProp = "alma", ObjProp = " . " };
            DoTestXmlSerialize(x, XmlSerializationOptions.RecursiveSerializationAsFallback | XmlSerializationOptions.IgnoreShouldSerialize | XmlSerializationOptions.IgnoreDefaultValueAttribute);
        }
    }
}
